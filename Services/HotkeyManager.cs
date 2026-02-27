using System.Runtime.InteropServices;

namespace ClipTranslator.Services;

/// <summary>
/// 시스템 전역 단축키(Global Hotkey)를 등록/해제하는 관리자.
/// </summary>
public class HotkeyManager : NativeWindow, IDisposable
{
    private const int WM_HOTKEY = 0x0312;

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

    // 모디파이어 플래그
    private const uint MOD_ALT = 0x0001;
    private const uint MOD_CONTROL = 0x0002;
    private const uint MOD_SHIFT = 0x0004;
    private const uint MOD_NOREPEAT = 0x4000;

    /// <summary>
    /// 등록된 단축키가 눌렸을 때 발생. int 파라미터는 hotkeyId.
    /// </summary>
    public event EventHandler<int>? HotkeyPressed;

    private readonly Dictionary<int, string> _registeredHotkeys = new();
    private int _nextId = 1;
    private bool _disposed;

    // 미리 정의된 단축키 ID
    public const int HOTKEY_SEND_TRANSLATE = 1;   // 발신 번역 (Ctrl+Shift+T)
    public const int HOTKEY_TOGGLE = 2;            // 모니터링 토글 (Ctrl+Shift+Q)

    public HotkeyManager()
    {
        CreateHandle(new CreateParams());
    }

    /// <summary>
    /// "Ctrl+Shift+T" 형태의 문자열을 파싱하여 단축키를 등록한다.
    /// </summary>
    public bool RegisterHotkey(int id, string hotkeyString)
    {
        // 이미 등록된 ID가 있으면 먼저 해제
        UnregisterHotkey(id);

        if (!TryParseHotkey(hotkeyString, out uint modifiers, out uint vk))
            return false;

        modifiers |= MOD_NOREPEAT; // 키 반복 방지
        if (RegisterHotKey(Handle, id, modifiers, vk))
        {
            _registeredHotkeys[id] = hotkeyString;
            return true;
        }
        return false;
    }

    public void UnregisterHotkey(int id)
    {
        if (_registeredHotkeys.ContainsKey(id))
        {
            UnregisterHotKey(Handle, id);
            _registeredHotkeys.Remove(id);
        }
    }

    public void UnregisterAll()
    {
        foreach (var id in _registeredHotkeys.Keys.ToList())
        {
            UnregisterHotKey(Handle, id);
        }
        _registeredHotkeys.Clear();
    }

    protected override void WndProc(ref Message m)
    {
        if (m.Msg == WM_HOTKEY)
        {
            int id = m.WParam.ToInt32();
            HotkeyPressed?.Invoke(this, id);
        }
        base.WndProc(ref m);
    }

    /// <summary>
    /// "Ctrl+Shift+T" 같은 문자열을 모디파이어와 가상키 코드로 파싱한다.
    /// </summary>
    private static bool TryParseHotkey(string hotkeyString, out uint modifiers, out uint vk)
    {
        modifiers = 0;
        vk = 0;

        if (string.IsNullOrWhiteSpace(hotkeyString))
            return false;

        var parts = hotkeyString.Split('+', StringSplitOptions.TrimEntries);
        foreach (var part in parts)
        {
            switch (part.ToUpperInvariant())
            {
                case "CTRL":
                case "CONTROL":
                    modifiers |= MOD_CONTROL;
                    break;
                case "SHIFT":
                    modifiers |= MOD_SHIFT;
                    break;
                case "ALT":
                    modifiers |= MOD_ALT;
                    break;
                default:
                    // 마지막 파트를 가상 키 코드로 변환
                    if (part.Length == 1 && char.IsLetterOrDigit(part[0]))
                    {
                        vk = (uint)char.ToUpperInvariant(part[0]);
                    }
                    else if (Enum.TryParse<Keys>(part, true, out var key))
                    {
                        vk = (uint)key;
                    }
                    else
                    {
                        return false;
                    }
                    break;
            }
        }

        return vk != 0;
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        UnregisterAll();
        DestroyHandle();
        GC.SuppressFinalize(this);
    }
}
