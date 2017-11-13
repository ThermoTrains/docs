using log4net;
using System;
using System.Reflection;
using HidLibrary;
using System.IO;
using System.Configuration;
using SebastianHaeni.ThermoBox.Common.Component;

namespace SebastianHaeni.ThermoBox.TemperatureReader.Temper
{
    internal class TemperComponent : ThermoBoxComponent
    {
        private static readonly ILog Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        private const double CalibrationOffset = -1.70;
        private const double CalibrationScale = 1;
        private static readonly string CaptureFolder = ConfigurationManager.AppSettings["CAPTURE_FOLDER"];

        private readonly HidDevice _bulk;
        private string _filename;

        public TemperComponent((HidDevice control, HidDevice bulk) temper)
        {
            _bulk = temper.bulk;

            Subscription(Commands.CaptureStart, (channel, message) => { _filename = message; });
            Subscription(Commands.CaptureStop, (channel, message) => { ReadTemperature(_filename); });
        }

        private void ReadTemperature(string message)
        {
            if (!_bulk.IsOpen)
            {
                Log.Error("Bulk interface is not open");
                Environment.Exit(1);
            }

            var outData = _bulk.CreateReport();
            outData.ReportId = 0x00;
            outData.Data = TemperCommands.Temp;
            _bulk.WriteReport(outData);

            while (outData.ReadStatus == HidDeviceData.ReadStatus.NoDataRead)
            {
                // query data
            }

            _bulk.ReadReport(report =>
            {
                var rawReading = (report.Data[3] & 0xFF) + (report.Data[2] << 8);

                var temperatureCelsius = CalibrationScale * (rawReading * (125.0 / 32000.0)) + CalibrationOffset;
                Log.Info($"Read {temperatureCelsius}°C from device");

                // ensuring the recordings directory exists
                var recordingDirectory = new DirectoryInfo(CaptureFolder);
                if (!recordingDirectory.Exists)
                {
                    recordingDirectory.Create();
                }

                var filename = $@"{CaptureFolder}\{message}-temperature.txt";

                var file = new StreamWriter(filename);
                file.WriteLine(temperatureCelsius);
                file.Close();

                Log.Info($"Written temperature to {filename}");

                Publish(Commands.Upload, filename);
            });
        }
    }
}
