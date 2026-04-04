using CommonSDK.Event;
using Godot;
using ImLag.GUI.Scripts.Core;

namespace ImLag.GUI.Scripts.UI.Foot;

[EventBusSubscriber]
public partial class StatusLabel : Label
{
    private string _messageKey = "status.init";
    private object[] _messageArgs = [];
    private string? _customText;

    public override void _Ready()
    {
        RefreshText();
    }

    [EventSubscribe]
    public void OnConfigLoadedEvent(ConfigLoadedEvent evt)
    {
        SetMessage("status.config_loaded");
    }

    [EventSubscribe]
    public void OnPlayerDeadEvent(PlayerDeadEvent evt)
    {
        SetMessage("status.player_dead", evt.PlayerName);
    }

    [EventSubscribe]
    public void OnChatMessageSentEvent(ChatMessageSentEvent evt)
    {
        SetMessage("status.message_sent", evt.Message);
    }

    [EventSubscribe]
    public void OnMessageAddedEvent(MessageAddedEvent evt)
    {
        SetMessage("status.message_added", evt.Message);
    }

    [EventSubscribe]
    public void OnMessageRemovedEvent(MessageRemovedEvent evt)
    {
        SetMessage("status.message_removed", evt.Message);
    }

    [EventSubscribe]
    public void OnCorpusImportedEvent(CorpusImportedEvent evt)
    {
        SetMessage("status.corpus_imported", evt.AddedCount, evt.SkippedCount);
    }

    [EventSubscribe]
    public void OnCorpusExportedEvent(CorpusExportedEvent evt)
    {
        SetMessage("status.corpus_exported", evt.ExportedCount, evt.Path);
    }

    [EventSubscribe]
    public void OnCorpusOperationFailedEvent(CorpusOperationFailedEvent evt)
    {
        SetCustomText(evt.Message);
    }

    [EventSubscribe]
    public void OnConfigSavedEvent(ConfigSavedEvent evt)
    {
        SetMessage("status.config_saved");
    }

    [EventSubscribe]
    public void OnCfgStatusChangedEvent(CfgStatusChangedEvent evt)
    {
        SetCustomText(evt.Message);
    }

    [EventSubscribe]
    public void OnCfgErrorOccurredEvent(CfgErrorOccurredEvent evt)
    {
        SetMessage("status.cfg_failed", evt.ErrorMessage);
    }

    [EventSubscribe]
    public void OnLanguageChangedEvent(LanguageChangedEvent evt)
    {
        RefreshText();
    }

    private void SetMessage(string messageKey, params object[] args)
    {
        _customText = null;
        _messageKey = messageKey;
        _messageArgs = args;
        RefreshText();
    }

    private void SetCustomText(string text)
    {
        _customText = text;
        Text = text;
    }

    private void RefreshText()
    {
        if (_customText != null)
        {
            Text = _customText;
            return;
        }

        Text = LocalizationManager.T(_messageKey, _messageArgs);
    }
}
