namespace Mikrofine.HidExplorer;

public static class HidReportDescriptorParser
{
    private sealed record GlobalState(int UsagePage, int LogicalMinimum, int LogicalMaximum, int ReportSize, int ReportCount, int ReportId);
    private sealed class CollectionState(int usagePage, int usage) { public int UsagePage { get; } = usagePage; public int Usage { get; } = usage; }

    public static HidReportDescriptorModel Parse(ReadOnlySpan<byte> descriptor)
    {
        var inputs = new List<HidReportField>();
        var outputs = new List<HidReportField>();
        var features = new List<HidReportField>();
        var collections = new List<string>();
        var globals = new Stack<GlobalState>();
        var collectionStack = new Stack<CollectionState>();
        var reportBits = new Dictionary<int, int>();
        var state = new GlobalState(0, 0, 0, 0, 0, 0);
        var localUsages = new List<int>();
        int? usageMin = null, usageMax = null;

        for (var i = 0; i < descriptor.Length;)
        {
            var prefix = descriptor[i++];
            if (prefix == 0xFE)
            {
                if (i + 1 >= descriptor.Length) break;
                var size = descriptor[i++]; i++;
                i = Math.Min(descriptor.Length, i + size);
                continue;
            }

            var dataSize = (prefix & 0x03) switch { 0 => 0, 1 => 1, 2 => 2, _ => 4 };
            var type = (prefix >> 2) & 0x03;
            var tag = (prefix >> 4) & 0x0F;
            if (i + dataSize > descriptor.Length) break;
            var data = descriptor.Slice(i, dataSize); i += dataSize;
            var u = ReadUnsigned(data); var s = ReadSigned(data);

            if (type == 1)
            {
                state = tag switch
                {
                    0x0 => state with { UsagePage = (int)u },
                    0x1 => state with { LogicalMinimum = s },
                    0x2 => state with { LogicalMaximum = s },
                    0x7 => state with { ReportSize = (int)u },
                    0x8 => state with { ReportId = (int)u },
                    0x9 => state with { ReportCount = (int)u },
                    0xA => PushGlobal(globals, state),
                    0xB => globals.Count == 0 ? state : globals.Pop(),
                    _ => state
                };
                continue;
            }

            if (type == 2)
            {
                if (tag == 0x0) localUsages.Add((int)u);
                else if (tag == 0x1) usageMin = (int)u;
                else if (tag == 0x2) usageMax = (int)u;
                continue;
            }

            if (type != 0) continue;
            if (tag == 0x8 || tag == 0x9 || tag == 0xB)
            {
                var flags = (byte)u;
                reportBits.TryGetValue(state.ReportId, out var bitOffset);
                var usages = ExpandUsages(localUsages, usageMin, usageMax, state.ReportCount);
                var path = string.Join(" > ", collectionStack.Reverse().Select(c => $"Page 0x{c.UsagePage:X2} / Usage 0x{c.Usage:X2}"));
                var field = new HidReportField(state.ReportId, state.UsagePage, usages, bitOffset, state.ReportSize, state.ReportCount, state.LogicalMinimum, state.LogicalMaximum, flags, path);
                if (tag == 0x8) inputs.Add(field); else if (tag == 0x9) outputs.Add(field); else features.Add(field);
                reportBits[state.ReportId] = bitOffset + state.ReportSize * state.ReportCount;
                localUsages.Clear(); usageMin = usageMax = null;
            }
            else if (tag == 0xA)
            {
                var usage = localUsages.LastOrDefault();
                collectionStack.Push(new CollectionState(state.UsagePage, usage));
                collections.Add($"Page 0x{state.UsagePage:X2} / Usage 0x{usage:X2}");
                localUsages.Clear(); usageMin = usageMax = null;
            }
            else if (tag == 0xC && collectionStack.Count > 0)
            {
                collectionStack.Pop(); localUsages.Clear(); usageMin = usageMax = null;
            }
        }

        return new HidReportDescriptorModel { InputFields = inputs, OutputFields = outputs, FeatureFields = features, Collections = collections.Distinct().ToArray() };
    }

    private static GlobalState PushGlobal(Stack<GlobalState> stack, GlobalState state) { stack.Push(state); return state; }
    private static List<int> ExpandUsages(List<int> explicitUsages, int? min, int? max, int count)
    {
        if (explicitUsages.Count > 0) return explicitUsages.ToList();
        if (min.HasValue && max.HasValue && max >= min) return Enumerable.Range(min.Value, max.Value - min.Value + 1).Take(Math.Max(1, count)).ToList();
        return [];
    }
    private static ulong ReadUnsigned(ReadOnlySpan<byte> data) { ulong v = 0; for (var i = 0; i < data.Length; i++) v |= (ulong)data[i] << (8 * i); return v; }
    private static int ReadSigned(ReadOnlySpan<byte> data)
    {
        if (data.Length == 0) return 0;
        var v = (int)ReadUnsigned(data); var bits = data.Length * 8;
        if (bits < 32 && (v & (1 << (bits - 1))) != 0) v |= -1 << bits;
        return v;
    }
}
