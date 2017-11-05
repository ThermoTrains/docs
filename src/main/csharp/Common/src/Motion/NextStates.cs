using System;

namespace SebastianHaeni.ThermoBox.Common.Motion
{
    internal class NextStates : Attribute
    {
        public DetectorState[] States { get; }

        public NextStates(params DetectorState[] states)
        {
            States = states;
        }
    }
}
