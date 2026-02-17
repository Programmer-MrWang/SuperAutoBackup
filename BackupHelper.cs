using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace SuperAutoBackup;

public static class BackupHelper
{
    private static readonly SemaphoreSlim _backupLock = new(1, 1);

    public static async Task CreateBackup(
        string backupFolderPath,
        bool generateLog,
        IProgress<double>? progress = null,
        CancellationToken cancellationToken = default)
    {
        await _backupLock.WaitAsync(cancellationToken);

        var sw = Stopwatch.StartNew();
        var tempPath = string.Empty;
        var skipCount = 0;

        try
        {
            var appPath = GetClassIslandPath();
            var appName = new DirectoryInfo(appPath).Name;

            tempPath = Path.Combine(Path.GetTempPath(),
                $"{appName}_Backup_{Guid.NewGuid():N}");

            Directory.CreateDirectory(backupFolderPath);
            Directory.CreateDirectory(tempPath);

            var filesToBackup = Directory
                .EnumerateFiles(appPath, "*", SearchOption.AllDirectories)
                .Where(f => !IsExcludedFile(f))
                .ToList();

            var totalFiles = filesToBackup.Count;
            var processedFiles = 0;

            var parallelOptions = new ParallelOptions
            {
                MaxDegreeOfParallelism = Environment.ProcessorCount,
                CancellationToken = cancellationToken
            };

            await Parallel.ForEachAsync(filesToBackup, parallelOptions, async (file, ct) =>
            {
                var relativePath = Path.GetRelativePath(appPath, file);
                var destFile = Path.Combine(tempPath, relativePath);
                var destDir = Path.GetDirectoryName(destFile)!;

                Directory.CreateDirectory(destDir);

                try
                {
                    await CopyFileAsync(file, destFile, ct);
                }
                catch
                {
                    Interlocked.Increment(ref skipCount);
                }

                var current = Interlocked.Increment(ref processedFiles);
                progress?.Report((double)current / totalFiles * 100);
            });

            // 创建压缩包
            var zipFileName = $"ClassIsland_Backup_{DateTime.Now:yyyyMMdd_HHmmss}.zip";
            var zipFilePath = Path.Combine(backupFolderPath, zipFileName);

            ZipFile.CreateFromDirectory(tempPath, zipFilePath,
                CompressionLevel.Optimal, false);

            // 日志
            if (generateLog)
            {
                await WriteLogAsync(backupFolderPath, zipFileName,
                    sw.Elapsed, skipCount, zipFilePath);
            }

            _ = Task.Run(async () =>
            {
                await Task.Delay(5000);
                TryDeleteDirectory(tempPath);
            });
        }
        finally
        {
            _backupLock.Release();
            sw.Stop();
        }
    }

    private static async Task CopyFileAsync(string source, string dest,
        CancellationToken ct)
    {
        const int bufferSize = 81920;

        await using var sourceStream = new FileStream(
            source, FileMode.Open, FileAccess.Read,
            FileShare.Read, bufferSize, true);

        await using var destStream = new FileStream(
            dest, FileMode.Create, FileAccess.Write,
            FileShare.None, bufferSize, true);

        await sourceStream.CopyToAsync(destStream, ct);
    }

    private static async Task WriteLogAsync(string backupFolderPath,
        string zipFileName, TimeSpan elapsed, int skipCount, string zipFilePath)
    {
        var logFileName = $"BackupLog_{DateTime.Now:yyyyMMdd}.txt";
        var logFilePath = Path.Combine(backupFolderPath, logFileName);

        var fileSize = new FileInfo(zipFilePath).Length / 1024 / 1024;
        var logEntry = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] " +
                      $"备份成功: {zipFileName} " +
                      $"(耗时: {elapsed.TotalSeconds:F2}秒, " +
                      $"跳过: {skipCount}个文件, " +
                      $"大小: {fileSize}MB)\n";

        await File.AppendAllTextAsync(logFilePath, logEntry);
    }

    private static string GetClassIslandPath()
    {
        // 定位目录
        var pluginDir = AppContext.BaseDirectory;
        return Directory.GetParent(pluginDir)?.Parent?.Parent?.FullName
            ?? throw new InvalidOperationException("无法定位 ClassIsland 目录");
    }

    private static bool IsExcludedFile(string filePath)
    {
        var fileName = Path.GetFileName(filePath).ToLowerInvariant();
        var extension = Path.GetExtension(filePath).ToLowerInvariant();

        var excludedExtensions = new[] { ".tmp", ".lock", ".log" };
        var excludedFiles = new[] { "classisland.exe", "backup_" };

        return excludedExtensions.Contains(extension) ||
               excludedFiles.Any(f => fileName.Contains(f));
    }

    private static void TryDeleteDirectory(string path)
    {
        try
        {
            if (Directory.Exists(path))
                Directory.Delete(path, true);
        }
        catch { /* ignore */ }
    }

    public static void CleanOldBackups(string backupFolderPath, int limit)
    {
        if (!Directory.Exists(backupFolderPath)) return;

        var backupFiles = Directory
            .EnumerateFiles(backupFolderPath, "ClassIsland_Backup_*.zip")
            .Select(f => new FileInfo(f))
            .OrderByDescending(f => f.CreationTime)
            .Skip(limit)
            .ToList();

        foreach (var file in backupFiles)
        {
            try { file.Delete(); } catch { /* ignore */ }
        }
    }
}