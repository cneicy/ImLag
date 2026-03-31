using System;
using System.Linq;
using CommonSDK.Event;
using Godot;
using ImLag.GUI.Scripts.Core;

namespace ImLag.GUI.Scripts.UI.Body;

public class GsiStartRequestEvent : EventBase;
public class GsiStopRequestEvent : EventBase;

[EventBusSubscriber]
public partial class GeneralPanel : PanelContainer
{
    private Label? _languageLabel;
    private OptionButton? _languageSelect;
    private CheckButton? _autoStartGsiToggle;
    private Button? _gsiToggleButton;
    private CheckButton? _onlySelfDeathToggle;
    private Label? _playerNamesHint;

    private ConfigManager? _configManager;
    private bool _uiInitialized;
    private bool _gsiRunning;

    public override void _Ready()
    {
        base._Ready();
        InitializeUi();
        RefreshView();
    }

    private void InitializeUi()
    {
        if (_uiInitialized)
        {
            return;
        }

        GetNode<Label>("MarginContainer/Content/Title").Text = "\u901a\u7528\u8bbe\u7f6e";

        _languageLabel = GetNode<Label>("MarginContainer/Content/LanguageRow/LanguageLabel");
        _languageSelect = GetNode<OptionButton>("MarginContainer/Content/LanguageRow/LanguageSelect");
        _autoStartGsiToggle = GetNode<CheckButton>("MarginContainer/Content/AutoStartGsiToggle");
        _gsiToggleButton = GetNode<Button>("MarginContainer/Content/GsiToggleButton");
        _onlySelfDeathToggle = GetNode<CheckButton>("MarginContainer/Content/OnlySelfDeathToggle");
        _playerNamesHint = GetNode<Label>("MarginContainer/Content/PlayerNamesHint");

        _languageSelect.Clear();
        _languageSelect.AddItem(LocalizationManager.T("language.zh-CN"), 0);
        _languageSelect.AddItem(LocalizationManager.T("language.zh-TW"), 1);
        ApplyTexts();
        _autoStartGsiToggle.Toggled += OnAutoStartGsiToggled;
        _gsiToggleButton.Pressed += OnGsiTogglePressed;
        _onlySelfDeathToggle.Toggled += OnOnlySelfDeathToggled;
        _languageSelect.ItemSelected += OnLanguageSelected;
        _uiInitialized = true;
    }

    [EventSubscribe]
    public void OnProgramInitEvent(ProgramInitEvent evt)
    {
        _configManager = evt.ConfigManager;
        RefreshView();
    }

    [EventSubscribe]
    public void OnConfigSavedEvent(ConfigSavedEvent evt)
    {
        RefreshView();
    }

    [EventSubscribe]
    public void OnLanguageChangedEvent(LanguageChangedEvent evt)
    {
        ApplyTexts();
        RefreshView();
    }

    [EventSubscribe]
    public void OnGSIInitEvent(GSIInitEvent evt)
    {
        _gsiRunning = false;
        RefreshView();
    }

    [EventSubscribe]
    public void OnGSIStartEvent(GSIStartEvent evt)
    {
        _gsiRunning = true;
        RefreshView();
    }

    [EventSubscribe]
    public void OnGSIStopEvent(GSIStopEvent evt)
    {
        _gsiRunning = false;
        RefreshView();
    }

    private void OnAutoStartGsiToggled(bool pressed)
    {
        _configManager?.UpdateAutoStartGsi(pressed);
    }

    private void OnGsiTogglePressed()
    {
        if (_gsiRunning)
        {
            EventBus.TriggerEvent(new GsiStopRequestEvent());
            return;
        }

        EventBus.TriggerEvent(new GsiStartRequestEvent());
    }

    private void OnOnlySelfDeathToggled(bool pressed)
    {
        _configManager?.UpdateOnlySelfDeath(pressed);
    }

    private void OnLanguageSelected(long selected)
    {
        var language = selected == 1 ? LocalizationManager.TraditionalChinese : LocalizationManager.DefaultLanguage;
        _configManager?.UpdateLanguage(language);
    }

    private void ApplyTexts()
    {
        GetNode<Label>("MarginContainer/Content/Title").Text = LocalizationManager.T("general.title");
        GetNode<Label>("MarginContainer/Content/Hint").Text = LocalizationManager.T("general.hint");

        if (_languageLabel != null)
        {
            _languageLabel.Text = LocalizationManager.T("language.label");
        }

        if (_languageSelect != null)
        {
            var selectedIndex = _languageSelect.Selected;
            _languageSelect.SetItemText(0, LocalizationManager.T("language.zh-CN"));
            _languageSelect.SetItemText(1, LocalizationManager.T("language.zh-TW"));
            if (selectedIndex >= 0)
            {
                _languageSelect.Select(selectedIndex);
            }
        }

        if (_autoStartGsiToggle != null)
        {
            _autoStartGsiToggle.Text = LocalizationManager.T("general.auto_start_gsi");
        }

        if (_onlySelfDeathToggle != null)
        {
            _onlySelfDeathToggle.Text = LocalizationManager.T("general.only_self_death");
        }
    }

    private void RefreshView()
    {
        if (!_uiInitialized || _configManager == null || _languageSelect == null || _autoStartGsiToggle == null || _gsiToggleButton == null ||
            _onlySelfDeathToggle == null || _playerNamesHint == null)
        {
            return;
        }

        _languageSelect.Select(_configManager.Config.Language == LocalizationManager.TraditionalChinese ? 1 : 0);
        _autoStartGsiToggle.SetPressedNoSignal(_configManager.Config.AutoStartGsi);
        _onlySelfDeathToggle.SetPressedNoSignal(_configManager.Config.OnlySelfDeath);
        _playerNamesHint.Text = LocalizationManager.T("general.player_list_summary", _configManager.Config.PlayerNames.Count);
        _gsiToggleButton.Text = LocalizationManager.T(_gsiRunning ? "general.stop_gsi" : "general.start_gsi");
    }
}
