using CommonSDK.Event;
using CommonSDK.Utils;
using Godot;
using ImLag.GUI.Scripts.UI.Head;

namespace ImLag.GUI.Scripts.UI.Body;

[EventBusSubscriber]
public partial class ModeDescriptionBox : VBoxContainer
{
    private Label _titleLabel;
    private Label _descriptionLabel;
    public override void _EnterTree()
    {
        base._EnterTree();
        _titleLabel = (Label)this.Find("ModeTitle");
        _descriptionLabel = (Label)this.Find("ModeDescriptions");
        
    }
    
    [EventSubscribe]
    public void OnProgramInitEvent(ProgramInitEvent evt)
    {
        if (evt.ConfigManager.Config.UseCfgMode)
        {
            _titleLabel.Text = "CFG模式";
            _descriptionLabel.Text = "生成游戏配置文件，检测到死亡后自动按下绑定的快捷键发送消息";
        }
        else
        {
            _titleLabel.Text = "聊天模式";
            _descriptionLabel.Text = "检测到死亡后自动模拟按键发送消息";
        }
    }

    [EventSubscribe]
    public void OnModeChanged(ModeUpdateEvent evt)
    {
        if (evt.UseCfgMode)
        {
            _titleLabel.Text = "CFG模式";
            _descriptionLabel.Text = "生成游戏配置文件，检测到死亡后自动按下绑定的快捷键发送消息";
        }
        else
        {
            _titleLabel.Text = "聊天模式";
            _descriptionLabel.Text = "检测到死亡后自动模拟按键发送消息";
        }
    }
}