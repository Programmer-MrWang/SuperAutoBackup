using ClassIsland.Core;
using ClassIsland.Core.Abstractions;
using ClassIsland.Core.Attributes;
using ClassIsland.Core.Extensions.Registry;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SuperAutoBackup.ConfigHandlers;
using SuperAutoBackup.Shared;
using System;

namespace SuperAutoBackup;

[PluginEntrance]
public class Plugin : PluginBase
{
    public override void Initialize(HostBuilderContext context, IServiceCollection services)
    {
        GlobalConstants.PluginConfigFolder = PluginConfigFolder;
        GlobalConstants.Information.PluginFolder = Info.PluginFolderPath;
        GlobalConstants.Information.PluginVersion = Info.Manifest.Version;
        GlobalConstants.Config = new ConfigHandler(PluginConfigFolder);

        Console.WriteLine($"[SuperAutoBackup] 自动备份: {GlobalConstants.Config.Data.IsAutoBackupEnabled}");

        services.AddSettingsPage<SuperAutoBackupSettingsPage>();
        services.AddHostedService<AutoBackupService>();
    }
}