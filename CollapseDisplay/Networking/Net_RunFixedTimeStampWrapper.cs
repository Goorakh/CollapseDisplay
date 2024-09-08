using RoR2;
using System;

namespace CollapseDisplay.Networking
{
    // UNetWeaver doesn't allow types outside the target assembly to be serialized,
    // So for any RoR2 types, we have to make wrappers like this :/
    public struct Net_RunFixedTimeStampWrapper : IEquatable<Net_RunFixedTimeStampWrapper>
    {
        public float t;

        public Net_RunFixedTimeStampWrapper()
        {
        }

        public readonly bool Equals(Net_RunFixedTimeStampWrapper other)
        {
            return t == other.t;
        }

        public static implicit operator Run.FixedTimeStamp(Net_RunFixedTimeStampWrapper wrapper)
        {
            return new Run.FixedTimeStamp(wrapper.t);
        }

        public static implicit operator Net_RunFixedTimeStampWrapper(Run.FixedTimeStamp timeStamp)
        {
            return new Net_RunFixedTimeStampWrapper { t = timeStamp.t };
        }
    }
}
