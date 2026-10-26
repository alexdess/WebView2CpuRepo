using System.Diagnostics;
using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;
using Microsoft.Web.WebView2.Core;

namespace WebView2CpuRepro;

public partial class MainWindow : Window
{
    private const int MEASURE_SECONDS = 30;
    private const int PREPARE_SECONDS = 5;
    private const int AUTO_MEASURE_SECONDS = 20;
    private const int AUTO_LOAD_SECONDS = 5;

    private static readonly string _resultsFile = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "WebView2CpuRepro",
        "results.txt");

    private readonly Process _process = Process.GetCurrentProcess();
    private readonly List<HostWindow> _openWindows = [];
    private readonly DispatcherTimer _cpuTimer = new() { Interval = TimeSpan.FromSeconds(1) };
    private TimeSpan _lastCpu;
    private DateTime _lastSample;

    public MainWindow()
    {
        InitializeComponent();
        _environment.Text = DescribeEnvironment();
        Directory.CreateDirectory(Path.GetDirectoryName(_resultsFile)!);
        if (File.Exists(_resultsFile))
        {
            _results.Text = File.ReadAllText(_resultsFile);
        }

        _lastCpu = CurrentCpu();
        _lastSample = DateTime.UtcNow;
        _cpuTimer.Tick += (_, _) => _cpu.Text = $"{SampleCpuPercent():0.0} %";
        _cpuTimer.Start();
        Closed += (_, _) => Application.Current.Shutdown();
    }

    private static string DescribeEnvironment()
    {
        string runtime;
        try
        {
            runtime = CoreWebView2Environment.GetAvailableBrowserVersionString();
        }
        catch (Exception ex)
        {
            runtime = $"not found ({ex.GetType().Name})";
        }

        return string.Join(Environment.NewLine,
            $"Machine          : {Environment.ProcessorCount} cores, {Environment.OSVersion.VersionString}",
            $"RDP session      : {(SystemParameters.IsRemoteSession ? "yes" : "no")}",
            $"WPF render tier  : {RenderCapability.Tier >> 16} (0 = software, 2 = GPU)",
            $"Forced rendering : {(App.ForcedSoftwareRendering ? "software (--software)" : "no")}",
            $"WebView2 runtime : {runtime}",
            $".NET             : {Environment.Version}");
    }

    private TimeSpan CurrentCpu()
    {
        _process.Refresh();
        return _process.TotalProcessorTime;
    }

    /// <summary>CPU since the previous sample, as a share of all cores, the way Task Manager shows it.</summary>
    private double SampleCpuPercent()
    {
        var cpu = CurrentCpu();
        var now = DateTime.UtcNow;
        var percent = (cpu - _lastCpu).TotalMilliseconds / (now - _lastSample).TotalMilliseconds / Environment.ProcessorCount * 100;
        _lastCpu = cpu;
        _lastSample = now;
        return percent;
    }

    private void OnOpenClick(object sender, RoutedEventArgs e) =>
        Open(Enum.Parse<HostMode>((string)((FrameworkElement)sender).Tag));

    private HostWindow Open(HostMode mode)
    {
        var window = new HostWindow(mode);
        window.Closed += (_, _) => _openWindows.Remove(window);
        _openWindows.Add(window);
        window.Show();
        return window;
    }

    private async void OnMeasureClick(object sender, RoutedEventArgs e)
    {
        _measureButton.IsEnabled = false;
        _runAllButton.IsEnabled = false;
        try
        {
            // Clicking the button activates this window; the delay lets the tester put the
            // test window back in the state to measure (front, minimized...).
            for (var remaining = PREPARE_SECONDS; remaining > 0; remaining--)
            {
                _measureButton.Content = $"Starting in {remaining} s";
                await Task.Delay(1000);
            }

            await MeasureAsync(MEASURE_SECONDS, _measureButton);
        }
        finally
        {
            _measureButton.Content = $"Measure {MEASURE_SECONDS} s";
            _measureButton.IsEnabled = true;
            _runAllButton.IsEnabled = true;
        }
    }

    private async void OnRunAllClick(object sender, RoutedEventArgs e) => await RunAllAsync(exitWhenDone: false);

    /// <summary>
    /// Opens each host in turn on the same content, measures it in front then minimized, and closes it.
    /// </summary>
    public async Task RunAllAsync(bool exitWhenDone)
    {
        _measureButton.IsEnabled = false;
        _runAllButton.IsEnabled = false;
        try
        {
            CloseTestWindows();
            await MeasureAsync(AUTO_MEASURE_SECONDS, _runAllButton);

            foreach (var mode in Enum.GetValues<HostMode>())
            {
                var window = Open(mode);
                await Task.Delay(TimeSpan.FromSeconds(AUTO_LOAD_SECONDS));
                window.Activate();
                await MeasureAsync(AUTO_MEASURE_SECONDS, _runAllButton);

                window.WindowState = WindowState.Minimized;
                await Task.Delay(TimeSpan.FromSeconds(2));
                await MeasureAsync(AUTO_MEASURE_SECONDS, _runAllButton);

                window.Close();
                await Task.Delay(TimeSpan.FromSeconds(AUTO_LOAD_SECONDS));
            }

            AppendResult("--- end of automatic run ---");
        }
        finally
        {
            _runAllButton.Content = "Measure all (auto, ~4 min)";
            _measureButton.IsEnabled = true;
            _runAllButton.IsEnabled = true;
        }

        if (exitWhenDone)
        {
            Close();
        }
    }

    private void CloseTestWindows()
    {
        foreach (var window in _openWindows.ToList())
        {
            window.Close();
        }
    }

    private async Task MeasureAsync(int seconds, System.Windows.Controls.Button progress)
    {
        var scenario = _openWindows.Count == 0
            ? "no test window"
            : string.Join(" + ", _openWindows.Select(w => $"{w.Mode.Label()} [{DescribeState(w)}]"));
        var startCpu = CurrentCpu();
        var start = DateTime.UtcNow;

        for (var remaining = seconds; remaining > 0; remaining--)
        {
            progress.Content = $"Measuring... {remaining} s";
            await Task.Delay(1000);
        }

        var average = (CurrentCpu() - startCpu).TotalMilliseconds / (DateTime.UtcNow - start).TotalMilliseconds / Environment.ProcessorCount * 100;
        AppendResult($"{DateTime.Now:yyyy-MM-dd HH:mm:ss}  {average,5:0.0} %  {scenario}");
    }

    private void AppendResult(string line)
    {
        if (_results.Text.Length == 0)
        {
            File.AppendAllText(_resultsFile, _environment.Text + Environment.NewLine + Environment.NewLine);
        }

        File.AppendAllText(_resultsFile, line + Environment.NewLine);
        _results.Text = File.ReadAllText(_resultsFile);
        _results.ScrollToEnd();
    }

    private static string DescribeState(HostWindow window) =>
        window.WindowState == WindowState.Minimized ? "minimized"
        : window.IsActive ? "foreground"
        : "open, not focused";

    private void OnCopyClick(object sender, RoutedEventArgs e) => Clipboard.SetText(_results.Text);

    private void OnOpenFolderClick(object sender, RoutedEventArgs e) =>
        Process.Start(new ProcessStartInfo("explorer.exe", Path.GetDirectoryName(_resultsFile)!) { UseShellExecute = true });
}
