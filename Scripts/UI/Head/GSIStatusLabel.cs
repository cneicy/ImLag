using CommonSDK.Event;
using Godot;
using ImLag.GUI.Scripts.Core;

// ReSharper disable InconsistentNaming

namespace ImLag.GUI.Scripts.UI.Head;
[EventBusSubscriber]
public partial class GSIStatusLabel : Label
{
    private string _statusKey = "gsi.initializing";

    public override void _Ready()
    {
        base._Ready();
        RefreshText();
    }

    [EventSubscribe]
    public void OnGSIInitEvent(GSIInitEvent evt)
    {
        _statusKey = "gsi.initializing";
        RefreshText();
    }

    [EventSubscribe]
    public void OnGSIStartEvent(GSIStartEvent evt)
    {
        _statusKey = "gsi.started";
        RefreshText();
    }

    [EventSubscribe]
    public void OnGSIStopEvent(GSIStopEvent evt)
    {
        _statusKey = "gsi.stopped";
        RefreshText();
    }

    [EventSubscribe]
    public void OnLanguageChangedEvent(LanguageChangedEvent evt)
    {
        RefreshText();
    }

    private void RefreshText()
    {
        Text = LocalizationManager.T(_statusKey);
    }
}
