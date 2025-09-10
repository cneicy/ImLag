using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Win32;

// ReSharper disable InconsistentNaming

namespace ImLag.GUI.Scripts.Core;

public class CfgManager
{
    private readonly ChatMessageManager _chatManager;
    private readonly ConfigManager _configManager;
    private readonly Random _random = new();

    public string SteamPath { get; private set; } = string.Empty;
    public string CS2Path => _configManager.Config.CS2Path;
    public string CfgPath { get; private set; } = string.Empty;
    public int TotalCfgFiles => GetExistingCfgFileCount();
    public List<string> BindKeys => _configManager.Config.BindKeys;
    public List<string> TeamBindKeys => _configManager.Config.TeamBindKeys;

    private const string AutoexecBackupSuffix = ".imlag_backup";
    private const string ImLagCommentStart = "// --- ImLag Auto-Bind Start ---";
    private const string ImLagCommentEnd = "// --- ImLag Auto-Bind End ---";

    public event EventHandler<string>? StatusChanged;
    public event EventHandler<string>? ErrorOccurred;

    public CfgManager(ChatMessageManager chatManager, ConfigManager configManager)
    {
        _chatManager = chatManager;
        _configManager = configManager;

        if (string.IsNullOrEmpty(CS2Path))
        {
            FindCS2Path();
        }
        else
        {
            UpdateCfgPath();
        }
    }

    public void FindCS2Path()
    {
        try
        {
            using var regKey = Registry.CurrentUser.OpenSubKey(@"Software\Valve\Steam");
            SteamPath = regKey?.GetValue("SteamPath") as string ?? string.Empty;

            if (!string.IsNullOrEmpty(SteamPath))
            {
                var libraryFoldersPath = Path.Combine(SteamPath, "steamapps", "libraryfolders.vdf");
                List<string> steamLibraries = new() { SteamPath };

                if (File.Exists(libraryFoldersPath))
                {
                    var lines = File.ReadAllLines(libraryFoldersPath);
                    steamLibraries.AddRange(from line in lines
                        select line.Trim()
                        into trimmedLine
                        where trimmedLine.StartsWith("\"path\"")
                        select trimmedLine.Split('\"')[3]
                        into path
                        where Directory.Exists(path)
                        select path);
                }

                string[] possibleRelativePaths =
                {
                    Path.Combine("steamapps", "common", "Counter-Strike Global Offensive"),
                    Path.Combine("steamapps", "common", "Counter-Strike 2")
                };

                foreach (var libPath in steamLibraries.Distinct())
                {
                    foreach (var relativePath in possibleRelativePaths)
                    {
                        var potentialCs2Path = Path.Combine(libPath, relativePath);
                        if (!Directory.Exists(potentialCs2Path) ||
                            !File.Exists(Path.Combine(potentialCs2Path, "game", "csgo", "pak01_dir.vpk")))
                            continue;

                        _configManager.UpdateCS2Path(potentialCs2Path);
                        UpdateCfgPath();
                        StatusChanged?.Invoke(this, $"检测到CS2游戏路径: {CS2Path}");
                        return;
                    }
                }
            }

            StatusChanged?.Invoke(this, "CS2路径未找到，请手动设置");
        }
        catch (Exception ex)
        {
            ErrorOccurred?.Invoke(this, $"检测CS2路径失败: {ex.Message}");
        }
    }

    private void UpdateCfgPath()
    {
        if (string.IsNullOrEmpty(CS2Path)) return;
        CfgPath = Path.Combine(CS2Path, "game", "csgo", "cfg");

        if (Directory.Exists(CfgPath)) return;
        try
        {
            Directory.CreateDirectory(CfgPath);
            StatusChanged?.Invoke(this, $"创建CFG文件夹: {CfgPath}");
        }
        catch (Exception ex)
        {
            ErrorOccurred?.Invoke(this, $"创建CFG文件夹失败: {ex.Message}");
        }
    }

    public bool SetCS2Path(string path)
    {
        if (Directory.Exists(path) && File.Exists(Path.Combine(path, "game", "csgo", "pak01_dir.vpk")))
        {
            _configManager.UpdateCS2Path(path);
            UpdateCfgPath();
            StatusChanged?.Invoke(this, $"将CS2路径设置为: {CS2Path}");
            return true;
        }

        ErrorOccurred?.Invoke(this, "非法路径");
        return false;
    }

    public bool AddBindKey(string key)
    {
        if (!string.IsNullOrWhiteSpace(key) && key.Length == 1 && char.IsLetterOrDigit(key[0]))
        {
            var normalizedKey = key.ToLower();
            if (!BindKeys.Contains(normalizedKey))
            {
                var newBindKeys = new List<string>(BindKeys) { normalizedKey };
                _configManager.UpdateBindKeys(newBindKeys);
                StatusChanged?.Invoke(this, $"已添加全局绑定快捷键: {normalizedKey}");
                return true;
            }

            ErrorOccurred?.Invoke(this, "此全局快捷键已存在");
            return false;
        }

        ErrorOccurred?.Invoke(this, "快捷键非法，请使用单个字母作为快捷键");
        return false;
    }

    public bool RemoveBindKey(string key)
    {
        if (BindKeys.Count <= 1)
        {
            ErrorOccurred?.Invoke(this, "最少保留一个全局快捷键");
            return false;
        }

        var normalizedKey = key.ToLower();
        if (BindKeys.Contains(normalizedKey))
        {
            var newBindKeys = BindKeys.Where(k => k != normalizedKey).ToList();
            _configManager.UpdateBindKeys(newBindKeys);
            StatusChanged?.Invoke(this, $"已删除全局快捷键: {normalizedKey}");
            return true;
        }

        ErrorOccurred?.Invoke(this, "全局快捷键未找到");
        return false;
    }

    public bool AddTeamBindKey(string key)
    {
        if (!string.IsNullOrWhiteSpace(key) && key.Length == 1 && char.IsLetterOrDigit(key[0]))
        {
            var normalizedKey = key.ToLower();
            if (!TeamBindKeys.Contains(normalizedKey))
            {
                var newTeamBindKeys = new List<string>(TeamBindKeys) { normalizedKey };
                _configManager.UpdateTeamBindKeys(newTeamBindKeys);
                StatusChanged?.Invoke(this, $"已添加队内快捷键: {normalizedKey}");
                return true;
            }

            ErrorOccurred?.Invoke(this, "此队内快捷键已存在");
            return false;
        }

        ErrorOccurred?.Invoke(this, "快捷键非法，请使用单个字母作为快捷键");
        return false;
    }

    public bool RemoveTeamBindKey(string key)
    {
        if (TeamBindKeys.Count <= 1)
        {
            ErrorOccurred?.Invoke(this, "最少保留一个队内快捷键");
            return false;
        }

        var normalizedKey = key.ToLower();
        if (TeamBindKeys.Contains(normalizedKey))
        {
            var newTeamBindKeys = TeamBindKeys.Where(k => k != normalizedKey).ToList();
            _configManager.UpdateTeamBindKeys(newTeamBindKeys);
            StatusChanged?.Invoke(this, $"已删除队内快捷键: {normalizedKey}");
            return true;
        }

        ErrorOccurred?.Invoke(this, "队内快捷键未找到");
        return false;
    }

    public string GetRandomBindKey()
    {
        return BindKeys.Count > 0 ? BindKeys[_random.Next(BindKeys.Count)] : "k";
    }

    public string GetRandomTeamBindKey()
    {
        return TeamBindKeys.Count > 0 ? TeamBindKeys[_random.Next(TeamBindKeys.Count)] : "l";
    }
    
    public string GetRandomBindKey(bool preferTeam = false)
    {
        if (preferTeam && TeamBindKeys.Count > 0)
        {
            return GetRandomTeamBindKey();
        }

        if (BindKeys.Count > 0)
        {
            return GetRandomBindKey();
        }

        return TeamBindKeys.Count > 0 ? GetRandomTeamBindKey() : "k";
    }

    private string EscapeMessageForCfg(string message)
    {
        message = message.Replace("\"", "\"\"");
        message = message.Replace(";", "");
        return message;
    }

    public bool GenerateConfigFiles()
    {
        var messages = _chatManager.GetAllMessages();
        if (messages.Count == 0)
        {
            ErrorOccurred?.Invoke(this, "消息列表为空，请先添加一条消息");
            return false;
        }

        if (string.IsNullOrEmpty(CS2Path) || !Directory.Exists(CS2Path))
        {
            ErrorOccurred?.Invoke(this, "CS2游戏路径不存在或路径非法");
            return false;
        }

        if (string.IsNullOrEmpty(CfgPath) || !Directory.Exists(CfgPath))
        {
            UpdateCfgPath();
            if (!Directory.Exists(CfgPath))
            {
                ErrorOccurred?.Invoke(this, "CFG文件夹不存在或不能创建");
                return false;
            }
        }

        try
        {
            var shuffledMessages = messages.OrderBy(_ => _random.Next()).ToList();
            var actualTotalFiles = Math.Min(TotalCfgFiles, shuffledMessages.Count);
            
            for (var i = 0; i < actualTotalFiles; i++)
            {
                var filename = $"imlag_say_global_{i + 1}.cfg";
                var filePath = Path.Combine(CfgPath, filename);
                var messageToUse = EscapeMessageForCfg(shuffledMessages[i]);

                using var writer = new StreamWriter(filePath, false);
                writer.WriteLine($"// ImLag Global Chat CFG - File {i + 1}");
                writer.WriteLine($"// Message: {shuffledMessages[i]}");
                writer.WriteLine($"say \"{messageToUse}\"");
            }
            
            shuffledMessages = messages.OrderBy(_ => _random.Next()).ToList();
            
            for (var i = 0; i < actualTotalFiles; i++)
            {
                var filename = $"imlag_say_team_{i + 1}.cfg";
                var filePath = Path.Combine(CfgPath, filename);
                var messageToUse = EscapeMessageForCfg(shuffledMessages[i]);

                using var writer = new StreamWriter(filePath, false);
                writer.WriteLine($"// ImLag Team Chat CFG - File {i + 1}");
                writer.WriteLine($"// Message: {shuffledMessages[i]}");
                writer.WriteLine($"say_team \"{messageToUse}\"");
            }
            
            for (var i = actualTotalFiles; i < 10000; i++)
            {
                var oldGlobalFile = $"imlag_say_global_{i + 1}.cfg";
                var oldTeamFile = $"imlag_say_team_{i + 1}.cfg";
                var oldGlobalPath = Path.Combine(CfgPath, oldGlobalFile);
                var oldTeamPath = Path.Combine(CfgPath, oldTeamFile);
                
                if (File.Exists(oldGlobalPath))
                    File.Delete(oldGlobalPath);
                if (File.Exists(oldTeamPath))
                    File.Delete(oldTeamPath);
            }

            if (actualTotalFiles > 0)
            {
                GenerateSelectorFiles(actualTotalFiles);
                StatusChanged?.Invoke(this, $"已生成 {actualTotalFiles * 2} 个CFG 文件 (全局+队内)");
                return true;
            }

            ErrorOccurred?.Invoke(this, "没有足够的消息用以生成CFG");
            return false;
        }
        catch (Exception ex)
        {
            ErrorOccurred?.Invoke(this, $"生成CFG时出错: {ex.Message}");
            return false;
        }
    }

    private void GenerateSelectorFiles(int numberOfMessageFiles)
    {
        if (numberOfMessageFiles == 0) return;
        
        var globalSelectorPath = Path.Combine(CfgPath, "imlag_say_global_selector.cfg");
        using (var writer = new StreamWriter(globalSelectorPath, false))
        {
            writer.WriteLine("// ImLag Global Chat Selector CFG");
            writer.WriteLine($"// Cycles through {numberOfMessageFiles} global message CFGs.");
            writer.WriteLine();

            for (var i = 1; i <= numberOfMessageFiles; i++)
            {
                var nextFileIndex = i % numberOfMessageFiles + 1;
                writer.WriteLine(
                    $"alias imlag_global_say_{i} \"exec imlag_say_global_{i}; alias imlag_do_global_say imlag_global_say_{nextFileIndex}\"");
            }

            writer.WriteLine();
            writer.WriteLine("alias imlag_do_global_say imlag_global_say_1");
            writer.WriteLine("imlag_do_global_say");
        }
        
        var teamSelectorPath = Path.Combine(CfgPath, "imlag_say_team_selector.cfg");
        using (var writer = new StreamWriter(teamSelectorPath, false))
        {
            writer.WriteLine("// ImLag Team Chat Selector CFG");
            writer.WriteLine($"// Cycles through {numberOfMessageFiles} team message CFGs.");
            writer.WriteLine();

            for (var i = 1; i <= numberOfMessageFiles; i++)
            {
                var nextFileIndex = i % numberOfMessageFiles + 1;
                writer.WriteLine(
                    $"alias imlag_team_say_{i} \"exec imlag_say_team_{i}; alias imlag_do_team_say imlag_team_say_{nextFileIndex}\"");
            }

            writer.WriteLine();
            writer.WriteLine("alias imlag_do_team_say imlag_team_say_1");
            writer.WriteLine("imlag_do_team_say");
        }
    }

    public bool UpdateAutoexecFile()
    {
        if (string.IsNullOrEmpty(CfgPath) || !Directory.Exists(CfgPath))
        {
            ErrorOccurred?.Invoke(this, "CFG路径不存在或路径非法");
            return false;
        }

        var autoexecFilePath = Path.Combine(CfgPath, "autoexec.cfg");
        var backupFilePath = autoexecFilePath + AutoexecBackupSuffix;

        try
        {
            List<string> lines = [];
            var autoexecExists = File.Exists(autoexecFilePath);
                
            if (autoexecExists && !File.Exists(backupFilePath))
            {
                File.Copy(autoexecFilePath, backupFilePath);
                StatusChanged?.Invoke(this, "已创建 autoexec.cfg 备份");
            }

            if (autoexecExists)
            {
                lines.AddRange(File.ReadAllLines(autoexecFilePath));
                RemoveImLagSection(lines);
            }
            else
            {
                lines.Add("// Counter-Strike 2 Autoexec Configuration File");
                lines.Add("// Generated by ImLag");
                lines.Add("");
            }

            AddImLagSection(lines);
            File.WriteAllLines(autoexecFilePath, lines);
            StatusChanged?.Invoke(this, "更新 autoexec.cfg 完成");
            return true;
        }
        catch (Exception ex)
        {
            ErrorOccurred?.Invoke(this, $"更新 autoexec.cfg 时出错: {ex.Message}");
            return false;
        }
    }

    private void RemoveImLagSection(List<string> lines)
    {
        int startIndex = -1, endIndex = -1;
        for (var i = 0; i < lines.Count; i++)
        {
            if (lines[i].Trim() == ImLagCommentStart) startIndex = i;
            if (lines[i].Trim() != ImLagCommentEnd || startIndex == -1) continue;
            endIndex = i;
            break;
        }

        if (startIndex != -1 && endIndex != -1)
        {
            lines.RemoveRange(startIndex, endIndex - startIndex + 1);
        }
        
        lines.RemoveAll(line => line.Contains("exec imlag_say_selector") || 
                               line.Contains("exec imlag_say_global_selector") ||
                               line.Contains("exec imlag_say_team_selector"));
    }

    private void AddImLagSection(List<string> lines)
    {
        lines.Add("");
        lines.Add(ImLagCommentStart);
        lines.Add("// This block is automatically managed by ImLag");
        
        foreach (var key in BindKeys)
        {
            lines.Add($"bind \"{key}\" \"exec imlag_say_global_selector\"");
            lines.Add($"echo \"ImLag: '{key}' bound to global chat selector.\"");
        }
        
        foreach (var key in TeamBindKeys)
        {
            lines.Add($"bind \"{key}\" \"exec imlag_say_team_selector\"");
            lines.Add($"echo \"ImLag: '{key}' bound to team chat selector.\"");
        }
        
        lines.Add(ImLagCommentEnd);
        lines.Add("");

        lines.RemoveAll(line => line.Trim().ToLower() == "host_writeconfig");
        lines.Add("host_writeconfig");
    }
    
    public int GetExistingCfgFileCount()
    {
        if (string.IsNullOrEmpty(CfgPath) || !Directory.Exists(CfgPath))
        {
            return 0;
        }

        try
        {
            var cfgFiles = Directory.GetFiles(CfgPath, "imlag_*.cfg");
            return cfgFiles.Length;
        }
        catch (Exception ex)
        {
            ErrorOccurred?.Invoke(this, $"Error counting existing CFG files: {ex.Message}");
            return 0;
        }
    }

    public bool RestoreOriginalCfg()
    {
        if (string.IsNullOrEmpty(CfgPath) || !Directory.Exists(CfgPath))
        {
            ErrorOccurred?.Invoke(this, "CFG path is invalid or not set.");
            return false;
        }

        try
        {
            var autoexecFilePath = Path.Combine(CfgPath, "autoexec.cfg");
            var backupFilePath = autoexecFilePath + AutoexecBackupSuffix;
            var hasChanges = false;
                
            if (File.Exists(backupFilePath))
            {
                File.Copy(backupFilePath, autoexecFilePath, true);
                File.Delete(backupFilePath);
                hasChanges = true;
            }
            else if (File.Exists(autoexecFilePath))
            {
                var lines = File.ReadAllLines(autoexecFilePath).ToList();
                var initialCount = lines.Count;
                    
                RemoveImLagSection(lines);
                    
                if (lines.Count != initialCount)
                {
                    hasChanges = true;
                    File.WriteAllLines(autoexecFilePath, lines);
                }
            }
                
            var cfgFiles = Directory.GetFiles(CfgPath, "imlag_*.cfg");
            foreach (var file in cfgFiles)
            {
                File.Delete(file);
                hasChanges = true;
            }

            if (hasChanges)
            {
                StatusChanged?.Invoke(this, "已成功还原原CFG设置");
                return true;
            }

            StatusChanged?.Invoke(this, "没有备份文件用以还原");
            return false;
        }
        catch (Exception ex)
        {
            ErrorOccurred?.Invoke(this, $"还原CFG时出错: {ex.Message}");
            return false;
        }
    }
}