using CommonSDK.Event;
using Godot;

// ReSharper disable InconsistentNaming

namespace ImLag.GUI.Scripts.UI.Head;
[EventBusSubscriber]
public partial class GSIStatusLabel : Label
{
    [EventSubscribe]
    public void OnGSIInitEvent(GSIInitEvent evt)
    {
        Text = "GSI初始化中";
    }
    [EventSubscribe]
    public void OnGSIStartEvent(GSIStartEvent evt)
    {
        Text = "GSI已启动";
    }
    [EventSubscribe]
    public void OnGSIStopEvent(GSIStopEvent evt)
    {
        Text = "GSI已停止";
    }
}