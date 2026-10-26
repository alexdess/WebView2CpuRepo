namespace WebView2CpuRepro;

public enum HostMode
{
    WpfOnly,
    WebView2Classic,
    WebView2Composition,
    BlazorWebView,
}

public static class HostModeExtensions
{
    // Read from the assembly so a build against another package version labels itself.
    private static readonly string BlazorWebViewVersion =
        typeof(Microsoft.AspNetCore.Components.WebView.Wpf.BlazorWebView).Assembly
            .GetCustomAttributes(typeof(System.Reflection.AssemblyInformationalVersionAttribute), false)
            .OfType<System.Reflection.AssemblyInformationalVersionAttribute>()
            .Select(a => a.InformationalVersion.Split('+')[0])
            .FirstOrDefault() ?? "?";

    public static string Label(this HostMode mode) => mode switch
    {
        HostMode.WpfOnly => "Plain WPF (no WebView)",
        HostMode.WebView2Classic => "WebView2 (HwndHost)",
        HostMode.WebView2Composition => "WebView2CompositionControl",
        HostMode.BlazorWebView => $"BlazorWebView {BlazorWebViewVersion}",
        _ => mode.ToString(),
    };
}
