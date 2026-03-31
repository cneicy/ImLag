using CommonSDK.Event;
using Godot;
using ImLag.GUI.Scripts.Core;

namespace ImLag.GUI.Scripts.UI.Body;

[EventBusSubscriber]
public partial class StatisticsLabel : Label
{
    private ChatMessageManager _chatManager;
    private CfgManager _cfgManager;

    [EventSubscribe]
    public void OnProgramInitEvent(ProgramInitEvent evt)
    {
        _chatManager = evt.ChatManager;
        _cfgManager = evt.CfgManager;
        RefreshText();
    }

    [EventSubscribe]
    public void OnCorpusItemRefreshEvent(CorpusItemRefreshEvent evt)
    {
        RefreshText();
    }

    [EventSubscribe]
    public void OnCfgFilesChangedEvent(CfgFilesChangedEvent evt)
    {
        RefreshText();
    }

    [EventSubscribe]
    public void OnCfgStatusChangedEvent(CfgStatusChangedEvent evt)
    {
        RefreshText();
    }

    [EventSubscribe]
    public void OnConfigSavedEvent(ConfigSavedEvent evt)
    {
        RefreshText();
    }

    [EventSubscribe]
    public void OnLanguageChangedEvent(LanguageChangedEvent evt)
    {
        RefreshText();
    }

    private void RefreshText()
    {
        if (_chatManager == null || _cfgManager == null)
        {
            return;
        }

        Text = LocalizationManager.T("stats.summary", _chatManager.MessageCount, _cfgManager.GeneratedCfgGroups);
    }
}
