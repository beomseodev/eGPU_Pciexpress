using System.Diagnostics;
using System.Drawing.Drawing2D;
using System.IO.Pipes;
using System.Runtime.InteropServices;

namespace EGPU.TrayToggle;

internal sealed class TrayToggleContext : ApplicationContext
{
    private readonly NotifyIcon notifyIcon;
    private readonly ToolStripMenuItem toggleMenuItem;
    private readonly ToolStripMenuItem refreshMenuItem;
    private readonly ToolStripMenuItem startupMenuItem;
    private readonly ToolStripMenuItem exitMenuItem;
    private readonly CancellationTokenSource pipeCancellation = new();
    private readonly string pipeName;
    private readonly Icon onIcon;
    private readonly Icon offIcon;
    private readonly Icon errorIcon;
    private readonly Form messageWindow;
    private bool isBusy;
    private bool pendingToggle;
    private PciexpressState currentState = PciexpressState.Unknown;

    public TrayToggleContext(string pipeName, bool toggleOnStart)
    {
        this.pipeName = pipeName;
        onIcon = IconFactory.CreateStatusLight(Color.FromArgb(0, 190, 90));
        offIcon = IconFactory.CreateStatusLight(Color.FromArgb(220, 35, 35));
        errorIcon = IconFactory.CreateText("!", Color.FromArgb(190, 35, 35), Color.White);
        messageWindow = new Form
        {
            ShowInTaskbar = false,
            WindowState = FormWindowState.Minimized,
            Opacity = 0
        };
        _ = messageWindow.Handle;

        toggleMenuItem = new ToolStripMenuItem("Toggle", null, async (_, _) => await ToggleAsync(showBalloon: true));
        refreshMenuItem = new ToolStripMenuItem("Refresh", null, async (_, _) => await RefreshAsync(showBalloon: true));
        startupMenuItem = new ToolStripMenuItem("Start with Windows", null, async (_, _) => await ToggleStartupAsync())
        {
            CheckOnClick = false
        };
        exitMenuItem = new ToolStripMenuItem("Exit", null, (_, _) => ExitApplication());

        var menu = new ContextMenuStrip();
        menu.Items.Add(toggleMenuItem);
        menu.Items.Add(refreshMenuItem);
        menu.Items.Add(startupMenuItem);
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add(exitMenuItem);

        notifyIcon = new NotifyIcon
        {
            ContextMenuStrip = menu,
            Icon = offIcon,
            Text = "eGPU status checking",
            Visible = true
        };
        notifyIcon.DoubleClick += async (_, _) => await ToggleAsync(showBalloon: true);

        _ = RunPipeServerAsync(pipeCancellation.Token);
        _ = RefreshStartupMenuAsync();
        _ = toggleOnStart
            ? ToggleAsync(showBalloon: true)
            : RefreshAsync(showBalloon: false);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            pipeCancellation.Cancel();
            notifyIcon.Visible = false;
            notifyIcon.Dispose();
            messageWindow.Dispose();
            onIcon.Dispose();
            offIcon.Dispose();
            errorIcon.Dispose();
            pipeCancellation.Dispose();
        }

        base.Dispose(disposing);
    }

    private async Task RunPipeServerAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                using var server = new NamedPipeServerStream(
                    pipeName,
                    PipeDirection.In,
                    maxNumberOfServerInstances: 1,
                    PipeTransmissionMode.Byte,
                    PipeOptions.Asynchronous);

                await server.WaitForConnectionAsync(cancellationToken);
                using var reader = new StreamReader(server);
                var command = await reader.ReadLineAsync(cancellationToken);
                if (string.Equals(command, "toggle", StringComparison.OrdinalIgnoreCase))
                {
                    RunOnUiThread(async () =>
                    {
                        notifyIcon.ShowBalloonTip(1000, "eGPU PCI Express", "Toggling state.", ToolTipIcon.Info);
                        await ToggleAsync(showBalloon: true);
                    });
                }
                else if (string.Equals(command, "refresh", StringComparison.OrdinalIgnoreCase))
                {
                    RunOnUiThread(async () => await RefreshAsync(showBalloon: false));
                }
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                RunOnUiThread(() =>
                {
                    ShowError("Command handling failed", ex);
                    return Task.CompletedTask;
                });
            }
        }
    }

    private async Task ToggleAsync(bool showBalloon)
    {
        if (isBusy)
        {
            pendingToggle = true;
            return;
        }

        SetBusy(true);
        try
        {
            var before = await BcdeditService.GetStateAsync();
            var nextMode = before == PciexpressState.On ? "Default" : "ForceDisable";
            await BcdeditService.SetModeAsync(nextMode);

            var after = await BcdeditService.GetStateAsync();
            SetState(after);

            if (showBalloon)
            {
                var message = after == PciexpressState.On
                    ? "ON applied. Restart may be required."
                    : "OFF restored. Restart may be required.";
                notifyIcon.ShowBalloonTip(3000, "eGPU PCI Express", message, ToolTipIcon.Info);
            }
        }
        catch (Exception ex)
        {
            SetErrorState();
            ShowError("Toggle failed", ex);
        }
        finally
        {
            SetBusy(false);
        }

        if (pendingToggle)
        {
            pendingToggle = false;
            await ToggleAsync(showBalloon);
        }
    }

    private async Task RefreshAsync(bool showBalloon)
    {
        if (isBusy)
        {
            return;
        }

        SetBusy(true);
        try
        {
            var state = await BcdeditService.GetStateAsync();
            SetState(state);
            if (showBalloon)
            {
                var message = state == PciexpressState.On
                    ? "Current state: ON"
                    : "Current state: OFF";
                notifyIcon.ShowBalloonTip(2000, "eGPU PCI Express", message, ToolTipIcon.Info);
            }
        }
        catch (Exception ex)
        {
            SetErrorState();
            ShowError("Refresh failed", ex);
        }
        finally
        {
            SetBusy(false);
        }
    }

    private void SetState(PciexpressState state)
    {
        currentState = state;
        if (state == PciexpressState.On)
        {
            notifyIcon.Icon = onIcon;
            notifyIcon.Text = "eGPU stabilization ON";
            toggleMenuItem.Text = "Turn OFF";
        }
        else
        {
            notifyIcon.Icon = offIcon;
            notifyIcon.Text = "eGPU stabilization OFF";
            toggleMenuItem.Text = "Turn ON";
        }

    }

    private void SetErrorState()
    {
        currentState = PciexpressState.Unknown;
        notifyIcon.Icon = errorIcon;
        notifyIcon.Text = "eGPU status error";
        toggleMenuItem.Text = "Toggle";
    }

    private void SetBusy(bool busy)
    {
        isBusy = busy;
        toggleMenuItem.Enabled = !busy;
        refreshMenuItem.Enabled = !busy;
        startupMenuItem.Enabled = !busy;
        notifyIcon.Text = busy
            ? "eGPU setting in progress"
            : currentState switch
            {
                PciexpressState.On => "eGPU stabilization ON",
                PciexpressState.Off => "eGPU stabilization OFF",
                _ => "eGPU status error"
            };
    }

    private async Task ToggleStartupAsync()
    {
        SetStartupMenuBusy(true);
        try
        {
            var installed = await StartupTaskService.IsInstalledAsync();
            if (installed)
            {
                await StartupTaskService.UninstallAsync();
                startupMenuItem.Checked = false;
                notifyIcon.ShowBalloonTip(2000, "eGPU PCI Express", "Windows startup disabled.", ToolTipIcon.Info);
            }
            else
            {
                await StartupTaskService.InstallAsync();
                startupMenuItem.Checked = true;
                notifyIcon.ShowBalloonTip(2000, "eGPU PCI Express", "Windows startup enabled.", ToolTipIcon.Info);
            }
        }
        catch (Exception ex)
        {
            ShowError("Startup setting failed", ex);
        }
        finally
        {
            SetStartupMenuBusy(false);
        }
    }

    private async Task RefreshStartupMenuAsync()
    {
        SetStartupMenuBusy(true);
        try
        {
            startupMenuItem.Checked = await StartupTaskService.IsInstalledAsync();
        }
        catch
        {
            startupMenuItem.Checked = false;
        }
        finally
        {
            SetStartupMenuBusy(false);
        }
    }

    private void SetStartupMenuBusy(bool busy)
    {
        startupMenuItem.Enabled = !busy && !isBusy;
    }

    private void ShowError(string title, Exception ex)
    {
        notifyIcon.ShowBalloonTip(5000, $"eGPU PCI Express - {title}", ex.Message, ToolTipIcon.Error);
    }

    private void RunOnUiThread(Func<Task> action)
    {
        if (messageWindow.IsDisposed)
        {
            return;
        }

        if (messageWindow.InvokeRequired)
        {
            messageWindow.BeginInvoke((System.Windows.Forms.MethodInvoker)(async () => await action()));
            return;
        }

        _ = action();
    }

    private void ExitApplication()
    {
        pipeCancellation.Cancel();
        notifyIcon.Visible = false;
        ExitThread();
    }
}

internal enum PciexpressState
{
    Unknown,
    Off,
    On
}

internal static class BcdeditService
{
    public static async Task<PciexpressState> GetStateAsync()
    {
        var result = await RunBcdeditAsync("/enum");
        var mode = ParsePciexpressMode(result.Output);
        return string.Equals(mode, "ForceDisable", StringComparison.OrdinalIgnoreCase)
            ? PciexpressState.On
            : PciexpressState.Off;
    }

    public static async Task SetModeAsync(string mode)
    {
        _ = await RunBcdeditAsync($"/set pciexpress {mode}");
    }

    private static string? ParsePciexpressMode(string output)
    {
        foreach (var line in output.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries))
        {
            var trimmed = line.Trim();
            if (!trimmed.StartsWith("pciexpress", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var parts = trimmed.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length >= 2)
            {
                return parts[1];
            }
        }

        return null;
    }

    private static async Task<CommandResult> RunBcdeditAsync(string arguments)
    {
        var fileName = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "System32", "bcdedit.exe");
        var startInfo = new ProcessStartInfo
        {
            FileName = fileName,
            Arguments = arguments,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };

        using var process = Process.Start(startInfo)
            ?? throw new InvalidOperationException("Failed to start bcdedit.exe.");

        var outputTask = process.StandardOutput.ReadToEndAsync();
        var errorTask = process.StandardError.ReadToEndAsync();
        await process.WaitForExitAsync();

        var output = (await outputTask).Trim();
        var error = (await errorTask).Trim();
        if (process.ExitCode != 0)
        {
            var message = string.IsNullOrWhiteSpace(error) ? output : error;
            throw new InvalidOperationException(message);
        }

        return new CommandResult(output, error);
    }
}

internal sealed record CommandResult(string Output, string Error);

internal static class StartupTaskService
{
    private const string TaskName = "eGPU Tray Toggle";

    public static async Task<bool> IsInstalledAsync()
    {
        var result = await RunSchtasksAsync($"/Query /TN \"{TaskName}\"", throwOnFailure: false);
        return result.ExitCode == 0;
    }

    public static async Task InstallAsync()
    {
        var exePath = Environment.ProcessPath
            ?? throw new InvalidOperationException("Unable to resolve the current executable path.");
        var taskRun = $"\"{exePath}\" --no-toggle";
        var arguments = $"/Create /TN \"{TaskName}\" /TR \"{taskRun}\" /SC ONLOGON /RL HIGHEST /F";
        _ = await RunSchtasksAsync(arguments, throwOnFailure: true);
    }

    public static async Task UninstallAsync()
    {
        _ = await RunSchtasksAsync($"/Delete /TN \"{TaskName}\" /F", throwOnFailure: true);
    }

    private static async Task<SchtasksResult> RunSchtasksAsync(string arguments, bool throwOnFailure)
    {
        var fileName = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "System32", "schtasks.exe");
        var startInfo = new ProcessStartInfo
        {
            FileName = fileName,
            Arguments = arguments,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };

        using var process = Process.Start(startInfo)
            ?? throw new InvalidOperationException("Failed to start schtasks.exe.");

        var outputTask = process.StandardOutput.ReadToEndAsync();
        var errorTask = process.StandardError.ReadToEndAsync();
        await process.WaitForExitAsync();

        var output = (await outputTask).Trim();
        var error = (await errorTask).Trim();
        if (throwOnFailure && process.ExitCode != 0)
        {
            var message = string.IsNullOrWhiteSpace(error) ? output : error;
            throw new InvalidOperationException(message);
        }

        return new SchtasksResult(process.ExitCode, output, error);
    }
}

internal sealed record SchtasksResult(int ExitCode, string Output, string Error);

internal static class IconFactory
{
    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool DestroyIcon(IntPtr hIcon);

    public static Icon CreateStatusLight(Color color)
    {
        using var bitmap = new Bitmap(32, 32);
        using (var graphics = Graphics.FromImage(bitmap))
        {
            graphics.SmoothingMode = SmoothingMode.AntiAlias;
            graphics.Clear(Color.Transparent);
            using var shadowBrush = new SolidBrush(Color.FromArgb(80, 0, 0, 0));
            graphics.FillEllipse(shadowBrush, 4, 5, 25, 25);

            using var ringBrush = new SolidBrush(Color.FromArgb(40, 40, 40));
            graphics.FillEllipse(ringBrush, 2, 2, 28, 28);

            using var lightBrush = new SolidBrush(color);
            graphics.FillEllipse(lightBrush, 5, 5, 22, 22);

            using var highlightBrush = new SolidBrush(Color.FromArgb(150, 255, 255, 255));
            graphics.FillEllipse(highlightBrush, 9, 8, 7, 7);
        }

        return CreateIconFromBitmap(bitmap);
    }

    public static Icon CreateText(string text, Color background, Color foreground)
    {
        using var bitmap = new Bitmap(32, 32);
        using (var graphics = Graphics.FromImage(bitmap))
        {
            graphics.SmoothingMode = SmoothingMode.AntiAlias;
            graphics.Clear(Color.Transparent);
            using var brush = new SolidBrush(background);
            graphics.FillEllipse(brush, 1, 1, 30, 30);

            using var font = new Font("Segoe UI", text.Length > 2 ? 8 : 10, FontStyle.Bold, GraphicsUnit.Pixel);
            using var textBrush = new SolidBrush(foreground);
            var size = graphics.MeasureString(text, font);
            var x = (32 - size.Width) / 2;
            var y = (32 - size.Height) / 2;
            graphics.DrawString(text, font, textBrush, x, y);
        }

        return CreateIconFromBitmap(bitmap);
    }

    private static Icon CreateIconFromBitmap(Bitmap bitmap)
    {
        var handle = bitmap.GetHicon();
        try
        {
            using var temporaryIcon = Icon.FromHandle(handle);
            return (Icon)temporaryIcon.Clone();
        }
        finally
        {
            DestroyIcon(handle);
        }
    }
}
