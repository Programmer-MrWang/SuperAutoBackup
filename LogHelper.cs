using System;
using System.IO;
using System.Diagnostics;

namespace SuperAutoBackup;

public static class LogHelper
{
    /// <summary>
    /// 完整日志：带时间戳、操作类型、详情
    /// </summary>
    public static void Write(string operation, string detail, string logFolder)
    {
        if (!SuperAutoBackup.Plugin.Settings.EnableLog) return;
        var path = Path.Combine(logFolder, "SuperAutoBackup_Full.log");
        var msg = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] [{operation,-12}] {detail}";
        File.AppendAllText(path, msg + "\n");
    }

    /// <summary>
    /// 错误日志：带堆栈
    /// </summary>
    public static void WriteError(Exception ex, string logFolder)
    {
        if (!SuperAutoBackup.Plugin.Settings.EnableLog) return;
        var path = Path.Combine(logFolder, "SuperAutoBackup_Error.log");
        var msg = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] ❌ {ex.Message}\n{ex.StackTrace}\n";
        File.AppendAllText(path, msg);
    }
}