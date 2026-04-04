using System.Linq;
using CommonSDK.Event;
using CommonSDK.Utils;
using Godot;
using ImLag.GUI.Scripts.Core;

namespace ImLag.GUI.Scripts.UI.Body;

[EventBusSubscriber]
public partial class PlayerListAddBox : HBoxContainer
{
    private LineEdit _lineEdit = null!;
    private Button _button = null!;
    private ConfigManager _configManager = null!;

    public override void _Ready()
    {
        _lineEdit = GetNode<LineEdit>("LineEdit");
        _button = GetNode<Button>("Button");
        _configManager = GetTree().Root.FindObjectOfType<Entry>().ConfigManager;
        ApplyTexts();
        _button.Pressed += OnAddButtonPressed;
        _lineEdit.TextSubmitted += OnTextSubmitted;
    }

    [EventSubscribe]
    public void OnLanguageChangedEvent(LanguageChangedEvent evt)
    {
        ApplyTexts();
    }

    private void ApplyTexts()
    {
        _lineEdit.PlaceholderText = LocalizationManager.T("general.player_add_placeholder");
        _button.Text = LocalizationManager.T("common.add");
    }

    private void OnTextSubmitted(string _)
    {
        TryAddPlayer();
    }

    private void OnAddButtonPressed()
    {
        TryAddPlayer();
    }

    private void TryAddPlayer()
    {
        var playerName = _lineEdit.Text.Trim();
        if (string.IsNullOrWhiteSpace(playerName))
        {
            return;
        }

        var updatedNames = _configManager.Config.PlayerNames.ToList();
        updatedNames.Add(playerName);
        _configManager.UpdatePlayerNames(updatedNames);
        _lineEdit.Text = string.Empty;
        EventBus.TriggerEvent(new PlayerListRefreshEvent());
    }
}
