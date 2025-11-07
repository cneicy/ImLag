using CommonSDK.Event;
using Godot;
using ImLag.GUI.Scripts.Core;

namespace ImLag.GUI.Scripts.UI.Foot;

[EventBusSubscriber]
public partial class StatusLabel : Label
{
    [EventSubscribe]
    public void OnConfigLoadedEvent(ConfigLoadedEvent evt)
    {
        Text = "设置已加载";
    }
    [EventSubscribe]
    public void OnPlayerDeadEvent(PlayerDeadEvent evt)
    {
        Text = $"检查到玩家死亡 {evt.PlayerName}";
    }

    [EventSubscribe]
    public void OnChatMessageSentEvent(ChatMessageSentEvent evt)
    {
        Text = $"已发送消息 {evt.Message}";
    }

    [EventSubscribe]
    public void OnMessageAddedEvent(MessageAddedEvent evt)
    {
        Text = $"已添加语料 {evt.Message}";
    }

    [EventSubscribe]
    public void OnMessageRemovedEvent(MessageRemovedEvent evt)
    {
        Text = $"已删除语料 {evt.Message}";
    }

    [EventSubscribe]
    public void OnConfigSavedEvent(ConfigSavedEvent evt)
    {
        Text = "设置已保存";
    }
}