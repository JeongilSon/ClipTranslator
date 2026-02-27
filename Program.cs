namespace ClipTranslator;

static class Program
{
    [STAThread]
    static void Main()
    {
        ApplicationConfiguration.Initialize();

        // 다중 인스턴스 방지
        using var mutex = new Mutex(true, "ClipTranslator_SingleInstance", out bool isNew);
        if (!isNew)
        {
            MessageBox.Show("ClipTranslator가 이미 실행 중입니다.", "알림",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        Application.Run(new Forms.MainForm());
    }
}
