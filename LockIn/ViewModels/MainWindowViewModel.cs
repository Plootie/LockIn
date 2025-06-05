using System.ComponentModel;
using System.Diagnostics;
using System.Text;
using System.Windows;
using System.Windows.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace LockIn.ViewModels;

public partial class MainWindowViewModel : ObservableObject
{
    [ObservableProperty]
    private bool _isLockEnabled;
    
    [ObservableProperty]
    private string _statusText = "Disabled";

    [ObservableProperty] private string _lockInText = "Lock In";

    private Process? _gameProcess;
    private readonly ManualResetEvent _threadSignaller = new(false);
    private readonly Thread _gameFinder;
    private readonly Thread _cursorClipper;
    private readonly CancellationTokenSource _applicationExitTokenSource = new();

    public MainWindowViewModel()
    {
        _gameFinder = new Thread(ThreadFindGame);
        _gameFinder.Start();

        _cursorClipper = new Thread(ThreadClipToWindow);
        _cursorClipper.Start();
        
        Application.Current.Exit += QuitApplication;
    }

    private void QuitApplication(object sender, ExitEventArgs e)
    {
        _applicationExitTokenSource.Cancel();
        _gameFinder.Interrupt();
        _cursorClipper.Interrupt();

        //Theoretically not need since clips are bound to the application, but better safe
        Natives.ClipCursor(IntPtr.Zero);
    }

    private void UpdateLabel()
    {
        string textToSet = string.Empty;
        if (!IsLockEnabled)
        {
            textToSet = "Disabled";
            _gameProcess = null;
        }
        else
        {
            if (_gameProcess == null || _gameProcess.HasExited)
            {
                textToSet = "Looking for game...";
                _threadSignaller.Set();
            }
            else
            {
                textToSet = "Enabled";
            }   
        }
        
        StatusText = textToSet;
    }

    private void ThreadFindGame()
    {
        try
        {
            while (_threadSignaller.WaitOne())
            {
                if (_applicationExitTokenSource.Token.IsCancellationRequested) return;
                string processToFind = "helldivers2";
#if DEBUG
                processToFind = "notepad";       
#endif
                Process[] processes = Process.GetProcessesByName(processToFind);
                if (processes.Length > 0)
                {
                    _gameProcess = processes.First();
                    _gameProcess.EnableRaisingEvents = true;
                    _gameProcess.Exited += GameExit;
                    _threadSignaller.Reset();
                    UpdateLabel();
                    continue;
                }

                Thread.Sleep(3000);
            }
        }
        catch (ThreadInterruptedException){}
    }

    private void ThreadClipToWindow()
    {
        try
        {
            while (!_applicationExitTokenSource.Token.IsCancellationRequested)
            {
                if (IsLockEnabled && _gameProcess != null)
                {
                    if (Natives.GetForegroundWindow() == _gameProcess.MainWindowHandle)
                    {
                        Natives.GetWindowRect(_gameProcess.MainWindowHandle, out RECT windowRect);
                        Natives.ClipCursor(ref windowRect);
                    }
                    else
                    {
                        Natives.ClipCursor(IntPtr.Zero);
                    }
                }
                
                Thread.Sleep(300); 
            } 
        }catch(ThreadInterruptedException){}
        
    }

    private void GameExit(object? sender, EventArgs e)
    {
        _gameProcess = null;
        UpdateLabel();
    }

    partial void OnIsLockEnabledChanged(bool value)
    {
        LockInText = value ? "Unlock" : "Lock In";
        Natives.ClipCursor(IntPtr.Zero);
        UpdateLabel();
    }
}