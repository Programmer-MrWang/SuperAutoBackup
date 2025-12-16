using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using ClassIsland.Core;
using ClassIsland.Core.Abstractions.Controls;
using ClassIsland.Core.Attributes;
using FluentAvalonia.UI.Controls;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace SuperAutoBackup;

[SettingsPageInfo("superAutoBackup", "高级备份", "\uef5d", "\uef5c")]
public partial class SuperAutoBackupSettingsPage : SettingsPageBase
{
    public Settings Settings { get; }

    public SuperAutoBackupSettingsPage(Settings settings)
    {
        Settings = settings;
        DataContext = this;
        InitializeComponent();
    }

    private async void ManualBackup_Click(object sender, RoutedEventArgs e)
    {
        var button = sender as Button;
        button.IsEnabled = false;

        // ✅ 关键修复：提前获取 TopLevel，避免异步操作后丢失上下文
        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel == null)
        {
            button.IsEnabled = true;
            return;
        }

        try
        {
            Settings.IsBackupInProgress = true;
            Settings.BackupProgress = 0;

            // ✅ 创建进度报告器
            var progress = new Progress<double>(value =>
            {
                Settings.BackupProgress = value;
            });

            // ✅ 在后台线程执行，并传递进度
            await Task.Run(async () =>
            {
                await BackupHelper.CreateBackup(Settings.BackupFolderPath, Settings.IsLogGenerationEnabled, progress);
                BackupHelper.CleanOldBackups(Settings.BackupFolderPath, Settings.BackupCountLimit);
            });

            // ✅ 修复：使用带 TopLevel 参数的 ShowAsync
            await new ContentDialog
            {
                Title = "备份成功",
                Content = "备份已完成！",
                PrimaryButtonText = "确定"
            }.ShowAsync(topLevel);  // 传入 topLevel 参数
        }
        catch (Exception ex)
        {
            // ✅ 修复：使用带 TopLevel 参数的 ShowAsync
            await new ContentDialog
            {
                Title = "备份失败",
                Content = ex.Message,
                PrimaryButtonText = "确定"
            }.ShowAsync(topLevel);  // 传入 topLevel 参数
        }
        finally
        {
            Settings.IsBackupInProgress = false;
            button.IsEnabled = true;
        }
    }

    private void OpenBackupFolder_Click(object sender, RoutedEventArgs e)
    {
        if (Directory.Exists(Settings.BackupFolderPath))
        {
            Process.Start(new ProcessStartInfo { FileName = Settings.BackupFolderPath, UseShellExecute = true, Verb = "open" });
        }
        else
        {
            Directory.CreateDirectory(Settings.BackupFolderPath);
            Process.Start(new ProcessStartInfo { FileName = Settings.BackupFolderPath, UseShellExecute = true, Verb = "open" });
        }
    }

    private async void SelectBackupFolder_Click(object sender, RoutedEventArgs e)
    {
        if (!Directory.Exists(Settings.BackupFolderPath))
            Directory.CreateDirectory(Settings.BackupFolderPath);

        var storage = TopLevel.GetTopLevel(this)?.StorageProvider;
        if (storage is null) return;

        var options = new FolderPickerOpenOptions
        {
            Title = "选择备份文件夹",
            SuggestedStartLocation = await storage.TryGetFolderFromPathAsync(Settings.BackupFolderPath),
            AllowMultiple = false
        };

        var result = await storage.OpenFolderPickerAsync(options);
        var folder = result.FirstOrDefault();
        if (folder is not null)
        {
            Settings.BackupFolderPath = folder.Path.LocalPath;
        }
    }
}