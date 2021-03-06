using System;
using System.Linq;
using ACE.Database.Models.Shard;
using ACE.Entity.Enum;
using ACE.Server.Entity;
using ACE.Server.Entity.Actions;
using ACE.Server.Network.GameMessages.Messages;
using ACE.Server.Network.Motion;
using ACE.Server.Physics.Animation;

namespace ACE.Server.WorldObjects
{
    /// <summary>
    /// Monster casting for magic spells
    /// </summary>
    partial class Creature
    {
        /// <summary>
        /// Returns TRUE if monster is a spell caster
        /// </summary>
        public bool IsCaster { get => Biota.BiotaPropertiesSpellBook.Count > 0; }

        /// <summary>
        /// The next spell the monster will attempt to cast
        /// </summary>
        public BiotaPropertiesSpellBook CurrentSpell;

        /// <summary>
        /// The delay after casting a magic spell
        /// </summary>
        public static readonly float MagicDelay = 2.0f;

        /// <summary>
        /// Returns the monster's current magic skill
        /// for the school containing the current spell
        /// </summary>
        public uint GetMagicSkill()
        {
            var currentSpell = GetCurrentSpell();
            return GetCreatureSkill((MagicSchool)currentSpell.School).Current;
        }

        /// <summary>
        /// Returns the magic skill level used for spell range checks.
        /// (initial points + points due to directly raising the skill)
        /// </summary>
        /// <returns></returns>
        public uint GetMagicSkillForRangeCheck()
        {
            var currentSpell = GetCurrentSpell();
            var skill = GetCreatureSkill((MagicSchool)currentSpell.School);
            return skill.InitLevel + skill.Ranks;
        }

        /// <summary>
        /// Returns the sum of all probabilities from monster's spell_book
        /// </summary>
        public float GetSpellProbability()
        {
            var probability = 0.0f;

            foreach (var spell in Biota.BiotaPropertiesSpellBook)
                probability += spell.Probability;

            return probability;
        }

        /// <summary>
        /// Rolls for a chance to cast magic spell
        /// </summary>
        public bool RollCastMagic()
        {
            var probability = GetSpellProbability();
            //Console.WriteLine("Spell probability: " + probability);

            var rng = Physics.Common.Random.RollDice(0.0f, 100.0f);
            //var rng = Physics.Common.Random.RollDice(0.0f, probability);
            return rng < probability;
        }

        /// <summary>
        /// Perform the first part of monster spell casting animation - spreading arms out
        /// </summary>
        public float PreCastMotion(WorldObject target)
        {
            // todo: monster spellcasting anim speed?
            var castMotion = new MotionItem(MotionCommand.CastSpell, 1.5f);
            var animLength = MotionTable.GetAnimationLength(MotionTableId, CurrentMotionState.Stance, MotionCommand.CastSpell, 1.5f);

            var motion = new UniversalMotion(CurrentMotionState.Stance, castMotion);
            motion.MovementData.CurrentStyle = (uint)CurrentMotionState.Stance;
            motion.MovementData.TurnSpeed = 2.25f;
            //motion.HasTarget = true;
            //motion.TargetGuid = target.Guid;
            CurrentMotionState = motion;

            EnqueueBroadcastMotion(motion);

            return animLength;
        }

        /// <summary>
        /// Perform the animations after casting a spell,
        /// ie. moving arms back in, returning to previous stance
        /// </summary>
        public void PostCastMotion()
        {
            // todo: monster spellcasting anim speed?
            var castMotion = new MotionItem(MotionCommand.Ready, 1.5f);

            var motion = new UniversalMotion(CurrentMotionState.Stance, castMotion);
            motion.MovementData.CurrentStyle = (uint)CurrentMotionState.Stance;
            motion.MovementData.TurnSpeed = 2.25f;
            //motion.HasTarget = true;
            //motion.TargetGuid = target.Guid;
            CurrentMotionState = motion;

            EnqueueBroadcastMotion(motion);
        }

        /// <summary>
        /// Performs the monster windup spell animation,
        /// casts the spell, and returns to attack stance
        /// </summary>
        public void MagicAttack()
        {
            var spell = GetCurrentSpell();
            //Console.WriteLine(spell.Name);

            var preCastTime = PreCastMotion(AttackTarget);

            var actionChain = new ActionChain();
            actionChain.AddDelaySeconds(preCastTime);
            actionChain.AddAction(this, () =>
            {
                CastSpell();
                PostCastMotion();
            });
            actionChain.EnqueueChain();

            var postCastTime = MotionTable.GetAnimationLength(MotionTableId, CurrentMotionState.Stance, MotionCommand.CastSpell, MotionCommand.Ready, 1.5f);
            var animTime = preCastTime + postCastTime;

            //Console.WriteLine($"{Name}.MagicAttack(): preCastTime({preCastTime}), postCastTime({postCastTime})");

            NextAttackTime = DateTime.UtcNow.AddSeconds(animTime + MagicDelay);
        }

        /// <summary>
        /// Casts the current monster spell on target
        /// </summary>
        public void CastSpell()
        {
            if (AttackTarget == null) return;

            bool? resisted;
            var spell = GetCurrentSpell();

            var targetSelf = spell.Flags.HasFlag(SpellFlags.SelfTargeted);
            var target = targetSelf ? this : AttackTarget;

            var player = AttackTarget as Player;

            switch (spell.School)
            {
                case MagicSchool.WarMagic:

                    WarMagic(AttackTarget, spell);
                    break;

                case MagicSchool.LifeMagic:

                    resisted = ResistSpell(target, spell);
                    if (!targetSelf && (resisted == true)) break;
                    if (resisted == null)
                    {
                        log.Error("Something went wrong with the Magic resistance check");
                        break;
                    }
                    LifeMagic(target, spell, out uint damage, out bool critical, out var msg);
                    EnqueueBroadcast(new GameMessageScript(target.Guid, spell.TargetEffect, spell.Formula.Scale));
                    break;

                case MagicSchool.CreatureEnchantment:

                    resisted = ResistSpell(target, spell);
                    if (!targetSelf && (resisted == true)) break;
                    if (resisted == null)
                    {
                        log.Error("Something went wrong with the Magic resistance check");
                        break;
                    }
                    CreatureMagic(target, spell);
                    EnqueueBroadcast(new GameMessageScript(target.Guid, spell.TargetEffect, spell.Formula.Scale));
                    break;
            }
        }

        /// <summary>
        /// Selects a random spell from the monster's spell book
        /// according to the probabilities
        /// </summary>
        public BiotaPropertiesSpellBook GetRandomSpell()
        {
            var probability = GetSpellProbability();
            var rng = Physics.Common.Random.RollDice(0.0f, probability);

            var currentSpell = 0.0f;
            foreach (var spell in Biota.BiotaPropertiesSpellBook)
            {
                if (rng < currentSpell + spell.Probability)
                    return spell;

                currentSpell += spell.Probability;
            }
            return Biota.BiotaPropertiesSpellBook.Last();
        }

        /// <summary>
        /// Returns the maximum range for the current spell
        /// </summary>
        public float GetSpellMaxRange()
        {
            var spell = GetCurrentSpell();
            var skill = GetMagicSkillForRangeCheck();

            var maxRange = spell.BaseRangeConstant + skill * spell.BaseRangeMod;
            if (maxRange == 0.0f)
                maxRange = float.PositiveInfinity;

            return maxRange;
        }

        /// <summary>
        /// Returns the current Spell for the monster
        /// </summary>
        public Spell GetCurrentSpell()
        {
            return new Spell(CurrentSpell.Spell);
        }
    }
}
