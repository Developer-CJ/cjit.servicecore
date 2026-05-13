using System;
using System.IO;
using System.Threading;
using System.Windows.Forms;

namespace CJIT.ServiceCore;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        ApplicationConfiguration.Initialize();
        SQLitePCL.Batteries_V2.Init();

        Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
        Application.ThreadException += (_, e) => CrashReporter.Show(e.Exception);
        AppDomain.CurrentDomain.UnhandledException += (_, e) =>
        {
            if (e.ExceptionObject is Exception ex) CrashReporter.Write(ex);
        };

        try
        {
            Db.EnsureDatabase();
            using var login = new LoginForm();
            if (login.ShowDialog() == DialogResult.OK)
            {
                Application.Run(new MainForm(login.Actor));
            }
        }
        catch (Exception ex)
        {
            CrashReporter.Show(ex);
        }
    }
}

internal static class CrashReporter
{
    public static string LogDir => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "CJIT", "ServiceCore", "logs");

    public static void Show(Exception ex)
    {
        var path = Write(ex);
        MessageBox.Show(
            "CJIT ServiceCore caught an error and wrote a crash log.\n\n" +
            "The app should keep your local database safe.\n\n" +
            "Log file:\n" + path + "\n\n" +
            ex.GetType().Name + ": " + ex.Message,
            "CJIT ServiceCore Error",
            MessageBoxButtons.OK,
            MessageBoxIcon.Error);
    }

    public static string Write(Exception ex)
    {
        Directory.CreateDirectory(LogDir);
        var path = Path.Combine(LogDir, "crash-" + DateTime.Now.ToString("yyyyMMdd-HHmmss") + ".txt");
        File.WriteAllText(path,
            "CJIT ServiceCore Crash Log\n" +
            "Timestamp: " + DateTime.Now.ToString("s") + "\n\n" +
            ex + "\n");
        return path;
    }
}
