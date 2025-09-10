using CommonSDK.Event;
using Godot;
using ImLag.GUI.Scripts.Core;

namespace ImLag.GUI.Scripts.UI.Head;

public class ModeUpdateEvent : EventBase
{
    public bool UseCfgMode { get; set; }
}

[EventBusSubscriber]
public partial class ModeCheckButton : CheckButton
{
    public bool UseCfgMode;
    private ConfigManager _configManager;

    [EventSubscribe]
    public void OnProgramInitEvent(ProgramInitEvent evt)
    {
        _configManager = evt.ConfigManager;
        UseCfgMode = _configManager.Config.UseCfgMode;
        Text = UseCfgMode ? "CFG模式" : "聊天模式";
        ButtonPressed = !UseCfgMode;
    }
    
    public override void _Pressed()
    {
        base._Pressed();
        Text = ButtonPressed ? "聊天模式" : "CFG模式";
        _configManager.UpdateUseCfgMode(!ButtonPressed);
        UseCfgMode = !ButtonPressed;
        EventBus.TriggerEvent(new ModeUpdateEvent
        {
            UseCfgMode = UseCfgMode
        });
    }
}