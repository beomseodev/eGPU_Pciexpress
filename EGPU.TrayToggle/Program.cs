using System.IO.Pipes;

namespace EGPU.TrayToggle;

internal static class Program
{
    private const string MutexName = "Global\\EGPU_Pciexpress_TrayToggle";
    private const string PipeName = "EGPU_Pciexpress_TrayToggle";

    [STAThread]
    private static void Main()
    {
        using var mutex = new Mutex(initiallyOwned: true, MutexName, out var createdNew);

        if (!createdNew)
        {
            SendToggleToRunningInstance();
            return;
        }

        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);
        using var context = new TrayToggleContext(PipeName);
        Application.Run(context);
    }

    private static void SendToggleToRunningInstance()
    {
        for (var attempt = 0; attempt < 10; attempt++)
        {
            try
            {
                using var pipe = new NamedPipeClientStream(".", PipeName, PipeDirection.Out);
                pipe.Connect(500);
                using var writer = new StreamWriter(pipe) { AutoFlush = true };
                writer.WriteLine("toggle");
                return;
            }
            catch
            {
                Thread.Sleep(200);
            }
        }
    }
}
