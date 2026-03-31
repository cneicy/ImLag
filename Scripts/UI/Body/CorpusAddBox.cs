using System.Threading.Tasks;
using CommonSDK.Event;
using CommonSDK.Utils;
using Godot;
using ImLag.GUI.Scripts.Core;

namespace ImLag.GUI.Scripts.UI.Body;

[EventBusSubscriber]
public partial class CorpusAddBox : VBoxContainer
{
    private LineEdit _lineEdit = null!;
    private Button _addButton = null!;
    private Button _importButton = null!;
    private Button _exportButton = null!;
    private LineEdit _urlInput = null!;
    private Button _importUrlButton = null!;
    private FileDialog _importDialog = null!;
    private FileDialog _exportDialog = null!;
    private ChatMessageManager _chatManager = null!;

    public override void _Ready()
    {
        base._Ready();
        _lineEdit = GetNode<LineEdit>("AddRow/LineEdit");
        _addButton = GetNode<Button>("AddRow/Button");
        _importButton = GetNode<Button>("ActionRow/ImportButton");
        _exportButton = GetNode<Button>("ActionRow/ExportButton");
        _urlInput = GetNode<LineEdit>("UrlRow/UrlInput");
        _importUrlButton = GetNode<Button>("UrlRow/ImportUrlButton");
        _chatManager = GetTree().Root.FindObjectOfType<Entry>().ChatManager;

        CreateDialogs();
        ApplyTexts();

        _addButton.Pressed += OnAddButtonPressed;
        _lineEdit.TextSubmitted += OnAddSubmitted;
        _importButton.Pressed += OnImportButtonPressed;
        _exportButton.Pressed += OnExportButtonPressed;
        _importUrlButton.Pressed += OnImportUrlButtonPressed;
        _urlInput.TextSubmitted += OnUrlSubmitted;
    }

    [EventSubscribe]
    public void OnLanguageChangedEvent(LanguageChangedEvent evt)
    {
        ApplyTexts();
    }

    private void CreateDialogs()
    {
        _importDialog = new FileDialog
        {
            Access = FileDialog.AccessEnum.Filesystem,
            FileMode = FileDialog.FileModeEnum.OpenFile,
            Title = LocalizationManager.T("corpus.dialog.import_title"),
            ModeOverridesTitle = false,
            UseNativeDialog = true
        };
        _importDialog.Filters = ["*.txt,*.json ; Corpus Files"];
        _importDialog.FileSelected += OnImportFileSelected;
        AddChild(_importDialog);

        _exportDialog = new FileDialog
        {
            Access = FileDialog.AccessEnum.Filesystem,
            FileMode = FileDialog.FileModeEnum.SaveFile,
            Title = LocalizationManager.T("corpus.dialog.export_title"),
            CurrentFile = LocalizationManager.T("corpus.dialog.default_name"),
            ModeOverridesTitle = false,
            UseNativeDialog = true
        };
        _exportDialog.Filters = ["*.txt ; Text Files", "*.json ; JSON Files"];
        _exportDialog.FileSelected += OnExportFileSelected;
        AddChild(_exportDialog);
    }

    private void ApplyTexts()
    {
        _lineEdit.PlaceholderText = LocalizationManager.T("corpus.placeholder");
        _addButton.Text = LocalizationManager.T("common.add");
        _importButton.Text = LocalizationManager.T("corpus.import_file");
        _exportButton.Text = LocalizationManager.T("corpus.export_file");
        _urlInput.PlaceholderText = LocalizationManager.T("corpus.url_placeholder");
        _importUrlButton.Text = LocalizationManager.T("corpus.import_url");

        if (_importDialog != null)
        {
            _importDialog.Title = LocalizationManager.T("corpus.dialog.import_title");
        }

        if (_exportDialog != null)
        {
            _exportDialog.Title = LocalizationManager.T("corpus.dialog.export_title");
            _exportDialog.CurrentFile = LocalizationManager.T("corpus.dialog.default_name");
        }
    }

    private void OnAddSubmitted(string _)
    {
        TryAddMessage();
    }

    private void OnAddButtonPressed()
    {
        TryAddMessage();
    }

    private void TryAddMessage()
    {
        if (_chatManager.AddMessage(_lineEdit.Text))
        {
            _lineEdit.Text = string.Empty;
            EventBus.TriggerEvent(new CorpusItemRefreshEvent());
        }
    }

    private void OnImportButtonPressed()
    {
        _importDialog.PopupCentered();
    }

    private void OnExportButtonPressed()
    {
        _exportDialog.CurrentFile = LocalizationManager.T("corpus.dialog.default_name");
        _exportDialog.PopupCentered();
    }

    private void OnImportFileSelected(string path)
    {
        var result = _chatManager.ImportMessagesFromFile(path);
        if (result.AddedCount > 0)
        {
            EventBus.TriggerEvent(new CorpusItemRefreshEvent());
        }
    }

    private void OnExportFileSelected(string path)
    {
        _chatManager.ExportMessages(path);
    }

    private void OnUrlSubmitted(string _)
    {
        var __ = ImportFromUrlAsync();
    }

    private void OnImportUrlButtonPressed()
    {
        _ = ImportFromUrlAsync();
    }

    private async Task ImportFromUrlAsync()
    {
        _importUrlButton.Disabled = true;
        try
        {
            var result = await _chatManager.ImportMessagesFromUrlAsync(_urlInput.Text);
            if (result.AddedCount > 0)
            {
                _urlInput.Text = string.Empty;
                EventBus.TriggerEvent(new CorpusItemRefreshEvent());
            }
        }
        finally
        {
            _importUrlButton.Disabled = false;
        }
    }
}
