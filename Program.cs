using System;
using System.IO;
using System.Windows.Forms;

namespace StartupSelector;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);

        var baseDir = AppContext.BaseDirectory;
        var configPath = Path.Combine(baseDir, "config.toml");
        var samplePath = Path.Combine(baseDir, "config.sample.toml");

        if (!File.Exists(configPath))
        {
            if (File.Exists(samplePath))
            {
                File.Copy(samplePath, configPath);
                MessageBox.Show(
                    "初回起動のため、サンプルの config.toml を作成しました。\n" +
                    $"次の場所を編集してから、もう一度起動してください:\n\n{configPath}",
                    "Startup Selector - 初期設定",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
            }
            else
            {
                MessageBox.Show(
                    $"設定ファイルが見つかりません:\n{configPath}\n\n" +
                    "exe と同じフォルダに config.toml を作成してください。",
                    "Startup Selector - エラー",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
            return;
        }

        SelectorConfig config;
        try
        {
            config = TomlLiteConfigReader.Load(configPath);
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"config.toml の読み込みに失敗しました。\n\n{ex.Message}",
                "Startup Selector - 設定エラー",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
            return;
        }

        Application.Run(new MainForm(config));
    }
}
