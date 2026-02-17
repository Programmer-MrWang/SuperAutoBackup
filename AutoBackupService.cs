using ClassIsland.Core;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SuperAutoBackup.ConfigHandlers;
using SuperAutoBackup.Shared;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace SuperAutoBackup;

public class AutoBackupService : IHostedService
{
    private readonly ConfigData _config;

    public AutoBackupService(IServiceProvider serviceProvider)
    {
        _config = GlobalConstants.Config!.Data;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        AppBase.Current.AppStarted += OnAppStarted;
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        AppBase.Current.AppStarted -= OnAppStarted;
        return Task.CompletedTask;
    }

    private async void OnAppStarted(object? sender, EventArgs e)
    {
        if (!_config.IsAutoBackupEnabled) return;

        try
        {
            await Task.Delay(TimeSpan.FromSeconds(10));

            _config.IsBackupInProgress = true;
            _config.BackupProgress = 0;

            var progress = new Progress<double>(value =>
                _config.BackupProgress = value);

            await BackupHelper.CreateBackup(
                _config.BackupFolderPath,
                _config.IsLogGenerationEnabled,
                progress);

            BackupHelper.CleanOldBackups(
                _config.BackupFolderPath,
                _config.BackupCountLimit);

            Console.WriteLine("[SuperAutoBackup] 自动备份完成");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[SuperAutoBackup] 自动备份失败: {ex.Message}");
        }
        finally
        {
            _config.IsBackupInProgress = false;
        }
    }
}