using CommonSDK.Event;
using CommonSDK.Utils;
using Godot;
using ImLag.GUI.Scripts.Core;

namespace ImLag.GUI.Scripts.UI.Body;

public class CorpusItemRefreshEvent : EventBase;

public partial class CorpusItem : HBoxContainer
{
    public Label CorpusLabel;
    private Button _button;
    private ChatMessageManager _chatManager;
    public override void _Ready()
    {
        base._Ready();
        _button = GetNode<Button>("Button");
        CorpusLabel = GetNode<Label>("Label");
        _chatManager = GetTree().Root.FindObjectOfType<Entry>().ChatManager;
        _button.Pressed += OnDeleteBtnPressed;
    }

    public void OnDeleteBtnPressed()
    {
        _chatManager.RemoveMessage(CorpusLabel.Text);
        EventBus.TriggerEvent(new CorpusItemRefreshEvent());
    }
}