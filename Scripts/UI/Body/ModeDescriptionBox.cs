using CommonSDK.Event;
using CommonSDK.Utils;
using Godot;
using ImLag.GUI.Scripts.Core;
using ImLag.GUI.Scripts.UI.Head;

namespace ImLag.GUI.Scripts.UI.Body;

[EventBusSubscriber]
public partial class ModeDescriptionBox : VBoxContainer
{
    private Label _titleLabel;
    private Label _descriptionLabel;
    private bool _useCfgMode;

    public override void _EnterTree()
    {
        base._EnterTree();
        _titleLabel = (Label)this.Find("ModeTitle");
        _descriptionLabel = (Label)this.Find("ModeDescriptions");
    }
    
    [EventSubscribe]
    public void OnProgramInitEvent(ProgramInitEvent evt)
    {
        _useCfgMode = evt.ConfigManager.Config.UseCfgMode;
        RefreshText();
    }

    [EventSubscribe]
    public void OnModeChanged(ModeUpdateEvent evt)
    {
        _useCfgMode = evt.UseCfgMode;
        RefreshText();
    }

    [EventSubscribe]
    public void OnLanguageChangedEvent(LanguageChangedEvent evt)
    {
        RefreshText();
    }

    private void RefreshText()
    {
        _titleLabel.Text = LocalizationManager.T(_useCfgMode ? "mode.cfg" : "mode.chat");
        _descriptionLabel.Text = LocalizationManager.T(_useCfgMode ? "mode.cfg_description" : "mode.chat_description");
    }
}
