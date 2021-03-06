using System;
using ACE.Server.WorldObjects;

namespace ACE.Server.Network.Enum
{
    [Flags]
    public enum WeaponMask
    {
        AttackSkill     = 0x1,
        MeleeDefense    = 0x2,
        Speed           = 0x4,
        Damage          = 0x8,
        DamageVariance  = 0x10,
        DamageMod       = 0x20
    }

    public static class WeaponMaskHelper
    {
        public static WeaponMask GetHighlightMask(WorldObject weapon, WorldObject wielder)
        {
            WeaponMask highlightMask = 0;

            if (wielder == null)
                return highlightMask;

            // Enchant applies to all weapons
            if (wielder.EnchantmentManager.GetDefenseMod() != 0)
                highlightMask |= WeaponMask.MeleeDefense;

            // Following enchants do not apply to caster weapons
            if (weapon.WeenieType != ACE.Entity.Enum.WeenieType.Caster)
            {
                if (wielder.EnchantmentManager.GetAttackMod() != 0)
                    highlightMask |= WeaponMask.AttackSkill;
                if (wielder.EnchantmentManager.GetWeaponSpeedMod() != 0)
                    highlightMask |= WeaponMask.Speed;
                if (wielder.EnchantmentManager.GetDamageMod() != 0)
                    highlightMask |= WeaponMask.Damage;
                if (wielder.EnchantmentManager.GetVarianceMod() != 1.0f)
                    highlightMask |= WeaponMask.DamageVariance;
                if (wielder.EnchantmentManager.GetDamageModifier() != 1.0f)
                    highlightMask |= WeaponMask.DamageMod;
            }

            return highlightMask;
        }

        public static WeaponMask GetColorMask(WorldObject weapon, WorldObject wielder)
        {
            WeaponMask colorMask = 0;

            if (wielder == null)
                return colorMask;

            // Enchant applies to all weapons
            if (wielder.EnchantmentManager.GetDefenseMod() > 0)
                colorMask |= WeaponMask.MeleeDefense;

            // Following enchants do not apply to caster weapons
            if (weapon.WeenieType != ACE.Entity.Enum.WeenieType.Caster)
            {
                // item enchanments are currently being cast on wielder
                if (wielder.EnchantmentManager.GetAttackMod() > 0)
                    colorMask |= WeaponMask.AttackSkill;
                if (wielder.EnchantmentManager.GetWeaponSpeedMod() < 0)
                    colorMask |= WeaponMask.Speed;
                if (wielder.EnchantmentManager.GetDamageMod() > 0)
                    colorMask |= WeaponMask.Damage;
                if (wielder.EnchantmentManager.GetVarianceMod() < 1.0f)
                    colorMask |= WeaponMask.DamageVariance;
                if (wielder.EnchantmentManager.GetDamageModifier() > 1.0f)
                    colorMask |= WeaponMask.DamageMod;
            }

            return colorMask;
        }
    }
}
