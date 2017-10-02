using System;
using System.Globalization;
using Flir.Atlas.Live.Device;

namespace SebastianHaeni.ThermoBox.IRReader.DeviceParameters
{
    [Serializable]
    internal class DeviceParameter
    {
        public string Name { get; private set; }
        public string Description { get; private set; }
        public string Value { get; private set; }

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

        private static string ParseValue(GenICamParameter param)
        {
            // Who came up with this? And why did nobody like Pleora or FLIR wrap this in a library?

            switch (param)
            {
                case GenICamInteger _:
                    return GetValueSafe(param as GenICamInteger, (p) => p.Value.ToString());
                case GenICamBoolean _:
                    return GetValueSafe(param as GenICamBoolean, (p) => p.Value.ToString());
                case GenICamString _:
                    return GetValueSafe(param as GenICamString, (p) => p.Value);
                case GenICamFloat _:
                    return GetValueSafe(param as GenICamFloat, (p) => p.Value.ToString(CultureInfo.InvariantCulture));
                case GenICamEnum _:
                    return GetValueSafe(param as GenICamEnum, (p) => p.Value.ToString());
            }

            throw new InvalidOperationException("Cannot get value of parameter without value");
        }

        private static string GetValueSafe<T>(T param, Func<T, string> unsafeGetter)
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
