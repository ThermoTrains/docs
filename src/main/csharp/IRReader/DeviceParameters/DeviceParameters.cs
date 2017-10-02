using System;
using System.Collections.Generic;

namespace SebastianHaeni.ThermoBox.IRReader.DeviceParameters
{
    [Serializable]
    internal class DeviceParameters
    {
        public IEnumerable<DeviceParameter> Parameters { get; set; }
    }
}
