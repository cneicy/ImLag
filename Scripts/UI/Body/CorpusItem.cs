using CommonSDK.Event;
using CommonSDK.Utils;
using Godot;
using ImLag.GUI.Scripts.Core;

namespace ImLag.GUI.Scripts.UI.Body;

public class CorpusItemRefreshEvent : EventBase;

[EventBusSubscriber]
public partial class CorpusItem : HBoxContainer
{
    public Label CorpusLabel;
    private Button _button;
    private ChatMessageManager _chatManager;
    public override void _Ready()
    {
        _button = GetNode<Button>("Button");
        CorpusLabel = GetNode<Label>("Label");
        _chatManager = GetTree().Root.FindObjectOfType<Entry>().ChatManager;
        ApplyTexts();
        _button.Pressed += OnDeleteBtnPressed;
    }

    [EventSubscribe]
    public void OnLanguageChangedEvent(LanguageChangedEvent evt)
    {
        ApplyTexts();
    }

    private void ApplyTexts()
    {
        _button.Text = LocalizationManager.T("common.delete");
    }

    public void OnDeleteBtnPressed()
    {
        _chatManager.RemoveMessage(CorpusLabel.Text);
        EventBus.TriggerEvent(new CorpusItemRefreshEvent());
    }
}
