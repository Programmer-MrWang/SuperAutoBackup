using ClassIsland.Core.Attributes;
using MaterialDesignThemes.Wpf;
using System.Diagnostics;
using System.IO;

namespace SuperAutoBackup;

[SettingsPageInfo(
    "com.example.superautobackup",
    "超级自动备份",
    PackIconKind.FolderOutline,
    PackIconKind.Folder)]
public partial class AdvancedBackupSettingsPage
{
    public AdvancedBackupSettingsPage()
    {
        DataContext = Plugin.Settings;  // 绑定静态配置
        InitializeComponent();
    }

    private void BtnBackupNow_Click(object sender, System.Windows.RoutedEventArgs e)
    {
        LogHelper.Write("MANUAL_BACKUP", "用户点击「立即备份」", Plugin.Settings.TargetPath);
        Plugin.DoBackup();
    }

    private void BtnOpenFolder_Click(object sender, System.Windows.RoutedEventArgs e)
    {
        var path = Plugin.Settings.TargetPath;
        LogHelper.Write("OPEN_FOLDER", $"打开备份文件夹：{path}", Plugin.Settings.TargetPath);
        if (Directory.Exists(path))
            Process.Start("explorer.exe", path);
    }
}