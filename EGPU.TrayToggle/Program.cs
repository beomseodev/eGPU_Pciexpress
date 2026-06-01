using System.IO.Pipes;

namespace EGPU.TrayToggle;

internal static class Program
{
    private const string MutexName = "Global\\EGPU_Pciexpress_TrayToggle";
    private const string PipeName = "EGPU_Pciexpress_TrayToggle";

    [STAThread]
    private static void Main(string[] args)
    {
        var startupMode = args.Any(argument =>
            string.Equals(argument, "--no-toggle", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(argument, "--startup", StringComparison.OrdinalIgnoreCase));
        using var mutex = new Mutex(initiallyOwned: true, MutexName, out var createdNew);

        if (!createdNew)
        {
            SendCommandToRunningInstance(startupMode ? "refresh" : "toggle");
            return;
        }

        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);
        using var context = new TrayToggleContext(PipeName, toggleOnStart: !startupMode);
        Application.Run(context);
    }

    private static void SendCommandToRunningInstance(string command)
    {
        for (var attempt = 0; attempt < 10; attempt++)
        {
            try
            {
                using var pipe = new NamedPipeClientStream(".", PipeName, PipeDirection.Out);
                pipe.Connect(500);
                using var writer = new StreamWriter(pipe) { AutoFlush = true };
                writer.WriteLine(command);
                return;
            }
            catch
            {
                Thread.Sleep(200);
            }
        }
    }
}
