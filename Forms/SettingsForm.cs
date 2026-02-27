using ClipTranslator.Models;

namespace ClipTranslator.Forms;

/// <summary>
/// API 키, Provider 선택, 언어, 단축키 등을 설정하는 폼.
/// </summary>
public class SettingsForm : Form
{
    private readonly AppSettings _settings;

    private RadioButton _rdoClaude = null!;
    private RadioButton _rdoOpenAI = null!;
    private RadioButton _rdoGemini = null!;

    private TextBox _txtClaudeKey = null!;
    private TextBox _txtOpenAIKey = null!;
    private TextBox _txtGeminiKey = null!;

    private TextBox _txtClaudeModel = null!;
    private TextBox _txtOpenAIModel = null!;
    private TextBox _txtGeminiModel = null!;

    private ComboBox _cboTargetLanguage = null!;
    private NumericUpDown _nudPopupDuration = null!;
    private TextBox _txtSendHotkey = null!;
    private TextBox _txtToggleHotkey = null!;

    /// <summary>
    /// 설정이 저장되었을 때 발생.
    /// </summary>
    public event Action<AppSettings>? SettingsSaved;

    public SettingsForm(AppSettings settings)
    {
        _settings = settings;
        InitializeComponents();
        LoadSettings();
    }

    private void InitializeComponents()
    {
        Text = "ClipTranslator 설정";
        Size = new Size(480, 580);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        StartPosition = FormStartPosition.CenterScreen;
        BackColor = Color.FromArgb(245, 245, 245);
        Font = new Font("Segoe UI", 9f);

        var tabControl = new TabControl
        {
            Dock = DockStyle.Fill,
            Padding = new Point(12, 6)
        };

        // === API 탭 ===
        var tabApi = new TabPage("API 설정");
        var apiPanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            AutoScroll = true,
            Padding = new Padding(10)
        };

        // Provider 선택
        var grpProvider = CreateGroupBox("AI Provider 선택", 120);
        _rdoClaude = new RadioButton { Text = "Claude (Anthropic)", Location = new Point(15, 25), AutoSize = true };
        _rdoOpenAI = new RadioButton { Text = "OpenAI (GPT)", Location = new Point(15, 50), AutoSize = true };
        _rdoGemini = new RadioButton { Text = "Gemini (Google)", Location = new Point(15, 75), AutoSize = true };
        grpProvider.Controls.AddRange(new Control[] { _rdoClaude, _rdoOpenAI, _rdoGemini });

        // API 키
        var grpKeys = CreateGroupBox("API 키", 200);
        AddLabelAndTextBox(grpKeys, "Claude API Key:", 25, out _txtClaudeKey);
        AddLabelAndTextBox(grpKeys, "OpenAI API Key:", 75, out _txtOpenAIKey);
        AddLabelAndTextBox(grpKeys, "Gemini API Key:", 125, out _txtGeminiKey);
        _txtClaudeKey.UseSystemPasswordChar = true;
        _txtOpenAIKey.UseSystemPasswordChar = true;
        _txtGeminiKey.UseSystemPasswordChar = true;

        // 모델 설정
        var grpModels = CreateGroupBox("모델 설정", 150);
        AddLabelAndTextBox(grpModels, "Claude:", 25, out _txtClaudeModel);
        AddLabelAndTextBox(grpModels, "OpenAI:", 55, out _txtOpenAIModel);
        AddLabelAndTextBox(grpModels, "Gemini:", 85, out _txtGeminiModel);

        apiPanel.Controls.AddRange(new Control[] { grpProvider, grpKeys, grpModels });
        tabApi.Controls.Add(apiPanel);

        // === 일반 탭 ===
        var tabGeneral = new TabPage("일반 설정");
        var genPanel = new Panel { Dock = DockStyle.Fill, Padding = new Padding(15) };

        var lblLang = new Label { Text = "기본 타겟 언어:", Location = new Point(15, 20), AutoSize = true };
        _cboTargetLanguage = new ComboBox
        {
            Location = new Point(15, 42),
            Size = new Size(200, 25),
            DropDownStyle = ComboBoxStyle.DropDownList
        };
        _cboTargetLanguage.Items.AddRange(new object[]
        {
            "English", "Japanese", "Chinese", "Spanish",
            "French", "German", "Vietnamese", "Thai", "Russian"
        });

        var lblDuration = new Label { Text = "팝업 표시 시간 (초):", Location = new Point(15, 85), AutoSize = true };
        _nudPopupDuration = new NumericUpDown
        {
            Location = new Point(15, 107),
            Size = new Size(80, 25),
            Minimum = 1,
            Maximum = 30,
            Value = 5
        };

        var grpHotkey = CreateGroupBox("단축키", 100);
        grpHotkey.Location = new Point(15, 150);
        var lblSend = new Label { Text = "발신 번역:", Location = new Point(15, 25), AutoSize = true };
        _txtSendHotkey = new TextBox { Location = new Point(120, 22), Size = new Size(150, 25), ReadOnly = true, BackColor = Color.White };
        var lblToggle = new Label { Text = "모니터링 토글:", Location = new Point(15, 55), AutoSize = true };
        _txtToggleHotkey = new TextBox { Location = new Point(120, 52), Size = new Size(150, 25), ReadOnly = true, BackColor = Color.White };
        grpHotkey.Controls.AddRange(new Control[] { lblSend, _txtSendHotkey, lblToggle, _txtToggleHotkey });

        genPanel.Controls.AddRange(new Control[] { lblLang, _cboTargetLanguage, lblDuration, _nudPopupDuration, grpHotkey });
        tabGeneral.Controls.Add(genPanel);

        tabControl.TabPages.AddRange(new[] { tabApi, tabGeneral });

        // 저장/취소 버튼
        var btnPanel = new Panel { Dock = DockStyle.Bottom, Height = 50 };
        var btnSave = new Button
        {
            Text = "저장",
            Size = new Size(100, 35),
            Location = new Point(250, 8),
            BackColor = Color.FromArgb(0, 120, 215),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat
        };
        btnSave.Click += BtnSave_Click;

        var btnCancel = new Button
        {
            Text = "취소",
            Size = new Size(100, 35),
            Location = new Point(360, 8),
            FlatStyle = FlatStyle.Flat
        };
        btnCancel.Click += (_, _) => Close();

        btnPanel.Controls.AddRange(new Control[] { btnSave, btnCancel });

        Controls.Add(tabControl);
        Controls.Add(btnPanel);
    }

    private void LoadSettings()
    {
        _rdoClaude.Checked = _settings.SelectedProvider == ApiProviderType.Claude;
        _rdoOpenAI.Checked = _settings.SelectedProvider == ApiProviderType.OpenAI;
        _rdoGemini.Checked = _settings.SelectedProvider == ApiProviderType.Gemini;

        _txtClaudeKey.Text = _settings.ClaudeApiKey;
        _txtOpenAIKey.Text = _settings.OpenAIApiKey;
        _txtGeminiKey.Text = _settings.GeminiApiKey;

        _txtClaudeModel.Text = _settings.ClaudeModel;
        _txtOpenAIModel.Text = _settings.OpenAIModel;
        _txtGeminiModel.Text = _settings.GeminiModel;

        _cboTargetLanguage.SelectedItem = _settings.DefaultTargetLanguage;
        if (_cboTargetLanguage.SelectedIndex < 0) _cboTargetLanguage.SelectedIndex = 0;

        _nudPopupDuration.Value = _settings.PopupDurationSeconds;
        _txtSendHotkey.Text = _settings.SendHotkey;
        _txtToggleHotkey.Text = _settings.ToggleHotkey;
    }

    private void BtnSave_Click(object? sender, EventArgs e)
    {
        if (_rdoClaude.Checked) _settings.SelectedProvider = ApiProviderType.Claude;
        else if (_rdoOpenAI.Checked) _settings.SelectedProvider = ApiProviderType.OpenAI;
        else if (_rdoGemini.Checked) _settings.SelectedProvider = ApiProviderType.Gemini;

        _settings.ClaudeApiKey = _txtClaudeKey.Text.Trim();
        _settings.OpenAIApiKey = _txtOpenAIKey.Text.Trim();
        _settings.GeminiApiKey = _txtGeminiKey.Text.Trim();

        _settings.ClaudeModel = _txtClaudeModel.Text.Trim();
        _settings.OpenAIModel = _txtOpenAIModel.Text.Trim();
        _settings.GeminiModel = _txtGeminiModel.Text.Trim();

        _settings.DefaultTargetLanguage = _cboTargetLanguage.SelectedItem?.ToString() ?? "English";
        _settings.PopupDurationSeconds = (int)_nudPopupDuration.Value;

        _settings.Save();
        SettingsSaved?.Invoke(_settings);
        Close();
    }

    private static GroupBox CreateGroupBox(string text, int height)
    {
        return new GroupBox
        {
            Text = text,
            Size = new Size(420, height),
            Font = new Font("Segoe UI", 9f, FontStyle.Bold)
        };
    }

    private static void AddLabelAndTextBox(GroupBox parent, string labelText, int y, out TextBox textBox)
    {
        var lbl = new Label
        {
            Text = labelText,
            Location = new Point(15, y),
            AutoSize = true,
            Font = new Font("Segoe UI", 9f)
        };
        textBox = new TextBox
        {
            Location = new Point(120, y - 3),
            Size = new Size(280, 25),
            Font = new Font("Segoe UI", 9f)
        };
        parent.Controls.AddRange(new Control[] { lbl, textBox });
    }
}
