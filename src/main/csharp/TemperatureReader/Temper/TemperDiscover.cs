using HidLibrary;
using log4net;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace SebastianHaeni.ThermoBox.TemperatureReader.Temper
{
    class TemperDiscover
    {
        private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        private TaskCompletionSource<(HidDevice control, HidDevice bulk)> deviceInitialized = new TaskCompletionSource<(HidDevice control, HidDevice bulk)>();
        private HidDevice control;
        private HidDevice bulk;

        // commands / magic numbers
        private static readonly byte[] ini = { 0x01, 0x01 };
        private static readonly byte[] temp = { 0x01, 0x80, 0x33, 0x01, 0x00, 0x00, 0x00, 0x00 };
        private static readonly byte[] ini1 = { 0x01, 0x82, 0x77, 0x01, 0x00, 0x00, 0x00, 0x00 };
        private static readonly byte[] ini2 = { 0x01, 0x86, 0xff, 0x01, 0x00, 0x00, 0x00, 0x00 };

        public Task<(HidDevice control, HidDevice bulk)> DiscoverTemper()
        {
            // Find all interfaces of the Temper device
            var temperInterfaces = (from device in HidDevices.Enumerate()
                                    where device.Attributes.ProductHexId == "0x7401" & device.Attributes.VendorHexId == "0x0C45"
                                    select device);

            // Find control interface
            control = (from intf in temperInterfaces
                       where intf.DevicePath.Contains("mi_00")
                       select intf).First();

            // Find bulk interface
            bulk = (from intf in temperInterfaces
                    where intf.DevicePath.Contains("mi_01")
                    select intf).First();

            // Connect event handlers
            control.Inserted += Device_Inserted;
            control.Removed += Device_Removed;
            control.MonitorDeviceEvents = true;

            return deviceInitialized.Task;
        }

        private void Device_Inserted()
        {
            //claim interfaces:
            control.OpenDevice();
            bulk.OpenDevice();
            byte[] ManufacturerRaw = new byte[64];
            byte[] ProductRaw = new byte[64];
            byte[] SerialRaw = new byte[64];


            control.ReadManufacturer(out ManufacturerRaw);
            control.ReadProduct(out ProductRaw);
            control.ReadSerialNumber(out SerialRaw);
            var manufacturer = Encoding.UTF8.GetString(Encoding.Convert(Encoding.Unicode, Encoding.ASCII, ManufacturerRaw)).TrimEnd('\0');
            var product = Encoding.UTF8.GetString(Encoding.Convert(Encoding.Unicode, Encoding.ASCII, ProductRaw)).TrimEnd('\0');
            var serial = Encoding.UTF8.GetString(Encoding.Convert(Encoding.Unicode, Encoding.ASCII, SerialRaw)).TrimEnd('\0');

            log.Info($"Connected to device: {manufacturer}, {product}, {serial}");

            var outData1 = control.CreateReport();
            outData1.ReportId = 0x01;
            outData1.Data = ini;
            control.WriteReport(outData1);
            while (outData1.ReadStatus != HidDeviceData.ReadStatus.Success) ;
            control.ReadReport((report) => { });

            var outData3 = bulk.CreateReport();
            outData3.ReportId = 0x00;
            outData3.Data = ini1;
            bulk.WriteReport(outData3);
            while (outData3.ReadStatus != HidDeviceData.ReadStatus.Success) ;
            bulk.ReadReport((report) => { });

            var outData4 = bulk.CreateReport();
            outData4.ReportId = 0x00;
            outData4.Data = ini2;
            bulk.WriteReport(outData4);
            while (outData4.ReadStatus != HidDeviceData.ReadStatus.Success) ;
            bulk.ReadReport((report) => { });

            // Clear out garbage from device
            var outData2 = bulk.CreateReport();
            outData2.ReportId = 0x00;
            outData2.Data = temp;
            bulk.WriteReport(outData2);
            while (outData2.ReadStatus != HidDeviceData.ReadStatus.Success) ;
            bulk.ReadReport((report) => { });
            bulk.WriteReport(outData2);
            while (outData2.ReadStatus != HidDeviceData.ReadStatus.Success) ;
            bulk.ReadReport((report) => { });

            deviceInitialized.SetResult((control: control, bulk: bulk));
        }

        private void Device_Removed()
        {
            bulk.CloseDevice();
            control.CloseDevice();
        }
    }
}
