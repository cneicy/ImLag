using CommonSDK.Event;
using CommonSDK.Utils;
using Godot;
using ImLag.GUI.Scripts.Core;

namespace ImLag.GUI.Scripts.UI.Body;

[EventBusSubscriber]
public partial class CorpusBox : VBoxContainer
{
    private readonly PackedScene _scene = ResourceLoader.Load<PackedScene>("res://Resources/corpus_item.tscn");
    private ChatMessageManager? _chatManager;

    public override void _EnterTree()
    {
        ClearItems();
    }

    [EventSubscribe]
    public void OnProgramInitEvent(ProgramInitEvent evt)
    {
        _chatManager = evt.ChatManager;
        RefreshItems();
    }

    [EventSubscribe]
    public void RefreshCorpusItem(CorpusItemRefreshEvent evt)
    {
        RefreshItems();
    }

    [EventSubscribe]
    public void OnLanguageChangedEvent(LanguageChangedEvent evt)
    {
        RefreshItems();
    }

    private void RefreshItems()
    {
        if (_chatManager == null)
        {
            return;
        }

        ClearItems();
        var messages = _chatManager.GetAllMessages();
        if (messages.Count == 0)
        {
            AddChild(new Label
            {
                Text = LocalizationManager.T("corpus.empty"),
                AutowrapMode = TextServer.AutowrapMode.WordSmart,
                Modulate = new Color(0.7f, 0.7f, 0.7f)
            });
            return;
        }

        foreach (var corpus in messages)
        {
            var item = _scene.Instantiate();
            AddChild(item);
            item.GetChild<Label>(0).Text = corpus;
        }
    }

    private void ClearItems()
    {
        foreach (var item in GetChildren())
        {
            item.QueueFree();
        }
    }
}
