using HidSharp;
using System.Collections.ObjectModel;
using System.Management;

namespace Mikrofine.HidExplorer;

public sealed class HidService
{
    private readonly HidDeviceLoader _loader = new();

    public ObservableCollection<HidDeviceInfo> Enumerate()
    {
        var result = new ObservableCollection<HidDeviceInfo>();

        foreach (var device in _loader.GetDevices())
        {
            try
            {
                var descriptor = device.GetRawReportDescriptor() ?? [];
                result.Add(new HidDeviceInfo
                {
                    DevicePath = device.DevicePath,
                    VendorId = device.VendorID,
                    ProductId = device.ProductID,
                    Manufacturer = Safe(() => device.GetManufacturer()),
                    Product = Safe(() => device.GetProductName()),
                    SerialNumber = Safe(() => device.GetSerialNumber()),
                    InputReportLength = device.MaxInputReportLength,
                    OutputReportLength = device.MaxOutputReportLength,
                    FeatureReportLength = device.MaxFeatureReportLength,
                    ReportDescriptor = descriptor
                });
            }
            catch
            {
                // A device may disappear during enumeration. Ignore that entry.
            }
        }

        return result;
    }

    public HidDevice? Find(string devicePath) =>
        _loader.GetDevices().FirstOrDefault(d => string.Equals(d.DevicePath, devicePath, StringComparison.OrdinalIgnoreCase));

    private static string Safe(Func<string?> getter)
    {
        try { return getter() ?? string.Empty; }
        catch { return string.Empty; }
    }
}
