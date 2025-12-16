using ClassIsland.Core;
using ClassIsland.Core.Abstractions;
using ClassIsland.Core.Attributes;
using ClassIsland.Core.Extensions.Registry;
using ClassIsland.Shared.Helpers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.IO;

namespace SuperAutoBackup;

[PluginEntrance]
public class Plugin : PluginBase
{
    private string _settingsPath = string.Empty;
    public Settings Settings { get; private set; } = new();

    public override void Initialize(HostBuilderContext context, IServiceCollection services)
    {
        // ✅ 加载配置
        _settingsPath = Path.Combine(PluginConfigFolder, "Settings.json");
        Settings = File.Exists(_settingsPath)
            ? ConfigureFileHelper.LoadConfig<Settings>(_settingsPath)
            : new Settings();

        // ✅ 自动保存配置
        Settings.PropertyChanged += (_, _) => ConfigureFileHelper.SaveConfig(_settingsPath, Settings);

        // 注册服务
        services.AddSingleton(Settings);
        services.AddSettingsPage<SuperAutoBackupSettingsPage>();
        services.AddHostedService<AutoBackupService>();
    }
}