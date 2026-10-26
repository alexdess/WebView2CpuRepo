using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;

namespace WebView2CpuRepro;

public partial class App : Application
{
    /// <summary>
    /// True when started with --software: forces WPF software rendering, which is what a
    /// GPU-less RDP session gets, so the RDP behaviour can be reproduced on a dev machine.
    /// </summary>
    public static bool ForcedSoftwareRendering { get; private set; }

    private void OnStartup(object sender, StartupEventArgs e)
    {
        ForcedSoftwareRendering = HasArg(e, "--software");
        if (ForcedSoftwareRendering)
        {
            RenderOptions.ProcessRenderMode = RenderMode.SoftwareOnly;
        }

        var main = new MainWindow();
        main.Show();
        if (HasArg(e, "--auto"))
        {
            _ = main.RunAllAsync(exitWhenDone: HasArg(e, "--exit"));
        }
    }

    private static bool HasArg(StartupEventArgs e, string name) =>
        e.Args.Any(a => string.Equals(a, name, StringComparison.OrdinalIgnoreCase));
}
