using System.Text;

namespace WebView2CpuRepro;

/// <summary>
/// The same static page for every host, so only the hosting control differs between measures.
/// </summary>
public static class TestContent
{
    public const int ROW_COUNT = 20;

    // Inline rather than a wwwroot file: the build output has no wwwroot folder, Blazor serves it from the static web assets manifest.
    public const string CSS = """
        body { font-family: Segoe UI, sans-serif; margin: 16px; color: #222; }
        input { font-size: 16px; padding: 6px; width: 320px; }
        table { border-collapse: collapse; margin-top: 16px; }
        td, th { border: 1px solid #ccc; padding: 4px 12px; text-align: left; }
        """;

    public static string UserDataFolder { get; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "WebView2CpuRepro",
        "WebView2Data");

    public static IEnumerable<(string Client, string City)> Rows() =>
        Enumerable.Range(1, ROW_COUNT).Select(i => ($"Client {i:00}", $"{1000 + i} City {i}"));

    public static string Html()
    {
        var sb = new StringBuilder();
        sb.Append("<!DOCTYPE html><html><head><meta charset=\"utf-8\"><style>");
        sb.Append(CSS);
        sb.Append("</style></head><body>");
        sb.Append("<h2>Static page</h2>");
        sb.Append("<input id=\"search\" placeholder=\"Search a contact...\" autofocus />");
        sb.Append("<table><tr><th>Client</th><th>City</th></tr>");
        foreach (var (client, city) in Rows())
        {
            sb.Append($"<tr><td>{client}</td><td>{city}</td></tr>");
        }

        sb.Append("</table></body></html>");
        return sb.ToString();
    }
}
