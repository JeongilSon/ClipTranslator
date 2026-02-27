using System.Runtime.InteropServices;

namespace ClipTranslator.Services;

/// <summary>
/// Win32 API를 이용한 클립보드 변경 감지기.
/// NativeWindow를 상속받아 WM_CLIPBOARDUPDATE 메시지를 수신한다.
/// </summary>
public class ClipboardMonitor : NativeWindow, IDisposable
{
    private const int WM_CLIPBOARDUPDATE = 0x031D;

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool AddClipboardFormatListener(IntPtr hwnd);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool RemoveClipboardFormatListener(IntPtr hwnd);

    /// <summary>
    /// 클립보드에 새로운 텍스트가 감지되었을 때 발생하는 이벤트.
    /// </summary>
    public event EventHandler<string>? ClipboardTextChanged;

    private bool _isSelfChange;
    private bool _isListening;
    private bool _disposed;

    public ClipboardMonitor()
    {
        CreateHandle(new CreateParams());
    }

    public void StartListening()
    {
        if (_isListening) return;
        if (AddClipboardFormatListener(Handle))
            _isListening = true;
    }

    public void StopListening()
    {
        if (!_isListening) return;
        RemoveClipboardFormatListener(Handle);
        _isListening = false;
    }

    /// <summary>
    /// 프로그램 자체에서 클립보드를 변경할 때 호출하여 자기 이벤트를 무시하도록 한다.
    /// </summary>
    public void SetClipboardWithoutNotify(string text)
    {
        _isSelfChange = true;
        try
        {
            Clipboard.SetText(text);
        }
        finally
        {
            // 약간의 딜레이 후 플래그 해제 (WM_CLIPBOARDUPDATE가 비동기로 올 수 있음)
            Task.Delay(100).ContinueWith(_ => _isSelfChange = false);
        }
    }

    protected override void WndProc(ref Message m)
    {
        if (m.Msg == WM_CLIPBOARDUPDATE)
        {
            if (!_isSelfChange)
            {
                OnClipboardUpdate();
            }
        }
        base.WndProc(ref m);
    }

    private void OnClipboardUpdate()
    {
        try
        {
            if (Clipboard.ContainsText())
            {
                var text = Clipboard.GetText().Trim();
                if (!string.IsNullOrEmpty(text))
                {
                    ClipboardTextChanged?.Invoke(this, text);
                }
            }
        }
        catch (ExternalException)
        {
            // 클립보드가 다른 프로세스에 의해 잠겨있는 경우 무시
        }
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        StopListening();
        DestroyHandle();
        GC.SuppressFinalize(this);
    }
}
