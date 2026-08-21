using System.Collections.ObjectModel;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

namespace Mikrofine.HidExplorer;

public partial class MainWindow : Window
{
    private readonly HidService _hid = new();
    private ObservableCollection<HidDeviceInfo> _devices = [];
    private CancellationTokenSource? _monitorCancellation;
    private int _reportCount;

    public MainWindow()
    {
        InitializeComponent();
        Loaded += (_, _) => RefreshDevices();
        Closed += (_, _) => StopMonitoring();
    }

    private void Refresh_Click(object sender, RoutedEventArgs e) => RefreshDevices();

    private void RefreshDevices()
    {
        StopMonitoring();
        _devices = _hid.Enumerate();
        DeviceList.ItemsSource = _devices;
        StatusText.Text = $"{_devices.Count} HID devices found";
        if (_devices.Count > 0) DeviceList.SelectedIndex = 0;
    }

    private void DeviceList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        StopMonitoring();
        ReportList.Items.Clear();
        _reportCount = 0;

        if (DeviceList.SelectedItem is not HidDeviceInfo device)
        {
            DetailsText.Text = string.Empty;
            return;
        }

        DetailsText.Text = BuildDetails(device);
    }

    private async void Monitor_Click(object sender, RoutedEventArgs e)
    {
        if (_monitorCancellation is not null)
        {
            StopMonitoring();
            return;
        }

        if (DeviceList.SelectedItem is not HidDeviceInfo device)
        {
            StatusText.Text = "Select a HID device first.";
            return;
        }

        _monitorCancellation = new CancellationTokenSource();
        MonitorButton.Content = "Stop Monitor";
        StatusText.Text = $"Monitoring {device.DisplayName}";

        try
        {
            await _hid.MonitorInputAsync(device.DevicePath, report =>
            {
                Dispatcher.Invoke(() => AddReport(report));
            }, _monitorCancellation.Token);
        }
        catch (OperationCanceledException) { }
        catch (Exception ex)
        {
            Dispatcher.Invoke(() => StatusText.Text = $"Monitor error: {ex.Message}");
        }
        finally
        {
            if (_monitorCancellation is not null)
            {
                _monitorCancellation.Dispose();
                _monitorCancellation = null;
            }
            MonitorButton.Content = "Start Monitor";
        }
    }

    private void StopMonitoring()
    {
        _monitorCancellation?.Cancel();
        _monitorCancellation = null;
        MonitorButton.Content = "Start Monitor";
    }

    private void AddReport(byte[] report)
    {
        _reportCount++;
        var hex = Convert.ToHexString(report);
        ReportList.Items.Insert(0, $"{DateTime.Now:HH:mm:ss.fff}  #{_reportCount:D6}  {hex}");
        while (ReportList.Items.Count > 1000) ReportList.Items.RemoveAt(ReportList.Items.Count - 1);
        StatusText.Text = $"Monitoring — {_reportCount:N0} reports";
    }

    private static string BuildDetails(HidDeviceInfo d)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"Product        : {d.Product}");
        sb.AppendLine($"Manufacturer   : {d.Manufacturer}");
        sb.AppendLine($"VID:PID        : {d.VendorProductId}");
        sb.AppendLine($"Serial         : {d.SerialNumber}");
        sb.AppendLine($"Input report   : {d.InputReportLength} bytes");
        sb.AppendLine($"Output report  : {d.OutputReportLength} bytes");
        sb.AppendLine($"Feature report : {d.FeatureReportLength} bytes");
        sb.AppendLine($"Device path    : {d.DevicePath}");
        sb.AppendLine();
        sb.AppendLine("HID Report Descriptor:");
        sb.AppendLine(FormatHex(d.ReportDescriptor));
        return sb.ToString();
    }

    private static string FormatHex(byte[] data)
    {
        if (data.Length == 0) return "<not available>";
        var sb = new StringBuilder();
        for (var i = 0; i < data.Length; i += 16)
        {
            var count = Math.Min(16, data.Length - i);
            sb.Append(i.ToString("X4")).Append("  ");
            for (var j = 0; j < count; j++) sb.Append(data[i + j].ToString("X2")).Append(' ');
            sb.AppendLine();
        }
        return sb.ToString();
    }
}
