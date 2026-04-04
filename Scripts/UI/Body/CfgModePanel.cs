using System.Linq;
using CommonSDK.Event;
using Godot;
using ImLag.GUI.Scripts.Core;
using ImLag.GUI.Scripts.UI.Head;

namespace ImLag.GUI.Scripts.UI.Body;

[EventBusSubscriber]
public partial class CfgModePanel : PanelContainer
{
    private LineEdit? _cs2PathInput;
    private Button? _applyPathButton;
    private Button? _detectPathButton;
    private Button? _generateButton;
    private Button? _restoreButton;
    private Label? _globalKeysLabel;
    private LineEdit? _globalKeyInput;
    private Button? _addGlobalKeyButton;
    private Button? _removeGlobalKeyButton;
    private Label? _teamKeysLabel;
    private LineEdit? _teamKeyInput;
    private Button? _addTeamKeyButton;
    private Button? _removeTeamKeyButton;
    private CheckButton? _preferTeamChatToggle;
    private bool _uiInitialized;

    private ConfigManager? _configManager;
    private CfgManager? _cfgManager;

    public override void _Ready()
    {
        InitializeUi();
        RefreshView();
    }

    private void InitializeUi()
    {
        if (_uiInitialized)
        {
            return;
        }

        _cs2PathInput = GetNode<LineEdit>("MarginContainer/Content/Cs2PathRow/PathInput");
        _applyPathButton = GetNode<Button>("MarginContainer/Content/Cs2PathRow/PathButtonRow/ApplyPathButton");
        _detectPathButton = GetNode<Button>("MarginContainer/Content/Cs2PathRow/PathButtonRow/DetectPathButton");
        _generateButton = GetNode<Button>("MarginContainer/Content/ActionRow/GenerateButton");
        _restoreButton = GetNode<Button>("MarginContainer/Content/ActionRow/RestoreButton");
        _globalKeysLabel = GetNode<Label>("MarginContainer/Content/GlobalKeysSection/CurrentGlobalKeys");
        _globalKeyInput = GetNode<LineEdit>("MarginContainer/Content/GlobalKeysSection/GlobalKeyRow/GlobalKeyInput");
        _addGlobalKeyButton = GetNode<Button>("MarginContainer/Content/GlobalKeysSection/GlobalKeyRow/AddGlobalKeyButton");
        _removeGlobalKeyButton = GetNode<Button>("MarginContainer/Content/GlobalKeysSection/GlobalKeyRow/RemoveGlobalKeyButton");
        _teamKeysLabel = GetNode<Label>("MarginContainer/Content/TeamKeysSection/CurrentTeamKeys");
        _teamKeyInput = GetNode<LineEdit>("MarginContainer/Content/TeamKeysSection/TeamKeyRow/TeamKeyInput");
        _addTeamKeyButton = GetNode<Button>("MarginContainer/Content/TeamKeysSection/TeamKeyRow/AddTeamKeyButton");
        _removeTeamKeyButton = GetNode<Button>("MarginContainer/Content/TeamKeysSection/TeamKeyRow/RemoveTeamKeyButton");
        _preferTeamChatToggle = GetNode<CheckButton>("MarginContainer/Content/PreferTeamChatToggle");

        _globalKeyInput.MaxLength = 1;
        _teamKeyInput.MaxLength = 1;
        ApplyTexts();

        _applyPathButton.Pressed += OnApplyPathPressed;
        _detectPathButton.Pressed += OnDetectPathPressed;
        _generateButton.Pressed += OnGeneratePressed;
        _restoreButton.Pressed += OnRestorePressed;
        _addGlobalKeyButton.Pressed += OnAddGlobalKeyPressed;
        _removeGlobalKeyButton.Pressed += OnRemoveGlobalKeyPressed;
        _addTeamKeyButton.Pressed += OnAddTeamKeyPressed;
        _removeTeamKeyButton.Pressed += OnRemoveTeamKeyPressed;
        _preferTeamChatToggle.Toggled += OnPreferTeamChatToggled;
        _uiInitialized = true;
    }

    [EventSubscribe]
    public void OnProgramInitEvent(ProgramInitEvent evt)
    {
        _configManager = evt.ConfigManager;
        _cfgManager = evt.CfgManager;
        RefreshView();
    }

    [EventSubscribe]
    public void OnConfigSavedEvent(ConfigSavedEvent evt)
    {
        RefreshView();
    }

    [EventSubscribe]
    public void OnCfgStatusChangedEvent(CfgStatusChangedEvent evt)
    {
        RefreshView();
    }

    [EventSubscribe]
    public void OnLanguageChangedEvent(LanguageChangedEvent evt)
    {
        ApplyTexts();
        RefreshView();
    }

    private void OnApplyPathPressed()
    {
        if (_cfgManager == null || _cs2PathInput == null)
        {
            return;
        }

        _cfgManager.SetCS2Path(_cs2PathInput.Text);
        RefreshView();
    }

    private void OnDetectPathPressed()
    {
        _cfgManager?.FindCS2Path();
        RefreshView();
    }

    private void OnGeneratePressed()
    {
        _cfgManager?.ApplyGeneratedCfg();
        RefreshView();
    }

    private void OnRestorePressed()
    {
        _cfgManager?.RestoreOriginalCfg();
        RefreshView();
    }

    private void OnAddGlobalKeyPressed()
    {
        if (_cfgManager == null || _globalKeyInput == null)
        {
            return;
        }

        if (_cfgManager.AddBindKey(_globalKeyInput.Text))
        {
            _globalKeyInput.Text = string.Empty;
        }

        RefreshView();
    }

    private void OnRemoveGlobalKeyPressed()
    {
        if (_cfgManager == null || _globalKeyInput == null)
        {
            return;
        }

        if (_cfgManager.RemoveBindKey(_globalKeyInput.Text))
        {
            _globalKeyInput.Text = string.Empty;
        }

        RefreshView();
    }

    private void OnAddTeamKeyPressed()
    {
        if (_cfgManager == null || _teamKeyInput == null)
        {
            return;
        }

        if (_cfgManager.AddTeamBindKey(_teamKeyInput.Text))
        {
            _teamKeyInput.Text = string.Empty;
        }

        RefreshView();
    }

    private void OnRemoveTeamKeyPressed()
    {
        if (_cfgManager == null || _teamKeyInput == null)
        {
            return;
        }

        if (_cfgManager.RemoveTeamBindKey(_teamKeyInput.Text))
        {
            _teamKeyInput.Text = string.Empty;
        }

        RefreshView();
    }

    private void OnPreferTeamChatToggled(bool pressed)
    {
        _configManager?.UpdatePreferTeamChat(pressed);
    }

    private void ApplyTexts()
    {
        GetNode<Label>("MarginContainer/Content/Title").Text = LocalizationManager.T("cfg.title");
        GetNode<Label>("MarginContainer/Content/Hint").Text = LocalizationManager.T("cfg.hint");
        GetNode<Label>("MarginContainer/Content/Cs2PathLabel").Text = LocalizationManager.T("cfg.cs2_path");
        GetNode<Label>("MarginContainer/Content/GlobalKeysSection/GlobalKeysTitle").Text = LocalizationManager.T("cfg.global_keys");
        GetNode<Label>("MarginContainer/Content/TeamKeysSection/TeamKeysTitle").Text = LocalizationManager.T("cfg.team_keys");

        if (_cs2PathInput != null)
        {
            _cs2PathInput.PlaceholderText = "Steam\\steamapps\\common\\Counter-Strike 2";
        }

        if (_globalKeyInput != null)
        {
            _globalKeyInput.PlaceholderText = LocalizationManager.T("cfg.key_placeholder", "k");
        }

        if (_teamKeyInput != null)
        {
            _teamKeyInput.PlaceholderText = LocalizationManager.T("cfg.key_placeholder", "l");
        }

        if (_applyPathButton != null)
        {
            _applyPathButton.Text = LocalizationManager.T("cfg.apply_path");
        }

        if (_detectPathButton != null)
        {
            _detectPathButton.Text = LocalizationManager.T("cfg.detect_path");
        }

        if (_generateButton != null)
        {
            _generateButton.Text = LocalizationManager.T("cfg.generate");
        }

        if (_restoreButton != null)
        {
            _restoreButton.Text = LocalizationManager.T("cfg.restore");
        }

        if (_addGlobalKeyButton != null)
        {
            _addGlobalKeyButton.Text = LocalizationManager.T("common.add");
        }

        if (_removeGlobalKeyButton != null)
        {
            _removeGlobalKeyButton.Text = LocalizationManager.T("common.delete");
        }

        if (_addTeamKeyButton != null)
        {
            _addTeamKeyButton.Text = LocalizationManager.T("common.add");
        }

        if (_removeTeamKeyButton != null)
        {
            _removeTeamKeyButton.Text = LocalizationManager.T("common.delete");
        }

        if (_preferTeamChatToggle != null)
        {
            _preferTeamChatToggle.Text = LocalizationManager.T("cfg.prefer_team_chat");
        }
    }

    private void RefreshView()
    {
        if (!_uiInitialized)
        {
            return;
        }

        if (_configManager == null || _cfgManager == null || _cs2PathInput == null || _preferTeamChatToggle == null ||
            _globalKeysLabel == null || _teamKeysLabel == null)
        {
            return;
        }

        _cs2PathInput.Text = _configManager.Config.CS2Path;
        _preferTeamChatToggle.SetPressedNoSignal(_configManager.Config.PreferTeamChat);
        _globalKeysLabel.Text =
            LocalizationManager.T("cfg.current_global_keys", string.Join(", ", _cfgManager.BindKeys.Select(key => key.ToUpperInvariant())));
        _teamKeysLabel.Text =
            LocalizationManager.T("cfg.current_team_keys", string.Join(", ", _cfgManager.TeamBindKeys.Select(key => key.ToUpperInvariant())));
    }
}
