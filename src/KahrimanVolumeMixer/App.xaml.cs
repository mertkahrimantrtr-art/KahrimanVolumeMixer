using System;
using System.IO;
using System.Threading;
using System.Windows;
using System.Windows.Threading;
using KahrimanVolumeMixer.Core;
using KahrimanVolumeMixer.Themes;

namespace KahrimanVolumeMixer;

public partial class App : Application
{
    private const string MutexName = @"Local\KahrimanVolumeMixer.SingleInstance";
    private const string ShowEventName = @"Local\KahrimanVolumeMixer.Show";

    private Mutex? _mutex;
    private bool _ownsMutex;
    private EventWaitHandle? _showEvent;
    private RegisteredWaitHandle? _showWait;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // Tek örnek: ikinci kez açılırsa mevcut pencere öne getirilir.
        _mutex = new Mutex(true, MutexName, out bool createdNew);
        _ownsMutex = createdNew;
        _showEvent = new EventWaitHandle(false, EventResetMode.AutoReset, ShowEventName);
        if (!createdNew)
        {
            _showEvent.Set();
            Shutdown();
            return;
        }

        DispatcherUnhandledException += OnDispatcherUnhandledException;
        AppDomain.CurrentDomain.UnhandledException += (_, args) => LogError(args.ExceptionObject as Exception);

        var store = new SettingsStore(SettingsStore.DefaultPath);
        AppSettings settings = store.Load();
        ThemeManager.Apply(settings.Theme);

        var window = new MainWindow(settings, store);
        MainWindow = window;
        window.Show();

        _showWait = ThreadPool.RegisterWaitForSingleObject(
            _showEvent,
            (_, _) => Dispatcher.BeginInvoke(new Action(window.ShowFromTray)),
            null,
            Timeout.Infinite,
            executeOnlyOnce: false);
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _showWait?.Unregister(null);
        _showEvent?.Dispose();

        if (_mutex is not null)
        {
            if (_ownsMutex)
            {
                try { _mutex.ReleaseMutex(); } catch (ApplicationException) { /* zaten bırakılmış */ }
            }

            _mutex.Dispose();
        }

        base.OnExit(e);
    }

    private static void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        // Arka planda kararlı çalışma: beklenmeyen bir hata uygulamayı kapatmaz, günlüğe yazılır.
        LogError(e.Exception);
        e.Handled = true;
    }

    internal static void LogError(Exception? exception)
    {
        if (exception is null) return;
        try
        {
            string directory = Path.GetDirectoryName(SettingsStore.DefaultPath)!;
            Directory.CreateDirectory(directory);
            string path = Path.Combine(directory, "error.log");
            var info = new FileInfo(path);
            if (info.Exists && info.Length > 512 * 1024) info.Delete();
            File.AppendAllText(path, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {exception}{Environment.NewLine}{Environment.NewLine}");
        }
        catch
        {
            // Günlük yazılamazsa sessizce devam edilir.
        }
    }
}
