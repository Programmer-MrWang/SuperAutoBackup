using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace SuperAutoBackup;

public class BackupSettings : INotifyPropertyChanged
{
    private bool _enabled = true;
    private string _targetPath = @"D:\ClassIslandBackup";
    private int _maxBackups = 5;
    private bool _enableLog = true;

    public bool Enabled
    {
        get => _enabled;
        set { _enabled = value; OnPropertyChanged(); }
    }

    public string TargetPath
    {
        get => _targetPath;
        set { _targetPath = value; OnPropertyChanged(); }
    }

    public int MaxBackups
    {
        get => _maxBackups;
        set { _maxBackups = value < 1 ? 1 : value; OnPropertyChanged(); }
    }

    public bool EnableLog
    {
        get => _enableLog;
        set { _enableLog = value; OnPropertyChanged(); }
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    private void OnPropertyChanged([CallerMemberName] string prop = "")
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop));
}