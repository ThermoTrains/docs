using log4net;
using SebastianHaeni.ThermoBox.Common;
using System;
using System.Reflection;
using HidLibrary;
using System.IO;
using System.Configuration;

namespace SebastianHaeni.ThermoBox.TemperatureReader.Temper
{
    class TemperComponent : ThermoBoxComponent
    {
        private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        private static readonly byte[] temp = { 0x01, 0x80, 0x33, 0x01, 0x00, 0x00, 0x00, 0x00 }; // TODO extract these into a separate class
        private static double CalibrationOffset = -1.70;
        private static double CalibrationScale = 1;
        private static readonly string CAPTURE_FOLDER = ConfigurationManager.AppSettings["CAPTURE_FOLDER"];

        private HidDevice control;
        private HidDevice bulk;

        public TemperComponent((HidDevice control, HidDevice bulk) temper)
        {
            control = temper.control;
            bulk = temper.bulk;
        }

        protected override void Configure()
        {
            Subscription(Commands.CaptureStart, (channel, message) =>
            {
                if (!bulk.IsOpen)
                {
                    log.Error("Bulk interface is not open");
                    Environment.Exit(1);
                }

                var outData = bulk.CreateReport();
                outData.ReportId = 0x00;
                outData.Data = temp;
                bulk.WriteReport(outData);

                while (outData.ReadStatus == HidDeviceData.ReadStatus.NoDataRead) ;

                bulk.ReadReport((report) =>
                {
                    int RawReading = (report.Data[3] & 0xFF) + (report.Data[2] << 8);

                    double temperatureCelsius = (CalibrationScale * (RawReading * (125.0 / 32000.0))) + CalibrationOffset;
                    log.Info($"Read {temperatureCelsius}°C from device");

                    // ensuring the recordings directory exists
                    DirectoryInfo recordingDirectory = new DirectoryInfo(CAPTURE_FOLDER);
                    if (!recordingDirectory.Exists)
                    {
                        recordingDirectory.Create();
                    }

                    var filename = $@"{CAPTURE_FOLDER}\{message}-temperature.txt";

                    StreamWriter file = new StreamWriter(filename);
                    file.WriteLine(temperatureCelsius);
                    file.Close();

                    log.Info($"Written temperature to {filename}");

                    Publish(Commands.Upload, filename);
                });
            });
        }
    }
}
