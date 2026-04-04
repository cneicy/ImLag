using System.Linq;
using CommonSDK.Event;
using CommonSDK.Utils;
using Godot;
using ImLag.GUI.Scripts.Core;

namespace ImLag.GUI.Scripts.UI.Body;

public class PlayerListRefreshEvent : EventBase;

[EventBusSubscriber]
public partial class PlayerListItem : HBoxContainer
{
    private string _playerName = string.Empty;
    public Label PlayerLabel = null!;
    private Button _button = null!;
    private ConfigManager _configManager = null!;

    public override void _Ready()
    {
        _button = GetNode<Button>("Button");
        PlayerLabel = GetNode<Label>("Label");
        _configManager = GetTree().Root.FindObjectOfType<Entry>().ConfigManager;
        PlayerLabel.Text = _playerName;
        ApplyTexts();
        _button.Pressed += OnDeleteBtnPressed;
    }

    [EventSubscribe]
    public void OnLanguageChangedEvent(LanguageChangedEvent evt)
    {
        ApplyTexts();
    }

    public void SetPlayerName(string playerName)
    {
        _playerName = playerName;
        if (PlayerLabel != null)
        {
            PlayerLabel.Text = playerName;
        }
    }

    private void ApplyTexts()
    {
        _button.Text = LocalizationManager.T("common.delete");
    }

    private void OnDeleteBtnPressed()
    {
        var updatedNames = _configManager.Config.PlayerNames
            .Where(name => !string.Equals(name, PlayerLabel.Text, System.StringComparison.OrdinalIgnoreCase))
            .ToList();
        _configManager.UpdatePlayerNames(updatedNames);
        EventBus.TriggerEvent(new PlayerListRefreshEvent());
    }
}
