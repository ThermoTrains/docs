using System;
using System.Reflection;

namespace SebastianHaeni.ThermoBox.Common.Motion
{
    internal static class DetectorExtensions
    {
        public static DetectorState[] GetStates(this DetectorState state)
        {
            return GetAttr(state).States;
        }

        private static NextStates GetAttr(DetectorState state)
        {
            return (NextStates) Attribute.GetCustomAttribute(ForValue(state), typeof(NextStates));
        }

        private static MemberInfo ForValue(DetectorState state)
        {
            return typeof(DetectorState).GetField(Enum.GetName(typeof(DetectorState), state));
        }
    }
}
