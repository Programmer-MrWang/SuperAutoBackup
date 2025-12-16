using SuperAutoBackup;
using ClassIsland.Core;
using ClassIsland.Core.Abstractions;
using ClassIsland.Core.Attributes;
using ClassIsland.Core.Extensions.Registry;
using ClassIsland.Shared.Helpers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading.Tasks;

namespace SuperAutoBackup;

[PluginEntrance]
public class Plugin : PluginBase
{
    public static BackupSettings Settings { get; private set; } = new();

    public override void Initialize(HostBuilderContext context, IServiceCollection services)
    {
        // 📌 日志：插件启动
        LogHelper.Write("PLUGIN_INIT", "插件开始初始化", PluginConfigFolder);

        var cfgFile = Path.Combine(PluginConfigFolder, "settings.json");

        try
        {
            Settings = ConfigureFileHelper.LoadConfig<BackupSettings>(cfgFile);
            LogHelper.Write("CONFIG_LOAD", $"配置加载成功：{cfgFile}", PluginConfigFolder);
        }
        catch (Exception ex)
        {
            LogHelper.WriteError(ex, PluginConfigFolder);
            Settings = new BackupSettings(); // 加载失败时用默认配置
        }

        Settings.PropertyChanged += (_, _) =>
        {
            ConfigureFileHelper.SaveConfig(cfgFile, Settings);
            LogHelper.Write("CONFIG_SAVE", "配置已保存", PluginConfigFolder);
        };

        services.AddSingleton(Settings);
        services.AddSettingsPage<AdvancedBackupSettingsPage>();

        // 启动时自动备份
        AppBase.Current.AppStarted += (_, _) =>
        {
            if (Settings.Enabled)
            {
                LogHelper.Write("BACKUP_TRIGGER", "触发启动自动备份", Settings.TargetPath);
                DoBackup();
            }
            else
            {
                LogHelper.Write("PLUGIN_START", "自动备份已禁用，仅记录启动", PluginConfigFolder);
            }
        };

        LogHelper.Write("PLUGIN_INIT", "插件初始化完成", PluginConfigFolder);
    }

    /// <summary>
    /// 供“立即备份”按钮调用
    /// </summary>
    public static void DoBackup()
    {
        var sw = Stopwatch.StartNew(); // 计时
        var src = "";
        var tempDir = "";
        var dstDir = "";
        var zip = "";
        var skippedFiles = new List<string>();

        LogHelper.Write("BACKUP_START", "备份任务开始", Settings.TargetPath);

        try
        {
            // 1. 获取源目录
            src = Path.GetDirectoryName(Process.GetCurrentProcess().MainModule?.FileName)
                ?? throw new Exception("无法获取源目录");
            LogHelper.Write("SOURCE_DIR", $"源目录：{src}", Settings.TargetPath);

            // 2. 创建 Temp 副本目录
            var tempRoot = Path.GetTempPath();
            tempDir = Path.Combine(tempRoot, $"SuperAutoBackup_{Guid.NewGuid()}");
            Directory.CreateDirectory(tempDir);
            LogHelper.Write("TEMP_CREATE", $"临时目录：{tempDir}", Settings.TargetPath);

            // 3. 复制到 Temp（跳过占用）
            CopyDirectorySkippingLockedFiles(src, tempDir, skippedFiles);
            LogHelper.Write("COPY_DONE", $"复制完成，跳过 {skippedFiles.Count} 个文件", Settings.TargetPath);

            // 4. 生成 ZIP 信息
            dstDir = Settings.TargetPath;
            Directory.CreateDirectory(dstDir);
            LogHelper.Write("TARGET_DIR", $"目标目录：{dstDir}", Settings.TargetPath);

            var ver = AppBase.AppVersion?.ToString()?.Replace('.', '_') ?? "Unknown";
            var ts = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            zip = Path.Combine(dstDir, $"ClassIsland_v{ver}_{ts}.zip");
            LogHelper.Write("ZIP_PREPARE", $"准备压缩：{zip}", Settings.TargetPath);

            // 5. 压缩 Temp 副本
            ZipFile.CreateFromDirectory(tempDir, zip, CompressionLevel.Optimal, false);
            LogHelper.Write("ZIP_DONE", $"压缩完成（{new FileInfo(zip).Length / 1024 / 1024} MB）", Settings.TargetPath);

            // 6. 清理旧备份
            var keeps = Settings.MaxBackups;
            var old = Directory.GetFiles(dstDir, "ClassIsland_v*.zip")
                               .OrderByDescending(File.GetCreationTime)
                               .Skip(keeps);
            var deletedCount = 0;
            foreach (var f in old)
            {
                File.Delete(f);
                deletedCount++;
            }
            if (deletedCount > 0)
                LogHelper.Write("CLEAN_OLD", $"删除 {deletedCount} 个旧备份", Settings.TargetPath);

            // 7. 异步删除 Temp
            Task.Delay(10000).ContinueWith(_ =>
            {
                try
                {
                    Directory.Delete(tempDir, recursive: true);
                    LogHelper.Write("TEMP_DELETE", $"临时目录已删除：{tempDir}", Settings.TargetPath);
                }
                catch (Exception ex)
                {
                    LogHelper.WriteError(ex, Settings.TargetPath);
                }
            });

            sw.Stop();
            LogHelper.Write("BACKUP_SUCCESS", $"备份成功（耗时 {sw.Elapsed.TotalSeconds:F2} 秒）", Settings.TargetPath);
        }
        catch (Exception ex)
        {
            sw.Stop();
            LogHelper.Write("BACKUP_ERROR", $"备份失败（耗时 {sw.Elapsed.TotalSeconds:F2} 秒）", Settings.TargetPath);
            LogHelper.WriteError(ex, Settings.TargetPath);
        }
    }

    /// <summary>
    /// 递归复制目录，跳过被占用的文件
    /// </summary>
    private static void CopyDirectorySkippingLockedFiles(string sourceDir, string destDir, List<string> skippedFiles)
    {
        foreach (var file in Directory.GetFiles(sourceDir))
        {
            try
            {
                var destFile = Path.Combine(destDir, Path.GetFileName(file));
                File.Copy(file, destFile, overwrite: true);
            }
            catch (IOException ex)
            {
                skippedFiles.Add(file);
                LogHelper.Write("COPY_SKIP", $"跳过文件：{file}（{ex.Message}）", Settings.TargetPath);
            }
        }

        foreach (var dir in Directory.GetDirectories(sourceDir))
        {
            var destSubDir = Path.Combine(destDir, Path.GetFileName(dir));
            Directory.CreateDirectory(destSubDir);
            CopyDirectorySkippingLockedFiles(dir, destSubDir, skippedFiles);
        }
    }

    public void Dispose() { }
}