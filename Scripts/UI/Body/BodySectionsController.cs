using CommonSDK.Event;
using Godot;
using ImLag.GUI.Scripts.Core;
using ImLag.GUI.Scripts.UI.Head;

namespace ImLag.GUI.Scripts.UI.Body;

[EventBusSubscriber]
public partial class BodySectionsController : VBoxContainer
{
    private Button? _generalHeader;
    private Control? _generalPanel;
    private Button? _cfgHeader;
    private Control? _cfgPanel;
    private Button? _chatHeader;
    private Control? _chatPanel;
    private Button? _corpusHeader;
    private Control? _corpusPanel;

    private bool _generalExpanded;
    private bool _cfgExpanded = true;
    private bool _chatExpanded = true;
    private bool _corpusExpanded = true;
    private bool _useCfgMode = true;
    private bool _uiInitialized;

    public override void _Ready()
    {
        base._Ready();
        InitializeUi();
        RefreshSections();
    }

    private void InitializeUi()
    {
        if (_uiInitialized)
        {
            return;
        }

        _generalHeader = GetNode<Button>("GeneralHeader");
        _generalPanel = GetNode<Control>("GeneralPanel");
        _cfgHeader = GetNode<Button>("CfgHeader");
        _cfgPanel = GetNode<Control>("CfgPanel");
        _chatHeader = GetNode<Button>("ChatHeader");
        _chatPanel = GetNode<Control>("ChatPanel");
        _corpusHeader = GetNode<Button>("CorpusHeader");
        _corpusPanel = GetNode<Control>("CorpusPanel");

        _generalHeader.Pressed += OnGeneralHeaderPressed;
        _cfgHeader.Pressed += OnCfgHeaderPressed;
        _chatHeader.Pressed += OnChatHeaderPressed;
        _corpusHeader.Pressed += OnCorpusHeaderPressed;
        _uiInitialized = true;
    }

    [EventSubscribe]
    public void OnProgramInitEvent(ProgramInitEvent evt)
    {
        _useCfgMode = evt.ConfigManager.Config.UseCfgMode;
        RefreshSections();
    }

    [EventSubscribe]
    public void OnModeChanged(ModeUpdateEvent evt)
    {
        _useCfgMode = evt.UseCfgMode;
        RefreshSections();
    }

    [EventSubscribe]
    public void OnLanguageChangedEvent(LanguageChangedEvent evt)
    {
        RefreshSections();
    }

    private void OnGeneralHeaderPressed()
    {
        _generalExpanded = !_generalExpanded;
        RefreshSections();
    }

    private void OnCfgHeaderPressed()
    {
        _cfgExpanded = !_cfgExpanded;
        RefreshSections();
    }

    private void OnChatHeaderPressed()
    {
        _chatExpanded = !_chatExpanded;
        RefreshSections();
    }

    private void OnCorpusHeaderPressed()
    {
        _corpusExpanded = !_corpusExpanded;
        RefreshSections();
    }

    private void RefreshSections()
    {
        if (!_uiInitialized || _generalHeader == null || _generalPanel == null || _cfgHeader == null || _cfgPanel == null ||
            _chatHeader == null || _chatPanel == null || _corpusHeader == null || _corpusPanel == null)
        {
            return;
        }

        ApplySection(_generalHeader, _generalPanel, _generalExpanded, true, LocalizationManager.T("section.general"));
        ApplySection(_cfgHeader, _cfgPanel, _cfgExpanded, _useCfgMode, LocalizationManager.T("section.cfg"));
        ApplySection(_chatHeader, _chatPanel, _chatExpanded, !_useCfgMode, LocalizationManager.T("section.chat"));
        ApplySection(_corpusHeader, _corpusPanel, _corpusExpanded, true, LocalizationManager.T("section.corpus"));
    }

    private static void ApplySection(Button header, Control content, bool expanded, bool available, string title)
    {
        header.Visible = available;
        content.Visible = available && expanded;
        header.Text = $"{(expanded ? "-" : "+")} {title}";
    }
}
