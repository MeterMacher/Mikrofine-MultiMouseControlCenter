namespace Mikrofine.HidExplorer;

public sealed record HidSemanticEvent(DateTimeOffset Timestamp, string Kind, string Description, int? UsagePage = null, int? Usage = null, int? Value = null);

public static class HidReportDecoder
{
    public static IReadOnlyList<HidSemanticEvent> Decode(HidReportDescriptorModel descriptor, ReadOnlySpan<byte> report, DateTimeOffset timestamp)
    {
        var result = new List<HidSemanticEvent>();
        foreach (var field in descriptor.InputFields.Where(f => f.IsVariable && !f.IsConstant))
        {
            var baseBit = field.ReportId == 0 ? field.BitOffset : field.BitOffset + 8;
            for (var index = 0; index < Math.Max(field.ReportCount, 1); index++)
            {
                var bitOffset = baseBit + index * field.BitSize;
                if (bitOffset + field.BitSize > report.Length * 8) continue;
                var value = (int)ReadBits(report, bitOffset, field.BitSize);
                var usage = index < field.Usages.Count ? field.Usages[index] : field.Usages.LastOrDefault();
                if (usage == 0 && field.UsagePage == HidUsagePages.Button) usage = index + 1;

                if (field.UsagePage == HidUsagePages.Button)
                    result.Add(new HidSemanticEvent(timestamp, "Button", $"Button {usage}: {(value != 0 ? "ON" : "OFF")}", field.UsagePage, usage, value));
                else if (field.UsagePage == HidUsagePages.GenericDesktop)
                {
                    var kind = usage switch { HidUsage.GenericDesktopX => "X", HidUsage.GenericDesktopY => "Y", HidUsage.GenericDesktopWheel => "Wheel", _ => $"Usage 0x{usage:X2}" };
                    if (value != 0 || kind is "X" or "Y" or "Wheel") result.Add(new HidSemanticEvent(timestamp, "Axis", $"{kind}: {value}", field.UsagePage, usage, value));
                }
                else if (field.UsagePage == HidUsagePages.Keyboard)
                    result.Add(new HidSemanticEvent(timestamp, "Keyboard", $"Usage 0x{usage:X2}: {value}", field.UsagePage, usage, value));
                else if (field.UsagePage == HidUsagePages.Consumer)
                    result.Add(new HidSemanticEvent(timestamp, "Consumer", $"Usage 0x{usage:X2}: {value}", field.UsagePage, usage, value));
            }
        }
        return result;
    }

    private static uint ReadBits(ReadOnlySpan<byte> data, int bitOffset, int bitCount)
    {
        uint result = 0;
        for (var bit = 0; bit < Math.Min(bitCount, 32); bit++)
        {
            var absolute = bitOffset + bit;
            if ((data[absolute / 8] & (1 << (absolute % 8))) != 0) result |= 1u << bit;
        }
        return result;
    }
}
