using CommonSDK.Event;
using CommonSDK.Utils;
using Godot;
using ImLag.GUI.Scripts.Core;

namespace ImLag.GUI.Scripts.UI.Body;

[EventBusSubscriber]
public partial class PlayerListBox : VBoxContainer
{
    private readonly PackedScene _scene = ResourceLoader.Load<PackedScene>("res://Resources/player_list_item.tscn");
    private ConfigManager? _configManager;

    public override void _EnterTree()
    {
        base._EnterTree();
        ClearItems();
    }

    [EventSubscribe]
    public void OnProgramInitEvent(ProgramInitEvent evt)
    {
        _configManager = evt.ConfigManager;
        RefreshItems();
    }

    [EventSubscribe]
    public void OnConfigSavedEvent(ConfigSavedEvent evt)
    {
        RefreshItems();
    }

    [EventSubscribe]
    public void OnPlayerListRefreshEvent(PlayerListRefreshEvent evt)
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
        if (_configManager == null)
        {
            return;
        }

        ClearItems();
        if (_configManager.Config.PlayerNames.Count == 0)
        {
            AddChild(new Label
            {
                Text = LocalizationManager.T("general.player_list_empty"),
                AutowrapMode = TextServer.AutowrapMode.WordSmart,
                Modulate = new Color(0.7f, 0.7f, 0.7f)
            });
            return;
        }

        foreach (var playerName in _configManager.Config.PlayerNames)
        {
            var item = _scene.Instantiate<PlayerListItem>();
            item.SetPlayerName(playerName);
            AddChild(item);
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
