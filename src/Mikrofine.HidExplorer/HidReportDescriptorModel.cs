namespace Mikrofine.HidExplorer;

public sealed record HidReportField(int ReportId, int UsagePage, IReadOnlyList<int> Usages, int BitOffset, int BitSize, int ReportCount, int LogicalMinimum, int LogicalMaximum, byte InputFlags, string CollectionPath)
{
    public bool IsVariable => (InputFlags & 0x02) != 0;
    public bool IsConstant => (InputFlags & 0x01) != 0;
    public string UsageSummary => Usages.Count == 0 ? $"Page 0x{UsagePage:X2}" : string.Join(", ", Usages.Select(u => $"0x{u:X2}"));
    public string Kind => UsagePage switch
    {
        HidUsagePages.Button => "Button",
        HidUsagePages.Keyboard => "Keyboard",
        HidUsagePages.Consumer => "Consumer Control",
        HidUsagePages.GenericDesktop => Usages.FirstOrDefault() switch
        {
            HidUsage.GenericDesktopX => "X Axis",
            HidUsage.GenericDesktopY => "Y Axis",
            HidUsage.GenericDesktopWheel => "Wheel",
            HidUsage.GenericDesktopMouse => "Mouse",
            _ => "Generic Desktop"
        },
        _ => "Input"
    };
}

public sealed class HidReportDescriptorModel
{
    public IReadOnlyList<HidReportField> InputFields { get; init; } = [];
    public IReadOnlyList<HidReportField> OutputFields { get; init; } = [];
    public IReadOnlyList<HidReportField> FeatureFields { get; init; } = [];
    public IReadOnlyList<string> Collections { get; init; } = [];
}

public static class HidUsagePages
{
    public const int GenericDesktop = 0x01;
    public const int Keyboard = 0x07;
    public const int Button = 0x09;
    public const int Consumer = 0x0C;
}

public static class HidUsage
{
    public const int GenericDesktopMouse = 0x02;
    public const int GenericDesktopX = 0x30;
    public const int GenericDesktopY = 0x31;
    public const int GenericDesktopWheel = 0x38;
}
