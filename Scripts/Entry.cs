using System;
using System.Linq;
using System.Threading.Tasks;
using CommonSDK.Event;
using CounterStrike2GSI;
using CounterStrike2GSI.EventMessages;
using Godot;
using ImLag.GUI.Scripts.Core;
using ImLag.GUI.Scripts.UI.Body;
// ReSharper disable InconsistentNaming

namespace ImLag.GUI.Scripts;

public class GSIInitEvent : EventBase;
public class GSIStartEvent : EventBase;
public class GSIStopEvent : EventBase;

public class ProgramInitEvent : EventBase
{
    public ChatMessageManager ChatManager;
    public ConfigManager ConfigManager;
    public CfgManager CfgManager;
    public ChatMessageSender MessageSender;
}

public class PlayerDeadEvent : EventBase
{
    public string PlayerName { get; set; }
}

[EventBusSubscriber]
public partial class Entry : Node
{
    public ChatMessageManager ChatManager;
    public ConfigManager ConfigManager;
    public CfgManager CfgManager;
    public ChatMessageSender MessageSender;
    public GameStateListener Gsl;

    public override void _Ready()
    {
        base._Ready();

        ConfigManager = new ConfigManager();
        ConfigManager.LoadConfig();

        ChatManager = new ChatMessageManager();
        ChatManager.LoadMessages();

        CfgManager = new CfgManager(ChatManager, ConfigManager);

        MessageSender = new ChatMessageSender(ConfigManager);
        EventBus.TriggerEvent(new ProgramInitEvent
        {
            ConfigManager = ConfigManager,
            CfgManager = CfgManager,
            MessageSender = MessageSender,
            ChatManager = ChatManager
        });
        InitializeGSI();
        if (!ConfigManager.Config.AutoStartGsi) return;
        StartGSI();
    }

    public override void _ExitTree()
    {
        base._ExitTree();
        Gsl.Stop();
    }

    private void InitializeGSI()
    {
        try
        {
            Gsl = new GameStateListener(4000);
            Gsl.Stop();
            Gsl.PlayerDied += OnPlayerDied;
            GD.Print($"GSI 初始化成功，正在监听端口 {4000}");
            EventBus.TriggerEvent(new GSIInitEvent());
        }
        catch (Exception portEx)
        {
            GD.Print($"监听 {4000} 失败: {portEx.Message}");
            Gsl = null;
        }
    }

    private async void OnPlayerDied(PlayerDied gameEvent)
    {
        EventBus.TriggerEvent(new PlayerDeadEvent
        {
            PlayerName = gameEvent.Player.Name
        });
        if (ConfigManager.Config.OnlySelfDeath &&
            !ConfigManager.Config.PlayerNames.Any(name =>
                string.Equals(name?.Trim(), gameEvent.Player.Name?.Trim(), StringComparison.OrdinalIgnoreCase)))
        {
            return;
        }

        var randomMessage = ChatManager.GetRandomMessage();
        if (string.IsNullOrEmpty(randomMessage))
        {
            return;
        }

        if (ConfigManager.Config.UseCfgMode)
        {
            try
            {
                if (!CSWindowChecker.IsCS2Active())
                {
                    return;
                }

                KeySimulator.ReleaseAllKeys();

                await Task.Delay(100);

                var useTeamChat = ConfigManager.Config.PreferTeamChat;
                var randomKey = useTeamChat ? CfgManager.GetRandomTeamBindKey() : CfgManager.GetRandomBindKey();

                KeySimulator.SimulateKeyPress(randomKey);

                var chatType = useTeamChat ? "team" : "global";
            }
            catch (Exception)
            {
                // ignored
            }
        }
        else
        {
            try
            {
                await MessageSender.SendMessageAsync(randomMessage);
            }
            catch (Exception)
            {
                //ignored
            }
        }
    }

    private void StartGSI()
    {
        try
        {
            GD.Print("正在启动GSI...");

            if (Gsl == null)
            {
                InitializeGSI();
            }

            if (Gsl == null || Gsl.Running) return;
            if (Gsl.Start())
            {
                EventBus.TriggerEvent(new GSIStartEvent());
                GD.Print("GSI启动成功");
            }
            else
            {
                GD.Print("GSI未能启动");
            }
        }
        catch (Exception ex)
        {
            GD.Print($"GSI启动失败: {ex.Message}");
        }
    }

    private void StopGSI()
    {
        try
        {
            GD.Print("正在停止GSI...");

            if (Gsl is { Running: true })
            {
                Gsl.Stop();
                GD.Print("GSI停止成功");
                EventBus.TriggerEvent(new GSIStopEvent());
            }
            else
            {
                GD.Print("GSI未运行或对象为空");
            }
        }
        catch (Exception ex)
        {
            GD.Print($"停止GSI失败: {ex.Message}");
        }
    }

    [EventSubscribe]
    public void OnGsiStartRequestEvent(GsiStartRequestEvent evt)
    {
        StartGSI();
    }

    [EventSubscribe]
    public void OnGsiStopRequestEvent(GsiStopRequestEvent evt)
    {
        StopGSI();
    }
}
