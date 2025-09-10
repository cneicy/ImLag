using CommonSDK.Event;
using CommonSDK.Utils;
using Godot;
using ImLag.GUI.Scripts.Core;

namespace ImLag.GUI.Scripts.UI.Body;

public partial class CorpusAddBox : HBoxContainer
{
    private LineEdit _lineEdit;
    private Button _button;
    private ChatMessageManager _chatManager;
    public override void _Ready()
    {
        base._Ready();
        _lineEdit = GetNode<LineEdit>("LineEdit");
        _button = GetNode<Button>("Button");
        _chatManager = GetTree().Root.FindObjectOfType<Entry>().ChatManager;
        _button.Pressed += OnAddButtonPressed;
    }

    private void OnAddButtonPressed()
    {
        _chatManager.AddMessage(_lineEdit.Text);
        _lineEdit.Text = "";
        EventBus.TriggerEvent(new CorpusItemRefreshEvent());
    }
}