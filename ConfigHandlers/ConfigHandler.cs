// SuperAutoBackup/ConfigHandlers/ConfigHandler.cs
using ClassIsland.Shared.Helpers;
using SuperAutoBackup.Shared;
using System;
using System.ComponentModel;
using System.IO;

namespace SuperAutoBackup.ConfigHandlers;

public class ConfigHandler
{
    readonly string _configPath;

    // 公开 Data 属性
    public ConfigData Data { get; private set; }

    public ConfigHandler(string pluginConfigFolder)
    {
        _configPath = Path.Combine(pluginConfigFolder, "Main.json");
        Data = new ConfigData();

        InitializeConfig();
    }

    void InitializeConfig()
    {
        if (!File.Exists(_configPath))
        {
            Save();
            return;
        }

        try
        {
            Data = ConfigureFileHelper.LoadConfig<ConfigData>(_configPath);
            SubscribeToChanges();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[SuperAutoBackup][ConfigHandler] 加载配置失败: {ex.Message}");
            File.Delete(_configPath);
            Data = new ConfigData();
            SubscribeToChanges();
            Save();
        }
    }

    void SubscribeToChanges()
    {
        Data.PropertyChanged += OnPropertyChanged;
    }

    void OnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        Save();
    }

    public void Save()
    {
        try
        {
            ConfigureFileHelper.SaveConfig(_configPath, Data);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[SuperAutoBackup][ConfigHandler] 保存配置失败: {ex.Message}");
        }
    }
}