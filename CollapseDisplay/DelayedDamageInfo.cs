using CollapseDisplay.Networking;
using RoR2;
using System;

namespace CollapseDisplay
{
    public struct DelayedDamageInfo : IEquatable<DelayedDamageInfo>
    {
        public static readonly DelayedDamageInfo None = new DelayedDamageInfo(-1f, Run.FixedTimeStamp.negativeInfinity);

        public float Damage;
        public Net_RunFixedTimeStampWrapper Wrap_DamageTimestamp;

        public Run.FixedTimeStamp DamageTimestamp
        {
            readonly get => Wrap_DamageTimestamp;
            set => Wrap_DamageTimestamp = value;
        }

        public DelayedDamageInfo()
        {
        }

        public DelayedDamageInfo(float damage, Run.FixedTimeStamp damageTimestamp)
        {
            Damage = damage;
            Wrap_DamageTimestamp = damageTimestamp;
        }

        public readonly bool Equals(DelayedDamageInfo other)
        {
            return Damage == other.Damage && Wrap_DamageTimestamp.Equals(other.Wrap_DamageTimestamp);
        }
    }
}
