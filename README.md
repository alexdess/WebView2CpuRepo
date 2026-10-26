# WebView2CpuRepro

Measures the CPU cost of hosting the same static page in four WPF hosts, to show that
`WebView2CompositionControl` (used by `BlazorWebView` WPF since .NET 10) keeps the app
process busy on an idle page when WPF renders in software, as it does in a Remote Desktop
session without GPU.

| Host | Control |
|---|---|
| Plain WPF | `TextBox` + `DataGrid`, no WebView (baseline) |
| WebView2 (HwndHost) | `Microsoft.Web.WebView2.Wpf.WebView2` |
| WebView2CompositionControl | `Microsoft.Web.WebView2.Wpf.WebView2CompositionControl` |
| BlazorWebView .NET 10 | `Microsoft.AspNetCore.Components.WebView.Wpf.BlazorWebView` 10.0.110 |

Every host shows the same page: a heading, a focused text input and a 20-row table.
No script, no animation.

## Run

```
dotnet publish -c Release -o publish
publish\WebView2CpuRepro.exe
```

The project publishes a self-contained single-file exe (.NET 10, win-x64). The WebView2
Runtime must be installed.

1. Start the exe inside an RDP session on a host without GPU. The header must read
   `RDP session: yes` and `WPF render tier: 0`.
2. Click **Measure all (auto, ~4 min)** and leave the machine alone. Each host is opened in
   turn, measured 20 s in the foreground, then 20 s minimized, then closed.
3. Click **Copy results**. Results are also written to
   `%LOCALAPPDATA%\WebView2CpuRepro\results.txt`.

The percentage is the CPU of the app process only, normalized over all cores, as Task
Manager shows it. The WebView2 browser processes are not included.

### Command line

| Option | Effect |
|---|---|
| `--software` | Forces WPF software rendering (`RenderMode.SoftwareOnly`), to reproduce the RDP case on a machine with a GPU |
| `--auto` | Starts the automatic run at startup |
| `--exit` | Closes the app at the end of the automatic run |

## Results

Windows Server 2022 (20348), RDP session, 8 cores, WPF tier 0, WebView2 Runtime
155.0.4283.62, .NET 10.0.11. Two runs, identical within 0.6 point:

| Host | Foreground | Minimized |
|---|---|---|
| Plain WPF | 0.1 % | 0.3 % |
| WebView2 (HwndHost) | 0.0 % | 0.0 % |
| WebView2CompositionControl | 14 % | 8.5 % |
| BlazorWebView .NET 10 | 18 % | 11 % |
