using HidSharp;
using System.Collections.ObjectModel;

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
                var model = HidReportDescriptorParser.Parse(descriptor);
                var firstInput = model.InputFields.FirstOrDefault(f => !f.IsConstant);
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
                    UsagePage = firstInput is null ? "Unknown" : $"0x{firstInput.UsagePage:X2}",
                    Usage = firstInput is null ? "Unknown" : firstInput.UsageSummary,
                    ReportDescriptor = descriptor,
                    DescriptorModel = model
                });
            }
            catch
            {
                // Devices can disappear during enumeration/hot unplug.
            }
        }
        return result;
    }

    public HidDevice? Find(string devicePath) =>
        _loader.GetDevices().FirstOrDefault(d => string.Equals(d.DevicePath, devicePath, StringComparison.OrdinalIgnoreCase));

    public async Task MonitorInputAsync(string devicePath, Action<byte[]> onReport, CancellationToken cancellationToken)
    {
        var device = Find(devicePath) ?? throw new InvalidOperationException("HID device is no longer available.");
        if (!device.TryOpen(out var stream)) throw new IOException("The HID device could not be opened.");
        using (stream)
        {
            var buffer = new byte[Math.Max(1, device.MaxInputReportLength)];
            while (!cancellationToken.IsCancellationRequested)
            {
                var read = await stream.ReadAsync(buffer, 0, buffer.Length, cancellationToken);
                if (read <= 0) break;
                onReport(buffer[..read]);
            }
        }
    }

    private static string Safe(Func<string?> getter)
    {
        try { return getter() ?? string.Empty; }
        catch { return string.Empty; }
    }
}
