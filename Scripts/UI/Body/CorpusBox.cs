using System.Collections.Generic;
using CommonSDK.Event;
using CommonSDK.Utils;
using Godot;
using ImLag.GUI.Scripts.Core;

namespace ImLag.GUI.Scripts.UI.Body;

[EventBusSubscriber]
public partial class CorpusBox : VBoxContainer
{
    private PackedScene _scene = ResourceLoader.Load<PackedScene>("res://Resources/corpus_item.tscn");
    private ChatMessageManager _chatManager;

    public override void _EnterTree()
    {
        base._EnterTree();
        // 你先别管我写她干啥，硬问就是偷懒
        foreach (var item in this.FindObjectsOfType<CorpusItem>())
        {
            item.QueueFree();
        }
    }

    [EventSubscribe]
    public void OnProgramInitEvent(ProgramInitEvent evt)
    {
        _chatManager = evt.ChatManager;
        foreach (var corpus in _chatManager.GetAllMessages())
        {
            var item = _scene.Instantiate();
            AddChild(item);
            item.GetChild<Label>(0).Text = corpus;
        }
    }

    [EventSubscribe]
    public void RefreshCorpusItem(CorpusItemRefreshEvent evt)
    {
        foreach (var item in this.FindObjectsOfType<CorpusItem>())
        {
            item.QueueFree();
        }
        foreach (var corpus in _chatManager.GetAllMessages())
        {
            var item = _scene.Instantiate();
            AddChild(item);
            item.GetChild<Label>(0).Text = corpus;
        }
    }
}