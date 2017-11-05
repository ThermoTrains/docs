using System;
using System.Drawing;
using System.Reflection;

namespace SebastianHaeni.ThermoBox.Common.Motion
{
    internal static class DetectorExtensions
    {
        public static Evaluator GetEvaluator(this DetectorState state, Size size)
        {
            var nextStates = GetAttr(state);

            return new Evaluator(nextStates.States, size);
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
