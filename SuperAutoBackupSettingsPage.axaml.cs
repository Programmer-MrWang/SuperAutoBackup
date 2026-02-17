using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using ClassIsland.Core.Abstractions.Controls;
using ClassIsland.Core.Attributes;
using ClassIsland.Core.Helpers.UI;
using ClassIsland.Core.Models.UI;
using CommunityToolkit.Mvvm.ComponentModel;
using SuperAutoBackup.ConfigHandlers;
using SuperAutoBackup.Shared;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace SuperAutoBackup;

[HidePageTitle]
[SettingsPageInfo("superAutoBackup.settings", "SuperAutoBackup 设置", "\uE5B7", "\uE5B7")]
public partial class SuperAutoBackupSettingsPage : SettingsPageBase
{
    public SuperAutoBackupSettingsViewModel ViewModel { get; }

    public SuperAutoBackupSettingsPage()
    {
        ViewModel = new SuperAutoBackupSettingsViewModel();
        DataContext = this;
        InitializeComponent();
    }

    private async void ManualBackup_Click(object sender, RoutedEventArgs e)
    {
        var button = (Button)sender;
        button.IsEnabled = false;

        try
        {
            ViewModel.Config.IsBackupInProgress = true;
            ViewModel.Config.BackupProgress = 0;

            var progress = new Progress<double>(value =>
                ViewModel.Config.BackupProgress = value);

            await Task.Run(async () =>
            {
                await BackupHelper.CreateBackup(
                    ViewModel.Config.BackupFolderPath,
                    ViewModel.Config.IsLogGenerationEnabled,
                    progress);

                BackupHelper.CleanOldBackups(
                    ViewModel.Config.BackupFolderPath,
                    ViewModel.Config.BackupCountLimit);
            });

            this.ShowSuccessToast("备份已完成！");
        }
        catch (Exception ex)
        {
            this.ShowErrorToast("备份失败", ex);
        }
        finally
        {
            ViewModel.Config.IsBackupInProgress = false;
            button.IsEnabled = true;
        }
    }

    private void OpenBackupFolder_Click(object sender, RoutedEventArgs e)
    {
        var path = ViewModel.Config.BackupFolderPath;

        if (!Directory.Exists(path))
            Directory.CreateDirectory(path);

        Process.Start(new ProcessStartInfo
        {
            FileName = path,
            UseShellExecute = true
        });
    }

    private async void SelectBackupFolder_Click(object sender, RoutedEventArgs e)
    {
        var storage = TopLevel.GetTopLevel(this)?.StorageProvider;
        if (storage is null) return;

        var options = new FolderPickerOpenOptions
        {
            Title = "选择备份文件夹",
            SuggestedStartLocation = await storage.TryGetFolderFromPathAsync(
                ViewModel.Config.BackupFolderPath),
            AllowMultiple = false
        };

        var result = await storage.OpenFolderPickerAsync(options);
        var folder = result.FirstOrDefault();

        if (folder is not null)
        {
            ViewModel.Config.BackupFolderPath = folder.Path.LocalPath;
        }
    }
}

public partial class SuperAutoBackupSettingsViewModel : ObservableObject
{
    [ObservableProperty]
    private ConfigData _config = GlobalConstants.Config!.Data;

    [ObservableProperty]
    private string _pluginVersion = GlobalConstants.Information.PluginVersion;
}