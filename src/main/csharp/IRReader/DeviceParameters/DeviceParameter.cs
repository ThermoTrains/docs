using System;
using Flir.Atlas.Live.Device;

namespace SebastianHaeni.ThermoBox.IRReader.DeviceParameters
{
    [Serializable]
    class DeviceParameter
    {
        public String Name { get; private set; }
        public String Description { get; private set; }
        public String Value { get; private set; }

        public DeviceParameter(GenICamParameter param)
        {
            Name = param.Name;
            Description = param.Description;
            Value = ParseValue(param);
        }

        internal static bool HasValue(GenICamParameter param)
        {
            return !(param is GenICamCommand);
        }

        private string ParseValue(GenICamParameter param)
        {
            // Who came up with this? And why did nobody like Pleora or FLIR wrap this in a library?

            if (param is GenICamInteger)
            {
                return GetValueSafe(param as GenICamInteger, (p) => p.Value.ToString());
            }

            if (param is GenICamBoolean)
            {
                return GetValueSafe(param as GenICamBoolean, (p) => p.Value.ToString());
            }

            if (param is GenICamString)
            {
                return GetValueSafe(param as GenICamString, (p) => p.Value);
            }

            if (param is GenICamFloat)
            {
                return GetValueSafe(param as GenICamFloat, (p) => p.Value.ToString());
            }

            if (param is GenICamEnum)
            {
                return GetValueSafe(param as GenICamEnum, (p) => p.Value.ToString());
            }

            throw new InvalidOperationException("Cannot get value of parameter without value");
        }

        private string GetValueSafe<T>(T param, Func<T, string> unsafeGetter)
        {
            // Exceptions can be thrown any time. This API is so great...
            try
            {
                return unsafeGetter.Invoke(param);
            }
            catch (Exception ex)
            {
                // Display error message
                return ex.Message;
            }
        }
    }
}
