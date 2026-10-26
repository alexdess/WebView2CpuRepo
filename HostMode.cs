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
    public static string Label(this HostMode mode) => mode switch
    {
        HostMode.WpfOnly => "Plain WPF (no WebView)",
        HostMode.WebView2Classic => "WebView2 (HwndHost)",
        HostMode.WebView2Composition => "WebView2CompositionControl",
        HostMode.BlazorWebView => "BlazorWebView .NET 10",
        _ => mode.ToString(),
    };
}
