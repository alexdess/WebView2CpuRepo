using System.Windows;
using System.Windows.Controls;
using Microsoft.AspNetCore.Components.WebView.Wpf;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Web.WebView2.Wpf;

namespace WebView2CpuRepro;

/// <summary>
/// One window per hosting mode, each showing the same static content.
/// </summary>
public sealed class HostWindow : Window
{
    private static readonly IServiceProvider _blazorServices = BuildBlazorServices();

    public HostWindow(HostMode mode)
    {
        Mode = mode;
        Title = mode.Label();
        Width = 900;
        Height = 650;
        WindowStartupLocation = WindowStartupLocation.CenterScreen;
        Content = mode switch
        {
            HostMode.WpfOnly => BuildWpfContent(),
            HostMode.WebView2Classic => BuildWebView2(new WebView2()),
            HostMode.WebView2Composition => BuildWebView2(new WebView2CompositionControl()),
            HostMode.BlazorWebView => BuildBlazorWebView(),
            _ => throw new ArgumentOutOfRangeException(nameof(mode)),
        };
    }

    public HostMode Mode { get; }

    private static IServiceProvider BuildBlazorServices()
    {
        var services = new ServiceCollection();
        services.AddWpfBlazorWebView();
        return services.BuildServiceProvider();
    }

    private static UIElement BuildWpfContent()
    {
        var search = new TextBox { Width = 320, FontSize = 16, Margin = new Thickness(0, 0, 0, 16), HorizontalAlignment = HorizontalAlignment.Left };
        var grid = new DataGrid { AutoGenerateColumns = true, IsReadOnly = true, ItemsSource = TestContent.Rows().Select(r => new { r.Client, r.City }).ToList() };
        var panel = new StackPanel { Margin = new Thickness(16) };
        panel.Children.Add(new TextBlock { Text = "Static page", FontSize = 22, Margin = new Thickness(0, 0, 0, 12) });
        panel.Children.Add(search);
        panel.Children.Add(grid);
        panel.Loaded += (_, _) => search.Focus();
        return panel;
    }

    private static UIElement BuildWebView2<T>(T webView)
        where T : FrameworkElement, IWebView2
    {
        webView.CreationProperties = new CoreWebView2CreationProperties { UserDataFolder = TestContent.UserDataFolder };
        webView.Loaded += async (_, _) =>
        {
            // async void handler: an escaping exception would kill the whole app.
            try
            {
                await webView.EnsureCoreWebView2Async();
                webView.NavigateToString(TestContent.Html());
                webView.Focus();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), "WebView2 initialization error");
            }
        };
        return webView;
    }

    private static UIElement BuildBlazorWebView()
    {
        var blazor = new BlazorWebView { HostPage = @"wwwroot\index.html", Services = _blazorServices };
        blazor.BlazorWebViewInitializing += (_, e) => e.UserDataFolder = TestContent.UserDataFolder;
        blazor.RootComponents.Add(new RootComponent { Selector = "#app", ComponentType = typeof(TestPage) });
        blazor.Loaded += (_, _) => blazor.Focus();
        return blazor;
    }
}
