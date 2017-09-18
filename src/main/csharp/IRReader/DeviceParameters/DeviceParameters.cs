using System;
using System.Collections.Generic;

namespace SebastianHaeni.ThermoBox.IRReader.DeviceParameters
{
    [Serializable]
    class DeviceParameters
    {
        public IEnumerable<DeviceParameter> Parameters { get; set; }
    }
}
