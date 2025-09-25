using System.Collections.Immutable;
using System.Diagnostics;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;

namespace LockIn.ViewModels;

public partial class MainWindowViewModel : ObservableObject
{
#if RELEASE
    const string HELLDIVER_PROCESS_NAME = "helldivers2";
#elif DEBUG
    const string HELLDIVER_PROCESS_NAME = "notepad";
#endif
    [ObservableProperty]
    private bool _isLockEnabled;
    
    [ObservableProperty]
    private string _statusText = "Disabled";

    [ObservableProperty] private string _lockInText = "Lock In";

    private CancellationTokenSource _clipCTS = new();
    
    private readonly ImmutableDictionary<StatusEnum, string> _statusTextDictionary = new Dictionary<StatusEnum, string>()
    {
        [StatusEnum.Disabled] = "Disabled",
        [StatusEnum.LookingForGame] = "Looking for game",
        [StatusEnum.Enabled] = "Enabled",
    }.ToImmutableDictionary();

    private readonly CancellationTokenSource _applicationExitTokenSource = new();
    
    public MainWindowViewModel()
    {
        StatusText = _statusTextDictionary[StatusEnum.Disabled];
        Application.Current.Exit += QuitApplication;
    }

    private async Task<Process> GetHelldiversProcessAsync(CancellationToken ct = default)
    {
        try
        {
            Process[] processes;
            while ((processes = Process.GetProcessesByName(HELLDIVER_PROCESS_NAME)).Length < 1)
            {
                await Task.Delay(1000, ct).ConfigureAwait(false);
            }

            return processes[0];
        }
        catch (Exception e)
        {
            if (e is not OperationCanceledException)
            {
                MessageBox.Show(
                    $"The code has undemocratically crashed!\nPlease report this violation to your democracy officer:\n\n{e}",
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            throw;
        }
    }

    private async Task LockIn(Process proc, CancellationToken ct = default)
    {
        try
        {
            IntPtr helldiverWindowHandle = proc.MainWindowHandle; //Technically this gets cached anyway but meh
            while (!ct.IsCancellationRequested && !proc.HasExited)
            {
                IntPtr foregroundWindowHandle = Natives.GetForegroundWindow();
                if (foregroundWindowHandle == helldiverWindowHandle && proc.Responding)
                {
                    if (!Natives.GetWindowRect(foregroundWindowHandle, out RECT windowBounds)) break;
                    Natives.ClipCursor(ref windowBounds);
                }
                else
                {
                    Natives.ClipCursor(IntPtr.Zero);
                }
            
                try{ await Task.Delay(100, ct).ConfigureAwait(false); }
                catch { break; }
            }

            Natives.ClipCursor(IntPtr.Zero);
        }
        catch (Exception e)
        {
            MessageBox.Show(
                $"The code has undemocratically crashed!\nPlease report this violation to your democracy officer:\n\n{e}",
                "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            throw;
        }
    }
    
    private void QuitApplication(object sender, ExitEventArgs e)
    {
        _applicationExitTokenSource.Cancel();

        //Theoretically not need since clips are bound to the invoking application, but better safe
        Natives.ClipCursor(IntPtr.Zero);
    }

    private void CancelClipping()
    {
        CancellationTokenSource oldCTS = Interlocked.Exchange(ref _clipCTS, new CancellationTokenSource());
        oldCTS.Cancel();
        oldCTS.Dispose();
        
        StatusText = _statusTextDictionary[StatusEnum.Disabled];
    }

    public void ClipGame()
    {
        StatusText = _statusTextDictionary[StatusEnum.LookingForGame];

        CancellationTokenSource linkedTokenSource =
            
            CancellationTokenSource.CreateLinkedTokenSource(_applicationExitTokenSource.Token, _clipCTS.Token);
        _ = GetHelldiversProcessAsync(linkedTokenSource.Token).ContinueWith(async t =>
        {
            if (t.Status != TaskStatus.RanToCompletion)
            {
                MessageBox.Show(
                    $"The code has undemocratically crashed!\nPlease report this violation to your democracy officer:\n\n{t.Exception}",
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            Process helldiversProc = t.Result;
            while (helldiversProc.MainWindowHandle == IntPtr.Zero)
            {
                MessageBox.Show("Proc found but now window handle. Waiting...");
                await Task.Delay(250, linkedTokenSource.Token).ConfigureAwait(false);
                helldiversProc.Refresh();
            }
            helldiversProc.Refresh();
            
            StatusText = _statusTextDictionary[StatusEnum.Enabled];
            _ = LockIn(helldiversProc, linkedTokenSource.Token).ContinueWith(x =>
            {
                if(x.Status != TaskStatus.RanToCompletion) return;
                if(IsLockEnabled) ClipGame();
            });
        }, linkedTokenSource.Token);
    }

    partial void OnIsLockEnabledChanged(bool value)
    {
        LockInText = value ? "Unlock" : "Lock In";
        if (value is not true)
        {
            CancelClipping();
            return;
        }

        ClipGame();
    }

    private enum StatusEnum
    {
        Disabled,
        LookingForGame,
        Enabled
    }
}