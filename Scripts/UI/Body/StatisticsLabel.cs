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
        _chatManager =evt.ChatManager;
        _cfgManager = evt.CfgManager;
        Text = $"消息数: {_chatManager.MessageCount}\nCFG文件数: {_cfgManager.TotalCfgFiles}";
    }
    [EventSubscribe]
    public void OnCorpusItemRefreshEvent(CorpusItemRefreshEvent evt)
    {
        Text = $"消息数: {_chatManager.MessageCount}\nCFG文件数: {_cfgManager.TotalCfgFiles}";
    }
}