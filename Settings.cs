using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.ObjectModel;
using System.IO;
using System;

namespace SuperAutoBackup;

public partial class Settings : ObservableObject
{
    [ObservableProperty]
    private bool _isAutoBackupEnabled = false;

    [ObservableProperty]
    private int _backupCountLimit = 10;

    [ObservableProperty]
    private bool _isLogGenerationEnabled = false;

    [ObservableProperty]
    private string _backupFolderPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
        "ClassIslandBackups");

    // ✅ 新增：备份进度（0-100）
    [ObservableProperty]
    private double _backupProgress = 0;

    // ✅ 新增：是否正在备份（用于控制进度条可见性）
    [ObservableProperty]
    private bool _isBackupInProgress = false;
}