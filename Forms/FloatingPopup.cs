using System.Drawing.Drawing2D;

namespace ClipTranslator.Forms;

/// <summary>
/// 번역 결과를 표시하는 플로팅 팝업 창.
/// 테두리 없는 둥근 모서리 폼으로, 클릭 시 번역문을 클립보드에 복사한다.
/// </summary>
public class FloatingPopup : Form
{
    private readonly Label _lblOriginal;
    private readonly Label _lblTranslated;
    private readonly Label _lblProvider;
    private readonly System.Windows.Forms.Timer _fadeTimer;
    private readonly System.Windows.Forms.Timer _autoCloseTimer;
    private double _opacity = 1.0;
    private string _translatedText = string.Empty;

    /// <summary>
    /// 번역문이 클립보드에 복사되었을 때 발생.
    /// </summary>
    public event Action? OnCopied;

    public FloatingPopup()
    {
        // 폼 기본 설정
        FormBorderStyle = FormBorderStyle.None;
        ShowInTaskbar = false;
        TopMost = true;
        StartPosition = FormStartPosition.Manual;
        BackColor = Color.FromArgb(30, 30, 30);
        Size = new Size(400, 140);
        Padding = new Padding(12);
        DoubleBuffered = true;

        // Provider 라벨 (좌측 상단)
        _lblProvider = new Label
        {
            AutoSize = true,
            ForeColor = Color.FromArgb(120, 120, 120),
            Font = new Font("Segoe UI", 8f),
            Location = new Point(16, 8),
            BackColor = Color.Transparent
        };

        // 원문 라벨 (작게)
        _lblOriginal = new Label
        {
            ForeColor = Color.FromArgb(160, 160, 160),
            Font = new Font("Segoe UI", 9f),
            Location = new Point(16, 28),
            Size = new Size(368, 36),
            BackColor = Color.Transparent,
            AutoEllipsis = true
        };

        // 번역문 라벨 (크게, 볼드)
        _lblTranslated = new Label
        {
            ForeColor = Color.FromArgb(240, 240, 240),
            Font = new Font("Segoe UI", 11f, FontStyle.Bold),
            Location = new Point(16, 68),
            Size = new Size(368, 56),
            BackColor = Color.Transparent,
            AutoEllipsis = true,
            Cursor = Cursors.Hand
        };

        Controls.AddRange(new Control[] { _lblProvider, _lblOriginal, _lblTranslated });

        // 클릭 시 번역문 복사
        _lblTranslated.Click += (_, _) => CopyTranslation();
        Click += (_, _) => CopyTranslation();

        // 마우스 호버 시 툴팁
        var tooltip = new ToolTip();
        tooltip.SetToolTip(_lblTranslated, "클릭하면 번역문을 클립보드에 복사합니다");

        // 자동 닫힘 타이머
        _autoCloseTimer = new System.Windows.Forms.Timer { Interval = 5000 };
        _autoCloseTimer.Tick += (_, _) => StartFadeOut();

        // 페이드아웃 타이머
        _fadeTimer = new System.Windows.Forms.Timer { Interval = 30 };
        _fadeTimer.Tick += FadeTimer_Tick;

        // 마우스가 올려져 있으면 자동 닫힘 일시정지
        MouseEnter += (_, _) => _autoCloseTimer.Stop();
        MouseLeave += (_, _) => _autoCloseTimer.Start();
        _lblTranslated.MouseEnter += (_, _) => _autoCloseTimer.Stop();
        _lblTranslated.MouseLeave += (_, _) => _autoCloseTimer.Start();
        _lblOriginal.MouseEnter += (_, _) => _autoCloseTimer.Stop();
        _lblOriginal.MouseLeave += (_, _) => _autoCloseTimer.Start();
    }

    /// <summary>
    /// 번역 결과를 표시한다.
    /// </summary>
    public void ShowTranslation(string original, string translated, string providerName, int durationSeconds = 5)
    {
        _translatedText = translated;
        _lblOriginal.Text = original.Length > 80 ? original[..77] + "..." : original;
        _lblTranslated.Text = translated;
        _lblProvider.Text = providerName;

        // 텍스트 길이에 따라 폼 높이 조정
        using var g = CreateGraphics();
        var textSize = g.MeasureString(translated, _lblTranslated.Font, 368);
        var neededHeight = Math.Max(140, (int)textSize.Height + 90);
        Size = new Size(400, Math.Min(neededHeight, 300));
        _lblTranslated.Size = new Size(368, Size.Height - 84);

        // 마우스 커서 근처에 표시 (화면 범위 내)
        PositionNearCursor();

        _opacity = 1.0;
        Opacity = 1.0;
        _fadeTimer.Stop();

        _autoCloseTimer.Interval = durationSeconds * 1000;
        _autoCloseTimer.Start();

        Show();
    }

    public void ShowError(string message)
    {
        _translatedText = string.Empty;
        _lblOriginal.Text = string.Empty;
        _lblTranslated.Text = message;
        _lblTranslated.ForeColor = Color.FromArgb(255, 100, 100);
        _lblProvider.Text = "오류";

        PositionNearCursor();
        _opacity = 1.0;
        Opacity = 1.0;
        _autoCloseTimer.Interval = 3000;
        _autoCloseTimer.Start();
        Show();

        // 색상 복원
        _lblTranslated.ForeColor = Color.FromArgb(240, 240, 240);
    }

    private void PositionNearCursor()
    {
        var cursor = Cursor.Position;
        var screen = Screen.FromPoint(cursor).WorkingArea;

        int x = cursor.X + 20;
        int y = cursor.Y + 20;

        // 화면 벗어남 방지
        if (x + Width > screen.Right) x = cursor.X - Width - 10;
        if (y + Height > screen.Bottom) y = cursor.Y - Height - 10;
        if (x < screen.Left) x = screen.Left;
        if (y < screen.Top) y = screen.Top;

        Location = new Point(x, y);
    }

    private void CopyTranslation()
    {
        if (string.IsNullOrEmpty(_translatedText)) return;
        try
        {
            Clipboard.SetText(_translatedText);
            OnCopied?.Invoke();

            // 복사 완료 피드백
            _lblProvider.Text = "복사됨!";
            StartFadeOut();
        }
        catch { }
    }

    private void StartFadeOut()
    {
        _autoCloseTimer.Stop();
        _fadeTimer.Start();
    }

    private void FadeTimer_Tick(object? sender, EventArgs e)
    {
        _opacity -= 0.05;
        if (_opacity <= 0)
        {
            _fadeTimer.Stop();
            Hide();
            _opacity = 1.0;
        }
        else
        {
            Opacity = _opacity;
        }
    }

    // 둥근 모서리 적용
    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        var rect = ClientRectangle;
        using var path = GetRoundedRectPath(rect, 12);
        Region = new Region(path);

        // 테두리 그리기
        using var pen = new Pen(Color.FromArgb(60, 60, 60), 1);
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        e.Graphics.DrawPath(pen, path);
    }

    private static GraphicsPath GetRoundedRectPath(Rectangle rect, int radius)
    {
        var path = new GraphicsPath();
        var d = radius * 2;
        path.AddArc(rect.X, rect.Y, d, d, 180, 90);
        path.AddArc(rect.Right - d, rect.Y, d, d, 270, 90);
        path.AddArc(rect.Right - d, rect.Bottom - d, d, d, 0, 90);
        path.AddArc(rect.X, rect.Bottom - d, d, d, 90, 90);
        path.CloseFigure();
        return path;
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _fadeTimer.Dispose();
            _autoCloseTimer.Dispose();
        }
        base.Dispose(disposing);
    }
}
