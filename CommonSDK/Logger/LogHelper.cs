using System.IO.Compression;
using System.Text;
using Godot;

namespace CommonSDK.Logger;

/// <summary>
/// 日志帮助类
/// <para>实现ILogger接口，负责日志输出到控制台和文件</para>
/// </summary>
public class LogHelper : ILogger
{
    private readonly string _context;
    private static readonly string LogDir = OS.GetUserDataDir() + "/logs/";
    private static readonly string LogFileName = $"log_{DateTime.Now:yyyyMMdd_HHmmss}.txt";
    private static readonly string LogFilePath = Path.Combine(LogDir, LogFileName);
    private static readonly object LockObject = new();
    private static StreamWriter _logWriter;
    private static bool _isInitialized;

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="context">日志上下文名称</param>
    public LogHelper(string context)
    {
        _context = context;
        Initialize();
    }

    private static void Initialize()
    {
        if (_isInitialized) return;

        lock (LockObject)
        {
            if (_isInitialized) return;

            InitializeLogDirectory();
            CheckAndCompressOldLogs();
            InitializeLogFile();
            _isInitialized = true;

            AppDomain.CurrentDomain.ProcessExit += (_, _) => OnExit();
        }
    }

    private static void InitializeLogDirectory()
    {
        if (Directory.Exists(LogDir)) return;
        Directory.CreateDirectory(LogDir);
        GD.Print($"创建日志目录: {LogDir}");
    }

    private static void InitializeLogFile()
    {
        try
        {
            lock (LockObject)
            {
                _logWriter = new StreamWriter(LogFilePath, true, Encoding.UTF8)
                {
                    AutoFlush = true
                };
            }
        }
        catch (Exception ex)
        {
            GD.PrintErr($"初始化日志文件失败: {ex.Message}");
        }
    }

    private static void CheckAndCompressOldLogs()
    {
        try
        {
            var logFiles = Directory.GetFiles(LogDir, "*.txt");
            foreach (var logFile in logFiles)
            {
                if (logFile == LogFilePath) continue;
                CompressLogFile(logFile);
            }
        }
        catch (Exception ex)
        {
            GD.PrintErr($"检查旧日志文件失败: {ex.Message}");
        }
    }

    private static void CompressLogFile(string logFilePath)
    {
        try
        {
            if (!File.Exists(logFilePath)) return;

            var zipPath = Path.ChangeExtension(logFilePath, ".zip");
            using (var zipArchive = ZipFile.Open(zipPath, ZipArchiveMode.Create))
            {
                zipArchive.CreateEntryFromFile(logFilePath, Path.GetFileName(logFilePath));
            }

            File.Delete(logFilePath);
            GD.Print($"已压缩日志: {zipPath}");
        }
        catch (Exception ex)
        {
            GD.PrintErr($"压缩日志文件失败: {logFilePath} - {ex.Message}");
        }
    }

    private static void OnExit()
    {
        try
        {
            lock (LockObject)
            {
                if (_logWriter != null)
                {
                    _logWriter.Flush();
                    _logWriter.Close();
                    _logWriter.Dispose();
                    _logWriter = null;
                }
                CompressLogFile(LogFilePath);
            }
        }
        catch (Exception ex)
        {
            GD.PrintErr($"关闭并压缩日志失败: {ex.Message}");
        }
    }

    /// <summary>
    /// 记录日志
    /// </summary>
    /// <param name="type">日志类型</param>
    /// <param name="msg">日志消息</param>
    public void Log(LogType type, string msg)
    {
        var logEntry = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] [{type}] [{_context}] {msg}";
        GD.Print(logEntry);
        WriteToFile(type, msg);
    }

    /// <summary>
    /// 记录信息日志
    /// </summary>
    /// <param name="msg">日志消息</param>
    public void LogInfo(string msg) => Log(LogType.Info, msg);

    /// <summary>
    /// 记录警告日志
    /// </summary>
    /// <param name="msg">日志消息</param>
    public void LogWarn(string msg) => Log(LogType.Warn, msg);

    /// <summary>
    /// 记录错误日志
    /// </summary>
    /// <param name="msg">日志消息</param>
    public void LogError(string msg) => Log(LogType.Error, msg);

    /// <summary>
    /// 记录致命日志
    /// </summary>
    /// <param name="msg">日志消息</param>
    public void LogFatal(string msg) => Log(LogType.Fatal, msg);

    private void WriteToFile(LogType type, string msg)
    {
        try
        {
            lock (LockObject)
            {
                if (_logWriter == null) return;
                var logEntry = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] [{type}] [{_context}] {msg}";
                _logWriter.WriteLine(logEntry);
            }
        }
        catch (Exception ex)
        {
            GD.PrintErr($"写入日志失败: {ex.Message}");
        }
    }
}