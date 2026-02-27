using System.Text.Json;
using System.Text.Json.Serialization;

namespace ClipTranslator.Models;

public enum ApiProviderType
{
    Claude,
    OpenAI,
    Gemini
}

public class AppSettings
{
    public ApiProviderType SelectedProvider { get; set; } = ApiProviderType.Claude;

    public string ClaudeApiKey { get; set; } = string.Empty;
    public string OpenAIApiKey { get; set; } = string.Empty;
    public string GeminiApiKey { get; set; } = string.Empty;

    public string ClaudeModel { get; set; } = "claude-sonnet-4-5-20250929";
    public string OpenAIModel { get; set; } = "gpt-4o-mini";
    public string GeminiModel { get; set; } = "gemini-2.0-flash";

    /// <summary>
    /// 한국어 입력 시 번역할 기본 타겟 언어 (예: "English", "Japanese", "Chinese")
    /// </summary>
    public string DefaultTargetLanguage { get; set; } = "English";

    /// <summary>
    /// 클립보드 모니터링 활성화 여부
    /// </summary>
    public bool IsMonitoringEnabled { get; set; } = true;

    /// <summary>
    /// 플로팅 팝업 자동 닫힘 시간 (초)
    /// </summary>
    public int PopupDurationSeconds { get; set; } = 5;

    /// <summary>
    /// 발신 번역 단축키 (기본: Ctrl+Shift+T)
    /// </summary>
    public string SendHotkey { get; set; } = "Ctrl+Shift+T";

    /// <summary>
    /// 모니터링 토글 단축키 (기본: Ctrl+Shift+Q)
    /// </summary>
    public string ToggleHotkey { get; set; } = "Ctrl+Shift+Q";

    private static readonly string SettingsDir = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "ClipTranslator");

    private static readonly string SettingsPath = Path.Combine(SettingsDir, "appsettings.json");

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() }
    };

    public static AppSettings Load()
    {
        try
        {
            if (File.Exists(SettingsPath))
            {
                var json = File.ReadAllText(SettingsPath);
                return JsonSerializer.Deserialize<AppSettings>(json, JsonOptions) ?? new AppSettings();
            }
        }
        catch
        {
            // 설정 파일 손상 시 기본값 사용
        }
        return new AppSettings();
    }

    public void Save()
    {
        try
        {
            Directory.CreateDirectory(SettingsDir);
            var json = JsonSerializer.Serialize(this, JsonOptions);
            File.WriteAllText(SettingsPath, json);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"설정 저장 실패: {ex.Message}");
        }
    }
}
