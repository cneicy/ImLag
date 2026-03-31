using CommonSDK.Event;
using Godot;
using ImLag.GUI.Scripts.Core;
using ImLag.GUI.Scripts.UI.Head;

namespace ImLag.GUI.Scripts.UI.Body;

[EventBusSubscriber]
public partial class ChatModePanel : PanelContainer
{
    private LineEdit? _chatKeyInput;
    private Button? _applyChatKeyButton;
    private SpinBox? _keyDelayInput;
    private Button? _applyKeyDelayButton;
    private CheckButton? _skipWindowCheckToggle;
    private CheckButton? _forceModeToggle;
    private bool _uiInitialized;

    private ConfigManager? _configManager;

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

        _chatKeyInput = GetNode<LineEdit>("MarginContainer/Content/ChatKeyRow/ChatKeyInput");
        _applyChatKeyButton = GetNode<Button>("MarginContainer/Content/ChatKeyRow/ApplyChatKeyButton");
        _keyDelayInput = GetNode<SpinBox>("MarginContainer/Content/KeyDelayRow/KeyDelayInput");
        _applyKeyDelayButton = GetNode<Button>("MarginContainer/Content/KeyDelayRow/ApplyKeyDelayButton");
        _skipWindowCheckToggle = GetNode<CheckButton>("MarginContainer/Content/SkipWindowCheckToggle");
        _forceModeToggle = GetNode<CheckButton>("MarginContainer/Content/ForceModeToggle");

        _chatKeyInput.PlaceholderText = "y / u / enter";
        _chatKeyInput.MaxLength = 5;
        ApplyTexts();

        _applyChatKeyButton.Pressed += OnApplyChatKeyPressed;
        _applyKeyDelayButton.Pressed += OnApplyKeyDelayPressed;
        _skipWindowCheckToggle.Toggled += OnSkipWindowCheckToggled;
        _forceModeToggle.Toggled += OnForceModeToggled;
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

    private void OnApplyChatKeyPressed()
    {
        if (_configManager == null || _chatKeyInput == null)
        {
            return;
        }

        var chatKey = _chatKeyInput.Text.Trim().ToLowerInvariant();
        if (string.IsNullOrEmpty(chatKey))
        {
            return;
        }

        _configManager.UpdateChatKey(chatKey);
        RefreshView();
    }

    private void OnApplyKeyDelayPressed()
    {
        if (_configManager == null || _keyDelayInput == null)
        {
            return;
        }

        _configManager.UpdateKeyDelay((int)_keyDelayInput.Value);
        RefreshView();
    }

    private void OnSkipWindowCheckToggled(bool pressed)
    {
        _configManager?.UpdateSkipWindowCheck(pressed);
    }

    private void OnForceModeToggled(bool pressed)
    {
        _configManager?.UpdateForceMode(pressed);
    }

    private void ApplyTexts()
    {
        GetNode<Label>("MarginContainer/Content/Title").Text = LocalizationManager.T("chat.title");
        GetNode<Label>("MarginContainer/Content/Hint").Text = LocalizationManager.T("chat.hint");
        GetNode<Label>("MarginContainer/Content/ChatKeyLabel").Text = LocalizationManager.T("chat.key");
        GetNode<Label>("MarginContainer/Content/KeyDelayLabel").Text = LocalizationManager.T("chat.delay");

        if (_applyChatKeyButton != null)
        {
            _applyChatKeyButton.Text = LocalizationManager.T("chat.apply_key");
        }

        if (_applyKeyDelayButton != null)
        {
            _applyKeyDelayButton.Text = LocalizationManager.T("chat.apply_delay");
        }

        if (_skipWindowCheckToggle != null)
        {
            _skipWindowCheckToggle.Text = LocalizationManager.T("chat.skip_window_check");
        }

        if (_forceModeToggle != null)
        {
            _forceModeToggle.Text = LocalizationManager.T("chat.force_mode");
        }
    }

    private void RefreshView()
    {
        if (!_uiInitialized)
        {
            return;
        }

        if (_configManager == null || _chatKeyInput == null || _keyDelayInput == null ||
            _skipWindowCheckToggle == null || _forceModeToggle == null)
        {
            return;
        }

        _chatKeyInput.Text = _configManager.Config.ChatKey;
        _keyDelayInput.Value = _configManager.Config.KeyDelay;
        _skipWindowCheckToggle.SetPressedNoSignal(_configManager.Config.SkipWindowCheck);
        _forceModeToggle.SetPressedNoSignal(_configManager.Config.ForceMode);
    }
}
