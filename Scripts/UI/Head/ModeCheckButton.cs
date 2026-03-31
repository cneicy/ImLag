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
        ApplyText();
        ButtonPressed = !UseCfgMode;
    }

    [EventSubscribe]
    public void OnLanguageChangedEvent(LanguageChangedEvent evt)
    {
        ApplyText();
    }
    
    public override void _Pressed()
    {
        base._Pressed();
        _configManager.UpdateUseCfgMode(!ButtonPressed);
        UseCfgMode = !ButtonPressed;
        ApplyText();
        EventBus.TriggerEvent(new ModeUpdateEvent
        {
            UseCfgMode = UseCfgMode
        });
    }

    private void ApplyText()
    {
        Text = LocalizationManager.T(UseCfgMode ? "mode.cfg" : "mode.chat");
    }
}
