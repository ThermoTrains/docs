using HidLibrary;
using log4net;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace SebastianHaeni.ThermoBox.TemperatureReader.Temper
{
    internal class TemperDiscover
    {
        private static readonly ILog Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        private readonly TaskCompletionSource<(HidDevice control, HidDevice bulk)> _deviceInitialized =
            new TaskCompletionSource<(HidDevice control, HidDevice bulk)>();

        private HidDevice _control;
        private HidDevice _bulk;

        public Task<(HidDevice control, HidDevice bulk)> DiscoverTemper()
        {
            // Find all interfaces of the Temper device
            var temperInterfaces = (from device in HidDevices.Enumerate()
                where device.Attributes.ProductHexId == "0x7401" & device.Attributes.VendorHexId == "0x0C45"
                select device);

            // Find control interface
            var hidDevices = temperInterfaces as HidDevice[] ?? temperInterfaces.ToArray();

            _control = (from intf in hidDevices
                where intf.DevicePath.Contains("mi_00")
                select intf).First();

            // Find bulk interface
            _bulk = (from intf in hidDevices
                where intf.DevicePath.Contains("mi_01")
                select intf).First();

            // Connect event handlers
            _control.Inserted += Device_Inserted;
            _control.Removed += Device_Removed;
            _control.MonitorDeviceEvents = true;

            return _deviceInitialized.Task;
        }

        private void Device_Inserted()
        {
            //claim interfaces:
            _control.OpenDevice();
            _bulk.OpenDevice();

            _control.ReadManufacturer(out var manufacturerRaw);
            _control.ReadProduct(out var productRaw);
            _control.ReadSerialNumber(out var serialRaw);
            var manufacturer = Encoding.UTF8
                .GetString(Encoding.Convert(Encoding.Unicode, Encoding.ASCII, manufacturerRaw)).TrimEnd('\0');
            var product = Encoding.UTF8.GetString(Encoding.Convert(Encoding.Unicode, Encoding.ASCII, productRaw))
                .TrimEnd('\0');
            var serial = Encoding.UTF8.GetString(Encoding.Convert(Encoding.Unicode, Encoding.ASCII, serialRaw))
                .TrimEnd('\0');

            Log.Info($"Connected to device: {manufacturer}, {product}, {serial}");

            var outData1 = _control.CreateReport();
            outData1.ReportId = 0x01;
            outData1.Data = TemperCommands.Ini;
            _control.WriteReport(outData1);
            while (outData1.ReadStatus != HidDeviceData.ReadStatus.Success)
            {
                // wait for success
            }
            _control.ReadReport((report) => { });

            var outData3 = _bulk.CreateReport();
            outData3.ReportId = 0x00;
            outData3.Data = TemperCommands.Ini1;
            _bulk.WriteReport(outData3);
            while (outData3.ReadStatus != HidDeviceData.ReadStatus.Success)
            {
                // wait for success
            }
            _bulk.ReadReport((report) => { });

            var outData4 = _bulk.CreateReport();
            outData4.ReportId = 0x00;
            outData4.Data = TemperCommands.Ini2;
            _bulk.WriteReport(outData4);
            while (outData4.ReadStatus != HidDeviceData.ReadStatus.Success)
            {
                // wait for success
            }
            _bulk.ReadReport((report) => { });

            // Clear out garbage from device
            var outData2 = _bulk.CreateReport();
            outData2.ReportId = 0x00;
            outData2.Data = TemperCommands.Temp;
            _bulk.WriteReport(outData2);
            while (outData2.ReadStatus != HidDeviceData.ReadStatus.Success)
            {
                // wait for success
            }
            _bulk.ReadReport((report) => { });
            _bulk.WriteReport(outData2);
            while (outData2.ReadStatus != HidDeviceData.ReadStatus.Success)
            {
                // wait for success
            }
            _bulk.ReadReport((report) => { });

            _deviceInitialized.SetResult((control: _control, bulk: _bulk));
        }

        private void Device_Removed()
        {
            _bulk.CloseDevice();
            _control.CloseDevice();
        }
    }
}
