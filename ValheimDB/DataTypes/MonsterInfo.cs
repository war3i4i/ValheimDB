using System;
using UnityEngine;

namespace ValheimDB.DataTypes;

public class MonsterInfoWrapper
{
    [Flags]
    private enum MonsterInfoFlags
    {
        None = 0,
        HasName = 1 << 0,
        HasHealth = 1 << 1,
        HasWalkSpeed = 1 << 2,
        HasSwimSpeed = 1 << 3,
        HasDamageMultiplier = 1 << 4,
        HasCloneSource = 1 << 5
    }

    private static bool HasFlagFast(MonsterInfoFlags value, MonsterInfoFlags flag) => (value & flag) != 0;

    public class MonsterInfo : ISerializableParameter
    {
        public string Name;
        public int? Health;
        public float? WalkSpeed;
        public float? SwimSpeed;
        public float? DamageMultiplier;
        public string CloneSource;

        public void Serialize(ref ZPackage pkg)
        {
            MonsterInfoFlags flags = MonsterInfoFlags.None;

            if (!string.IsNullOrEmpty(Name)) flags |= MonsterInfoFlags.HasName;
            if (Health.HasValue) flags |= MonsterInfoFlags.HasHealth;
            if (WalkSpeed.HasValue) flags |= MonsterInfoFlags.HasWalkSpeed;
            if (SwimSpeed.HasValue) flags |= MonsterInfoFlags.HasSwimSpeed;
            if (DamageMultiplier.HasValue) flags |= MonsterInfoFlags.HasDamageMultiplier;
            if (!string.IsNullOrEmpty(CloneSource)) flags |= MonsterInfoFlags.HasCloneSource;

            pkg.Write((int)flags);

            if (HasFlagFast(flags, MonsterInfoFlags.HasName)) pkg.Write(Name);
            if (HasFlagFast(flags, MonsterInfoFlags.HasHealth)) pkg.Write(Health.Value);
            if (HasFlagFast(flags, MonsterInfoFlags.HasWalkSpeed)) pkg.Write(WalkSpeed.Value);
            if (HasFlagFast(flags, MonsterInfoFlags.HasSwimSpeed)) pkg.Write(SwimSpeed.Value);
            if (HasFlagFast(flags, MonsterInfoFlags.HasDamageMultiplier)) pkg.Write(DamageMultiplier.Value);
            if (HasFlagFast(flags, MonsterInfoFlags.HasCloneSource)) pkg.Write(CloneSource);
        }

        public void Deserialize(ref ZPackage pkg)
        {
            MonsterInfoFlags flags = (MonsterInfoFlags)pkg.ReadInt();

            if (HasFlagFast(flags, MonsterInfoFlags.HasName)) Name = pkg.ReadString();
            if (HasFlagFast(flags, MonsterInfoFlags.HasHealth)) Health = pkg.ReadInt();
            if (HasFlagFast(flags, MonsterInfoFlags.HasWalkSpeed)) WalkSpeed = pkg.ReadSingle();
            if (HasFlagFast(flags, MonsterInfoFlags.HasSwimSpeed)) SwimSpeed = pkg.ReadSingle();
            if (HasFlagFast(flags, MonsterInfoFlags.HasDamageMultiplier)) DamageMultiplier = pkg.ReadSingle();
            if (HasFlagFast(flags, MonsterInfoFlags.HasCloneSource)) CloneSource = pkg.ReadString();
        }

        public static implicit operator MonsterInfo(Character c)
        {
            if (c == null) return null;
            MonsterInfo info = new MonsterInfo
            {
                Name = c.m_name,
                Health = (int)c.m_health,
                WalkSpeed = c.m_walkSpeed,
                SwimSpeed = c.m_swimSpeed
            };
            return info;
        }

        public void Apply(ZNetScene zns, Character c)
        {
            if (!string.IsNullOrEmpty(Name)) c.m_name = Name;
            if (Health.HasValue) c.m_health = Health.Value;
            if (WalkSpeed.HasValue) c.m_walkSpeed = WalkSpeed.Value;
            if (SwimSpeed.HasValue) c.m_swimSpeed = SwimSpeed.Value;
        }
    }
}
