namespace Mikrofine.HidExplorer;

public sealed class HidDeviceInfo
{
    public string DevicePath { get; init; } = string.Empty;
    public int VendorId { get; init; }
    public int ProductId { get; init; }
    public string Manufacturer { get; init; } = string.Empty;
    public string Product { get; init; } = string.Empty;
    public string SerialNumber { get; init; } = string.Empty;
    public int InputReportLength { get; init; }
    public int OutputReportLength { get; init; }
    public int FeatureReportLength { get; init; }
    public string UsagePage { get; init; } = "Unknown";
    public string Usage { get; init; } = "Unknown";
    public byte[] ReportDescriptor { get; init; } = [];

    public string DisplayName => string.IsNullOrWhiteSpace(Product)
        ? $"HID {VendorId:X4}:{ProductId:X4}"
        : Product;

    public string VendorProductId => $"{VendorId:X4}:{ProductId:X4}";
}
