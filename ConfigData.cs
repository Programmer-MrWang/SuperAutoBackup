using System;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;

namespace SuperAutoBackup.ConfigHandlers;

public class ConfigData : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;

    bool _isAutoBackupEnabled = false;
    [JsonPropertyName("isAutoBackupEnabled")]
    public bool IsAutoBackupEnabled
    {
        get => _isAutoBackupEnabled;
        set
        {
            if (_isAutoBackupEnabled == value) return;
            _isAutoBackupEnabled = value;
            OnPropertyChanged();
        }
    }

    int _backupCountLimit = 10;
    [JsonPropertyName("backupCountLimit")]
    public int BackupCountLimit
    {
        get => _backupCountLimit;
        set
        {
            if (_backupCountLimit == value) return;
            _backupCountLimit = value;
            OnPropertyChanged();
        }
    }

    bool _isLogGenerationEnabled = false;
    [JsonPropertyName("isLogGenerationEnabled")]
    public bool IsLogGenerationEnabled
    {
        get => _isLogGenerationEnabled;
        set
        {
            if (_isLogGenerationEnabled == value) return;
            _isLogGenerationEnabled = value;
            OnPropertyChanged();
        }
    }

    string _backupFolderPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
        "ClassIslandBackups");
    [JsonPropertyName("backupFolderPath")]
    public string BackupFolderPath
    {
        get => _backupFolderPath;
        set
        {
            if (_backupFolderPath == value) return;
            _backupFolderPath = value;
            OnPropertyChanged();
        }
    }

    [JsonIgnore]
    public double BackupProgress { get; set; } = 0;

    [JsonIgnore]
    public bool IsBackupInProgress { get; set; } = false;

    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}