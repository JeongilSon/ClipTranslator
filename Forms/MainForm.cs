using ClipTranslator.Models;
using ClipTranslator.Services;

namespace ClipTranslator.Forms;

/// <summary>
/// 메인 폼. 시스템 트레이에 상주하면서 클립보드 모니터링 및 번역을 관리한다.
/// </summary>
public class MainForm : Form
{
    private readonly NotifyIcon _trayIcon;
    private readonly ContextMenuStrip _trayMenu;
    private readonly ClipboardMonitor _clipboardMonitor;
    private readonly HotkeyManager _hotkeyManager;
    private readonly TranslationService _translationService;
    private readonly FloatingPopup _popup;
    private AppSettings _settings;

    private ToolStripMenuItem _mnuToggle = null!;
    private bool _isProcessing;

    public MainForm()
    {
        // 폼 숨기기 (트레이 전용)
        ShowInTaskbar = false;
        WindowState = FormWindowState.Minimized;
        FormBorderStyle = FormBorderStyle.FixedToolWindow;
        Opacity = 0;
        Size = new Size(1, 1);

        // 설정 로드
        _settings = AppSettings.Load();

        // 서비스 초기화
        _clipboardMonitor = new ClipboardMonitor();
        _hotkeyManager = new HotkeyManager();
        _translationService = new TranslationService();
        _popup = new FloatingPopup();

        // Provider 설정
        ConfigureProvider();

        // 트레이 메뉴 생성
        _trayMenu = CreateTrayMenu();

        // 트레이 아이콘 설정
        _trayIcon = new NotifyIcon
        {
            Text = "ClipTranslator - 클립보드 자동 번역",
            Icon = CreateDefaultIcon(),
            ContextMenuStrip = _trayMenu,
            Visible = true
        };
        _trayIcon.DoubleClick += (_, _) => OpenSettings();

        // 이벤트 연결
        _clipboardMonitor.ClipboardTextChanged += OnClipboardTextChanged;
        _hotkeyManager.HotkeyPressed += OnHotkeyPressed;
        _popup.OnCopied += () =>
        {
            _trayIcon.ShowBalloonTip(1000, "ClipTranslator", "번역문이 클립보드에 복사되었습니다.", ToolTipIcon.Info);
        };

        // 클립보드 모니터링 시작
        if (_settings.IsMonitoringEnabled)
            _clipboardMonitor.StartListening();

        // 글로벌 단축키 등록
        RegisterHotkeys();
    }

    private void ConfigureProvider()
    {
        try
        {
            _translationService.Configure(_settings);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Provider 설정 실패: {ex.Message}");
        }
    }

    private void RegisterHotkeys()
    {
        _hotkeyManager.RegisterHotkey(HotkeyManager.HOTKEY_SEND_TRANSLATE, _settings.SendHotkey);
        _hotkeyManager.RegisterHotkey(HotkeyManager.HOTKEY_TOGGLE, _settings.ToggleHotkey);
    }

    private ContextMenuStrip CreateTrayMenu()
    {
        var menu = new ContextMenuStrip();

        _mnuToggle = new ToolStripMenuItem(
            _settings.IsMonitoringEnabled ? "모니터링 비활성화" : "모니터링 활성화");
        _mnuToggle.Click += (_, _) => ToggleMonitoring();

        var mnuSettings = new ToolStripMenuItem("설정...");
        mnuSettings.Click += (_, _) => OpenSettings();

        var mnuHistory = new ToolStripMenuItem("번역 이력");
        mnuHistory.Click += (_, _) => ShowHistory();

        var mnuExit = new ToolStripMenuItem("종료");
        mnuExit.Click += (_, _) =>
        {
            _trayIcon.Visible = false;
            Application.Exit();
        };

        menu.Items.AddRange(new ToolStripItem[]
        {
            _mnuToggle,
            new ToolStripSeparator(),
            mnuSettings,
            mnuHistory,
            new ToolStripSeparator(),
            mnuExit
        });

        return menu;
    }

    private async void OnClipboardTextChanged(object? sender, string text)
    {
        if (!_settings.IsMonitoringEnabled || _isProcessing)
            return;

        // 너무 짧거나 긴 텍스트 무시
        if (text.Length < 2 || text.Length > 5000)
            return;

        _isProcessing = true;
        try
        {
            var result = await _translationService.TranslateAsync(text, _settings.DefaultTargetLanguage);

            if (InvokeRequired)
            {
                Invoke(() => ShowResult(result));
            }
            else
            {
                ShowResult(result);
            }
        }
        finally
        {
            _isProcessing = false;
        }
    }

    private void ShowResult(TranslationResult result)
    {
        if (result.IsSuccess)
        {
            _popup.ShowTranslation(
                result.OriginalText,
                result.TranslatedText,
                result.ProviderName,
                _settings.PopupDurationSeconds);
        }
        else
        {
            _popup.ShowError(result.ErrorMessage ?? "알 수 없는 오류");
        }
    }

    private async void OnHotkeyPressed(object? sender, int hotkeyId)
    {
        switch (hotkeyId)
        {
            case HotkeyManager.HOTKEY_SEND_TRANSLATE:
                await HandleSendTranslate();
                break;
            case HotkeyManager.HOTKEY_TOGGLE:
                ToggleMonitoring();
                break;
        }
    }

    /// <summary>
    /// 발신 번역: 현재 클립보드 텍스트를 번역하여 클립보드를 덮어쓴다.
    /// </summary>
    private async Task HandleSendTranslate()
    {
        if (_isProcessing) return;

        string? text = null;
        if (InvokeRequired)
        {
            Invoke(() =>
            {
                if (Clipboard.ContainsText())
                    text = Clipboard.GetText().Trim();
            });
        }
        else
        {
            if (Clipboard.ContainsText())
                text = Clipboard.GetText().Trim();
        }

        if (string.IsNullOrEmpty(text)) return;

        _isProcessing = true;
        try
        {
            var result = await _translationService.TranslateAsync(text, _settings.DefaultTargetLanguage);
            if (result.IsSuccess && !string.IsNullOrEmpty(result.TranslatedText))
            {
                if (InvokeRequired)
                {
                    Invoke(() =>
                    {
                        _clipboardMonitor.SetClipboardWithoutNotify(result.TranslatedText);
                        _trayIcon.ShowBalloonTip(1500, "ClipTranslator",
                            "번역 완료! Ctrl+V로 붙여넣으세요.", ToolTipIcon.Info);
                    });
                }
                else
                {
                    _clipboardMonitor.SetClipboardWithoutNotify(result.TranslatedText);
                    _trayIcon.ShowBalloonTip(1500, "ClipTranslator",
                        "번역 완료! Ctrl+V로 붙여넣으세요.", ToolTipIcon.Info);
                }
            }
            else
            {
                Invoke(() => _popup.ShowError(result.ErrorMessage ?? "번역 실패"));
            }
        }
        finally
        {
            _isProcessing = false;
        }
    }

    private void ToggleMonitoring()
    {
        _settings.IsMonitoringEnabled = !_settings.IsMonitoringEnabled;

        if (_settings.IsMonitoringEnabled)
        {
            _clipboardMonitor.StartListening();
            _mnuToggle.Text = "모니터링 비활성화";
            _trayIcon.ShowBalloonTip(1000, "ClipTranslator", "클립보드 모니터링 활성화", ToolTipIcon.Info);
        }
        else
        {
            _clipboardMonitor.StopListening();
            _mnuToggle.Text = "모니터링 활성화";
            _trayIcon.ShowBalloonTip(1000, "ClipTranslator", "클립보드 모니터링 비활성화", ToolTipIcon.Info);
        }

        _settings.Save();
    }

    private void OpenSettings()
    {
        var form = new SettingsForm(_settings);
        form.SettingsSaved += newSettings =>
        {
            _settings = newSettings;
            ConfigureProvider();

            // 단축키 재등록
            _hotkeyManager.UnregisterAll();
            RegisterHotkeys();
        };
        form.ShowDialog();
    }

    private void ShowHistory()
    {
        var items = _translationService.History.Items;
        if (items.Count == 0)
        {
            MessageBox.Show("번역 이력이 없습니다.", "번역 이력", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        var historyText = string.Join("\n\n", items.Select(i =>
            $"[{i.Timestamp:HH:mm:ss}] ({i.ProviderName})\n원문: {i.OriginalText}\n번역: {i.TranslatedText}"));

        var form = new Form
        {
            Text = "번역 이력",
            Size = new Size(600, 500),
            StartPosition = FormStartPosition.CenterScreen
        };
        var txt = new TextBox
        {
            Multiline = true,
            ReadOnly = true,
            ScrollBars = ScrollBars.Vertical,
            Dock = DockStyle.Fill,
            Font = new Font("Segoe UI", 10f),
            Text = historyText
        };
        form.Controls.Add(txt);
        form.Show();
    }

    /// <summary>
    /// 기본 트레이 아이콘 생성 (코드에서 직접 그리기)
    /// </summary>
    private static Icon CreateDefaultIcon()
    {
        var bmp = new Bitmap(32, 32);
        using var g = Graphics.FromImage(bmp);
        g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

        // 배경 원
        using var bgBrush = new SolidBrush(Color.FromArgb(0, 120, 215));
        g.FillEllipse(bgBrush, 1, 1, 30, 30);

        // "T" 글자
        using var font = new Font("Segoe UI", 16f, FontStyle.Bold);
        using var textBrush = new SolidBrush(Color.White);
        var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
        g.DrawString("T", font, textBrush, new RectangleF(0, 0, 32, 32), sf);

        return Icon.FromHandle(bmp.GetHicon());
    }

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        // X 버튼 클릭 시 트레이로 최소화
        if (e.CloseReason == CloseReason.UserClosing)
        {
            e.Cancel = true;
            Hide();
            return;
        }
        base.OnFormClosing(e);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _clipboardMonitor.Dispose();
            _hotkeyManager.Dispose();
            _translationService.Dispose();
            _popup.Dispose();
            _trayIcon.Dispose();
            _trayMenu.Dispose();
        }
        base.Dispose(disposing);
    }
}
