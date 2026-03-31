using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using CommonSDK.Event;
using Microsoft.Win32;

// ReSharper disable InconsistentNaming

namespace ImLag.GUI.Scripts.Core;

public class CfgStatusChangedEvent : EventBase
{
    public string Message { get; }
    public DateTime Timestamp { get; }
    
    public CfgStatusChangedEvent(string message)
    {
        Message = message;
        Timestamp = DateTime.UtcNow;
    }
}

public class CfgErrorOccurredEvent : EventBase
{
    public string ErrorMessage { get; }
    public string OperationType { get; set; }
    public DateTime Timestamp { get; }
    
    public CfgErrorOccurredEvent(string errorMessage, string operationType = "")
    {
        ErrorMessage = errorMessage;
        OperationType = operationType;
        Timestamp = DateTime.UtcNow;
    }
}

[SuppressMessage("ReSharper", "UnusedMember.Global")]
[SuppressMessage("Interoperability", "CA1416:验证平台兼容性")]
public class CfgFilesChangedEvent : EventBase;

[SuppressMessage("Interoperability", "CA1416:验证平台兼容性")]
public class CfgManager
{
    private readonly ChatMessageManager _chatManager;
    private readonly ConfigManager _configManager;
    private readonly Random _random = new();

    public string SteamPath { get; private set; } = string.Empty;
    public string CS2Path => _configManager.Config.CS2Path;
    public string CfgPath { get; private set; } = string.Empty;
    public int GeneratedCfgGroups => GetGeneratedCfgGroupCount();
    public List<string> BindKeys => _configManager.Config.BindKeys;
    public List<string> TeamBindKeys => _configManager.Config.TeamBindKeys;

    private const string AutoexecBackupSuffix = ".imlag_backup";
    private const string ImLagCommentStart = "// --- ImLag Auto-Bind Start ---";
    private const string ImLagCommentEnd = "// --- ImLag Auto-Bind End ---";

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

    private void StatusChanged(string message)
    {
        EventBus.TriggerEvent(new CfgStatusChangedEvent(message));
    }

    private void ErrorOccurred(string errorMessage, string operationType = "")
    {
        EventBus.TriggerEvent(new CfgErrorOccurredEvent(errorMessage, operationType));
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
                        StatusChanged(LocalizationManager.T("cfg.status.detected_path", CS2Path));
                        return;
                    }
                }
            }

            StatusChanged(LocalizationManager.T("cfg.status.path_not_found"));
        }
        catch (Exception ex)
        {
            ErrorOccurred(LocalizationManager.T("cfg.error.detect_path_failed", ex.Message), "FindCS2Path");
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
            StatusChanged(LocalizationManager.T("cfg.status.created_cfg_dir", CfgPath));
        }
        catch (Exception ex)
        {
            ErrorOccurred(LocalizationManager.T("cfg.error.create_cfg_dir_failed", ex.Message), "UpdateCfgPath");
        }
    }

    public bool SetCS2Path(string path)
    {
        if (Directory.Exists(path) && File.Exists(Path.Combine(path, "game", "csgo", "pak01_dir.vpk")))
        {
            _configManager.UpdateCS2Path(path);
            UpdateCfgPath();
            StatusChanged(LocalizationManager.T("cfg.status.set_path", CS2Path));
            return true;
        }

        ErrorOccurred(LocalizationManager.T("cfg.error.invalid_path"), "SetCS2Path");
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
                StatusChanged(LocalizationManager.T("cfg.status.added_global_key", normalizedKey));
                return true;
            }

            ErrorOccurred(LocalizationManager.T("cfg.error.duplicate_global_key"), "AddBindKey");
            return false;
        }

        ErrorOccurred(LocalizationManager.T("cfg.error.invalid_key"), "AddBindKey");
        return false;
    }

    public bool RemoveBindKey(string key)
    {
        if (BindKeys.Count <= 1)
        {
            ErrorOccurred(LocalizationManager.T("cfg.error.keep_one_global_key"), "RemoveBindKey");
            return false;
        }

        var normalizedKey = key.ToLower();
        if (BindKeys.Contains(normalizedKey))
        {
            var newBindKeys = BindKeys.Where(k => k != normalizedKey).ToList();
            _configManager.UpdateBindKeys(newBindKeys);
            StatusChanged(LocalizationManager.T("cfg.status.removed_global_key", normalizedKey));
            return true;
        }

        ErrorOccurred(LocalizationManager.T("cfg.error.global_key_not_found"), "RemoveBindKey");
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
                StatusChanged(LocalizationManager.T("cfg.status.added_team_key", normalizedKey));
                return true;
            }

            ErrorOccurred(LocalizationManager.T("cfg.error.duplicate_team_key"), "AddTeamBindKey");
            return false;
        }

        ErrorOccurred(LocalizationManager.T("cfg.error.invalid_key"), "AddTeamBindKey");
        return false;
    }

    public bool RemoveTeamBindKey(string key)
    {
        if (TeamBindKeys.Count <= 1)
        {
            ErrorOccurred(LocalizationManager.T("cfg.error.keep_one_team_key"), "RemoveTeamBindKey");
            return false;
        }

        var normalizedKey = key.ToLower();
        if (TeamBindKeys.Contains(normalizedKey))
        {
            var newTeamBindKeys = TeamBindKeys.Where(k => k != normalizedKey).ToList();
            _configManager.UpdateTeamBindKeys(newTeamBindKeys);
            StatusChanged(LocalizationManager.T("cfg.status.removed_team_key", normalizedKey));
            return true;
        }

        ErrorOccurred(LocalizationManager.T("cfg.error.team_key_not_found"), "RemoveTeamBindKey");
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
            ErrorOccurred(LocalizationManager.T("cfg.error.empty_messages"), "GenerateConfigFiles");
            return false;
        }

        if (string.IsNullOrEmpty(CS2Path) || !Directory.Exists(CS2Path))
        {
            ErrorOccurred(LocalizationManager.T("cfg.error.invalid_cs2_path"), "GenerateConfigFiles");
            return false;
        }

        if (string.IsNullOrEmpty(CfgPath) || !Directory.Exists(CfgPath))
        {
            UpdateCfgPath();
            if (!Directory.Exists(CfgPath))
            {
                ErrorOccurred(LocalizationManager.T("cfg.error.invalid_cfg_dir"), "GenerateConfigFiles");
                return false;
            }
        }

        try
        {
            DeleteGeneratedMessageCfgFiles();
            var shuffledMessages = messages.OrderBy(_ => _random.Next()).ToList();
            var actualTotalFiles = shuffledMessages.Count;
            
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
            
            if (actualTotalFiles > 0)
            {
                GenerateSelectorFiles(actualTotalFiles);
                EventBus.TriggerEvent(new CfgFilesChangedEvent());
                StatusChanged(LocalizationManager.T("cfg.status.generated", actualTotalFiles * 2));
                return true;
            }

            ErrorOccurred(LocalizationManager.T("cfg.error.empty_messages"), "GenerateConfigFiles");
            return false;
        }
        catch (Exception ex)
        {
            ErrorOccurred(LocalizationManager.T("cfg.error.generate_failed", ex.Message), "GenerateConfigFiles");
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
        }
    }

    private void DeleteGeneratedMessageCfgFiles()
    {
        foreach (var file in Directory.GetFiles(CfgPath, "imlag_say_global_*.cfg")
                     .Where(file => !file.EndsWith("_selector.cfg", StringComparison.OrdinalIgnoreCase)))
        {
            File.Delete(file);
        }

        foreach (var file in Directory.GetFiles(CfgPath, "imlag_say_team_*.cfg")
                     .Where(file => !file.EndsWith("_selector.cfg", StringComparison.OrdinalIgnoreCase)))
        {
            File.Delete(file);
        }
    }

    public bool UpdateAutoexecFile()
    {
        if (string.IsNullOrEmpty(CfgPath) || !Directory.Exists(CfgPath))
        {
            ErrorOccurred(LocalizationManager.T("cfg.error.invalid_cfg_path"), "UpdateAutoexecFile");
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
                StatusChanged(LocalizationManager.T("cfg.status.autoexec_backup"));
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
            StatusChanged(LocalizationManager.T("cfg.status.autoexec_updated"));
            return true;
        }
        catch (Exception ex)
        {
            ErrorOccurred(LocalizationManager.T("cfg.error.update_autoexec_failed", ex.Message), "UpdateAutoexecFile");
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
        lines.Add("exec imlag_say_global_selector");
        lines.Add("exec imlag_say_team_selector");
        
        foreach (var key in BindKeys)
        {
            lines.Add($"bind \"{key}\" \"imlag_do_global_say\"");
            lines.Add($"echo \"ImLag: '{key}' bound to global chat.\"");
        }
        
        foreach (var key in TeamBindKeys)
        {
            lines.Add($"bind \"{key}\" \"imlag_do_team_say\"");
            lines.Add($"echo \"ImLag: '{key}' bound to team chat.\"");
        }
        
        lines.Add(ImLagCommentEnd);
        lines.Add("");

        lines.RemoveAll(line => line.Trim().ToLower() == "host_writeconfig");
        lines.Add("host_writeconfig");
    }
    
    public int GetGeneratedCfgGroupCount()
    {
        if (string.IsNullOrEmpty(CfgPath) || !Directory.Exists(CfgPath))
        {
            return 0;
        }

        try
        {
            var globalFiles = Directory.GetFiles(CfgPath, "imlag_say_global_*.cfg")
                .Count(file => !file.EndsWith("_selector.cfg", StringComparison.OrdinalIgnoreCase));
            var teamFiles = Directory.GetFiles(CfgPath, "imlag_say_team_*.cfg")
                .Count(file => !file.EndsWith("_selector.cfg", StringComparison.OrdinalIgnoreCase));
            return Math.Min(globalFiles, teamFiles);
        }
        catch (Exception ex)
        {
            ErrorOccurred(LocalizationManager.T("cfg.error.count_cfg_failed", ex.Message), "GetGeneratedCfgGroupCount");
            return 0;
        }
    }

    public bool ApplyGeneratedCfg()
    {
        if (!GenerateConfigFiles())
        {
            return false;
        }

        if (!UpdateAutoexecFile())
        {
            return false;
        }

        StatusChanged(LocalizationManager.T("cfg.status.apply_done", GeneratedCfgGroups));
        return true;
    }

    public bool RestoreOriginalCfg()
    {
        if (string.IsNullOrEmpty(CfgPath) || !Directory.Exists(CfgPath))
        {
            ErrorOccurred(LocalizationManager.T("cfg.error.restore_invalid_path"), "RestoreOriginalCfg");
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
                EventBus.TriggerEvent(new CfgFilesChangedEvent());
                StatusChanged(LocalizationManager.T("cfg.status.restored"));
                return true;
            }

            StatusChanged(LocalizationManager.T("cfg.status.no_backup"));
            return false;
        }
        catch (Exception ex)
        {
            ErrorOccurred(LocalizationManager.T("cfg.error.restore_failed", ex.Message), "RestoreOriginalCfg");
            return false;
        }
    }
}
