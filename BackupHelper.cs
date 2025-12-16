using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;    // ✅ 新增
using System.Threading.Tasks;

namespace SuperAutoBackup;

public static class BackupHelper
{
    public static async Task CreateBackup(string backupFolderPath, bool generateLog, IProgress<double>? progress = null)
    {
        var sw = Stopwatch.StartNew();
        var tempPath = string.Empty;
        var zipFilePath = string.Empty;
        var skipCount = 0;

        try
        {
            // ✅ 从插件 dll 位置向上 4 层定位 ClassIsland 根目录
            var dllPath = Assembly.GetExecutingAssembly().Location;
            var pluginDir = Path.GetDirectoryName(dllPath)!;
            var appPath = Directory.GetParent(pluginDir)!.Parent!.Parent!.FullName;

            var appName = new DirectoryInfo(appPath).Name;

            tempPath = Path.Combine(Path.GetTempPath(), $"{appName}_Backup_{Guid.NewGuid()}");
            Directory.CreateDirectory(backupFolderPath);

            // ✅ 统计总文件数
            var allFiles = Directory.GetFiles(appPath, "*", SearchOption.AllDirectories);
            var totalFiles = allFiles.Length;

            // ✅ 复制并报告进度
            skipCount = CopyDirectoryWithProgress(appPath, tempPath, true, totalFiles, progress);

            var zipFileName = $"ClassIsland_Backup_{DateTime.Now:yyyyMMdd_HHmmss}.zip";
            zipFilePath = Path.Combine(backupFolderPath, zipFileName);

            ZipFile.CreateFromDirectory(tempPath, zipFilePath, CompressionLevel.Optimal, false);

            if (generateLog)
            {
                var logFileName = $"BackupLog_{DateTime.Now:yyyyMMdd}.txt";
                var logFilePath = Path.Combine(backupFolderPath, logFileName);

                var logEntry = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] 备份成功: {zipFileName} " +
                              $"(耗时: {sw.Elapsed.TotalSeconds:F2}秒, 跳过: {skipCount}个文件, " +
                              $"大小: {new FileInfo(zipFilePath).Length / 1024 / 1024}MB)\n";
                File.AppendAllText(logFilePath, logEntry);
            }

            _ = Task.Delay(10000).ContinueWith(_ =>
            {
                try
                {
                    if (Directory.Exists(tempPath))
                        Directory.Delete(tempPath, true);
                }
                catch { /* ignore */ }
            });
        }
        catch (Exception ex)
        {
            if (generateLog)
            {
                var logFileName = $"BackupLog_{DateTime.Now:yyyyMMdd}.txt";
                var logFilePath = Path.Combine(backupFolderPath, logFileName);

                var logEntry = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] 备份失败: {ex.Message} " +
                              $"(耗时: {sw.Elapsed.TotalSeconds:F2}秒)\n";
                File.AppendAllText(logFilePath, logEntry);
            }
            Console.WriteLine($"备份失败: {ex.Message}");
        }
        finally
        {
            sw.Stop();
        }
    }

    private static int CopyDirectoryWithProgress(string sourceDir, string destDir, bool recursive, int totalFiles, IProgress<double>? progress)
    {
        var dir = new DirectoryInfo(sourceDir);
        if (!dir.Exists) throw new DirectoryNotFoundException($"源目录不存在: {sourceDir}");

        Directory.CreateDirectory(destDir);
        var skipCount = 0;
        var processedFiles = 0;

        // 复制文件
        foreach (var file in dir.GetFiles("*", SearchOption.TopDirectoryOnly))
        {
            try
            {
                string targetFilePath = Path.Combine(destDir, file.Name);
                file.CopyTo(targetFilePath, true);
                processedFiles++;
                progress?.Report((double)processedFiles / totalFiles * 100);
            }
            catch (IOException)
            {
                skipCount++;
                processedFiles++;
                progress?.Report((double)processedFiles / totalFiles * 100);
            }
            catch (Exception)
            {
                skipCount++;
                processedFiles++;
                progress?.Report((double)processedFiles / totalFiles * 100);
            }
        }

        // 递归复制子目录
        if (recursive)
        {
            foreach (var subDir in dir.GetDirectories())
            {
                string newDestinationDir = Path.Combine(destDir, subDir.Name);
                skipCount += CopyDirectoryWithProgress(subDir.FullName, newDestinationDir, true, totalFiles, progress);
            }
        }

        return skipCount;
    }

    public static void CleanOldBackups(string backupFolderPath, int limit)
    {
        if (!Directory.Exists(backupFolderPath)) return;

        var backupFiles = Directory.GetFiles(backupFolderPath, "ClassIsland_Backup_*.zip")
            .Select(f => new FileInfo(f))
            .OrderByDescending(f => f.CreationTime)
            .ToList();

        if (backupFiles.Count > limit)
        {
            var filesToDelete = backupFiles.Skip(limit);
            foreach (var file in filesToDelete)
            {
                try
                {
                    file.Delete();
                }
                catch
                {
                    // 忽略删除失败
                }
            }
        }
    }
}