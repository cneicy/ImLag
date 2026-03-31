using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Encodings.Web;
using System.Text.Json;
using CommonSDK.Event;
using Godot;

// ReSharper disable InconsistentNaming

namespace ImLag.GUI.Scripts.Core;

public class ConfigLoadedEvent : EventBase
{
    
}

public class ConfigSavedEvent : EventBase
{
    
}

public class ConfigManager
{
    public AppConfig Config { get; private set; } = new();
    private const string ConfigFile = "Config.json";

    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        WriteIndented = true,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };


    public void LoadConfig()
    {
        try
        {
            if (File.Exists(ConfigFile))
            {
                var json = File.ReadAllText(ConfigFile);
                var loadedConfig = JsonSerializer.Deserialize<AppConfig>(json);
                if (loadedConfig != null)
                {
                    Config = loadedConfig;
                    ValidateConfig();
                    EventBus.TriggerEvent(new ConfigLoadedEvent());
                    return;
                }
            }
        }
        catch (Exception)
        {
            // fuck it
        }

        LoadDefaultConfig();
        SaveConfig();
    }

    private void ValidateConfig()
    {
        Config.CS2Path ??= string.Empty;
        Config.ChatKey ??= "y";
            
        if (Config.BindKeys == null || Config.BindKeys.Count == 0)
            Config.BindKeys = ["k"];
                
        if (Config.TeamBindKeys == null || Config.TeamBindKeys.Count == 0)
            Config.TeamBindKeys = ["l"];
                
        if (Config.PlayerNames == null)
            Config.PlayerNames = [];
                
        if (Config.KeyDelay is < 30 or > 1000)
            Config.KeyDelay = 100;

        Config.Language = LocalizationManager.NormalizeLanguage(Config.Language);
        LocalizationManager.SetLanguage(Config.Language, notify: false);
    }

    private void LoadDefaultConfig()
    {
        Config = new AppConfig
        {
            PlayerNames = [],
            OnlySelfDeath = true,
            BindKeys = ["k"],
            TeamBindKeys = ["l"],
            CS2Path = string.Empty,
            UseCfgMode = true,
            ChatKey = "y",
            SkipWindowCheck = false,
            ForceMode = false,
            KeyDelay = 100,
            Language = "zh-CN",
            AutoStartGsi = true,
            PreferTeamChat = false
        };
        EventBus.TriggerEvent(new ConfigLoadedEvent());
    }

    public void SaveConfig()
    {
        try
        {
            var json = JsonSerializer.Serialize(Config, _jsonOptions);
            File.WriteAllText(ConfigFile, json);
            EventBus.TriggerEvent(new  ConfigSavedEvent());
        }
        catch (Exception)
        {
            // fuck it
        }
    }

    public void UpdatePlayerNames(List<string> playerNames)
    {
        Config.PlayerNames = playerNames
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .Select(name => name.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        SaveConfig();
    }

    public void UpdateOnlySelfDeath(bool onlySelf)
    {
        Config.OnlySelfDeath = onlySelf;
        SaveConfig();
    }

    public void UpdateCS2Path(string path)
    {
        Config.CS2Path = path?.Trim() ?? string.Empty;
        SaveConfig();
    }

    public void UpdateBindKeys(List<string> bindKeys)
    {
        if (bindKeys is not { Count: > 0 }) return;
        Config.BindKeys = new List<string>(bindKeys);
        SaveConfig();
    }

    public void UpdateTeamBindKeys(List<string> teamBindKeys)
    {
        if (teamBindKeys is not { Count: > 0 }) return;
        Config.TeamBindKeys = new List<string>(teamBindKeys);
        SaveConfig();
    }

    public void UpdateUseCfgMode(bool useCfgMode)
    {
        Config.UseCfgMode = useCfgMode;
        SaveConfig();
    }

    public void UpdateChatKey(string chatKey)
    {
        Config.ChatKey = chatKey?.Trim() ?? "y";
        SaveConfig();
    }

    public void UpdateSkipWindowCheck(bool skip)
    {
        Config.SkipWindowCheck = skip;
        SaveConfig();
    }

    public void UpdateForceMode(bool force)
    {
        Config.ForceMode = force;
        SaveConfig();
    }

    public void UpdateKeyDelay(int delay)
    {
        if (delay is < 30 or > 1000) return;
        Config.KeyDelay = delay;
        SaveConfig();
    }

    public void UpdateLanguage(string language)
    {
        Config.Language = LocalizationManager.NormalizeLanguage(language);
        LocalizationManager.SetLanguage(Config.Language);
        SaveConfig();
    }

    public void UpdateAutoStartGsi(bool autoStart)
    {
        Config.AutoStartGsi = autoStart;
        SaveConfig();
    }

    public void UpdatePreferTeamChat(bool preferTeam)
    {
        Config.PreferTeamChat = preferTeam;
        SaveConfig();
    }
}

public class AppConfig
{
    public List<string> PlayerNames { get; set; } = [];
    public bool OnlySelfDeath { get; set; } = true;
    public List<string> BindKeys { get; set; } = ["k"];
    public List<string> TeamBindKeys { get; set; } = ["l"];
    public string CS2Path { get; set; } = string.Empty;
    public bool UseCfgMode { get; set; } = true;
    public string ChatKey { get; set; } = "y";
    public bool SkipWindowCheck { get; set; }
    public bool ForceMode { get; set; }
    public int KeyDelay { get; set; } = 100;
    public string Language { get; set; } = "zh-CN";
    public bool AutoStartGsi { get; set; } = true;
    public bool PreferTeamChat { get; set; }
}
