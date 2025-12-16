using ClassIsland.Core;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace SuperAutoBackup;

public class AutoBackupService : IHostedService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly Settings _settings;

    public AutoBackupService(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
        _settings = serviceProvider.GetRequiredService<Settings>();
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
        if (!_settings.IsAutoBackupEnabled) return;

        try
        {
            await Task.Delay(5000);

            _settings.IsBackupInProgress = true;
            _settings.BackupProgress = 0;

            var progress = new Progress<double>(value => _settings.BackupProgress = value);

            await Task.Run(async () =>
            {
                await BackupHelper.CreateBackup(_settings.BackupFolderPath, _settings.IsLogGenerationEnabled, progress);
                BackupHelper.CleanOldBackups(_settings.BackupFolderPath, _settings.BackupCountLimit);
            });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"自动备份失败: {ex.Message}");
        }
        finally
        {
            _settings.IsBackupInProgress = false;
        }
    }
}