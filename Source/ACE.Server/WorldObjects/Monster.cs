using System;

namespace ACE.Server.WorldObjects
{
    /// <summary>
    /// Monster AI functions
    /// </summary>
    partial class Creature
    {
        public bool IsMonster;

        /// <summary>
        /// The exclusive state of the monster
        /// </summary>
        public State MonsterState = State.Idle;

        /// <summary>
        /// The exclusive states the monster can be in
        /// </summary>
        public enum State
        {
            Idle,
            Awake
        };

        public void EquipInventoryItems(bool weaponsOnly = false)
        {
            var items = weaponsOnly ? SelectWieldedWeapons() : SelectWieldedTreasure();
            if (items != null)
            {
                foreach (var item in items)
                {
                    //Console.WriteLine($"{Name} equipping {item.Name}");

                    if (item.ValidLocations != null)
                        TryEquipObject(item, (int)item.ValidLocations);
                }
            }
        }

        /// <summary>
        /// Cleans up state on monster death
        /// </summary>
        public void OnDeath()
        {
            IsTurning = false;
            IsMoving = false;

            //SetFinalPosition();
        }
    }
}
