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
| `--software` | Forces WPF software rendering (`RenderMode.SoftwareOnly`). It does **not** reproduce the RDP case: on a machine with a GPU the composition control stays near 0 % even with this flag, so measure in a real RDP session on a host without GPU |
| `--auto` | Starts the automatic run at startup |
| `--exit` | Closes the app at the end of the automatic run |

## Results

Windows Server 2022 (20348), RDP session, 8 cores, WPF tier 0, WebView2 Runtime
155.0.4283.62, .NET 10.0.11:

| Host | Foreground | Minimized |
|---|---|---|
| Plain WPF | 0.1 % | 0.3 % |
| WebView2 (HwndHost) | 0.0 % | 0.0 % |
| WebView2CompositionControl | 12.5 to 14 % | 6.8 to 8.8 % |
| BlazorWebView 10.0.110, window opened alone | 15.6 % | |
| BlazorWebView 9.0.120 (HWND `WebView2`), window opened alone | 0.4 % | |

Builds before the one that disposes each host on close charged the
`WebView2CompositionControl` cost to the next measure of the automatic run: with no test
window open, the process went from 0.1 % to 5.0 % once that window had been opened and
closed. The BlazorWebView rows above were measured with the window opened alone, in a
freshly started app.

The same build in a Windows 11 (26100) Windows Sandbox with `<vGPU>Disable</vGPU>`, also an
RDP session at WPF tier 0, shows 0.2 core for `WebView2CompositionControl` instead of 1.1:
an RDP session without GPU is not enough, something of the Windows Server 2022 host is
needed, the OS build being the first candidate.
