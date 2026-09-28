using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;

namespace StartupSelector;

/// <summary>
/// TOML のうち、このアプリの設定ファイルに必要な範囲だけをサポートする軽量パーサー。
/// （NuGet の外部ライブラリに依存せず、単体exeで完結させるための自前実装）
///
/// サポートする書式:
///   # コメント
///   timeout_seconds = 10
///   default_app = "Slack"
///
///   [[apps]]
///   name = "Slack"
///   path = 'C:\Users\me\AppData\Local\Programs\slack\slack.exe'
///   args = ""
///
/// 文字列は "二重引用符"（\\ や \" をエスケープとして解釈）と
/// '単一引用符'（TOMLのリテラル文字列＝バックスラッシュをそのまま扱う。Windowsのパスに便利）
/// の両方に対応しています。
/// </summary>
public static class TomlLiteConfigReader
{
    public static SelectorConfig Load(string path)
    {
        var lines = File.ReadAllLines(path, Encoding.UTF8);
        var config = new SelectorConfig();
        AppEntry? current = null;

        for (int lineNo = 0; lineNo < lines.Length; lineNo++)
        {
            var raw = lines[lineNo];
            var line = StripComment(raw).Trim();
            if (line.Length == 0) continue;

            if (line.StartsWith("[[") && line.EndsWith("]]"))
            {
                var tableName = line.Substring(2, line.Length - 4).Trim();
                if (!string.Equals(tableName, "apps", StringComparison.OrdinalIgnoreCase))
                {
                    throw new FormatException(
                        $"{lineNo + 1}行目: 未対応のテーブル [[{tableName}]] です。[[apps]] のみ対応しています。");
                }
                current = new AppEntry();
                config.Apps.Add(current);
                continue;
            }

            var eq = line.IndexOf('=');
            if (eq < 0)
            {
                throw new FormatException($"{lineNo + 1}行目を解釈できません: \"{raw}\"");
            }

            var key = line.Substring(0, eq).Trim();
            var rawValue = line.Substring(eq + 1).Trim();
            var value = ParseValue(rawValue, lineNo + 1);

            if (current is null)
            {
                // トップレベル（[[apps]] より前）の設定
                switch (key)
                {
                    case "timeout_seconds":
                        config.TimeoutSeconds = ToInt(value, key, lineNo + 1);
                        break;
                    case "default_app":
                        config.DefaultApp = value;
                        break;
                    case "title":
                        config.Title = value;
                        break;
                    default:
                        // 未知のキーは将来の拡張用に無視する
                        break;
                }
            }
            else
            {
                switch (key)
                {
                    case "name":
                        current.Name = value;
                        break;
                    case "path":
                        current.Path = value;
                        break;
                    case "args":
                        current.Args = value;
                        break;
                    case "working_directory":
                        current.WorkingDirectory = value;
                        break;
                    default:
                        break;
                }
            }
        }

        Validate(config);
        return config;
    }

    private static void Validate(SelectorConfig config)
    {
        if (config.Apps.Count == 0)
            throw new FormatException("設定ファイルに [[apps]] が1つもありません。");

        for (int i = 0; i < config.Apps.Count; i++)
        {
            var app = config.Apps[i];
            if (string.IsNullOrWhiteSpace(app.Name))
                throw new FormatException($"{i + 1}番目の [[apps]] に name がありません。");
            if (string.IsNullOrWhiteSpace(app.Path))
                throw new FormatException($"[[apps]] \"{app.Name}\" に path がありません。");
        }

        // default_app が未指定、または [[apps]] のどの name とも一致しない場合は
        // 先頭のアプリを既定にする（表示と実際の動作を一致させるためここで確定させる）
        bool matches = config.Apps.Any(a =>
            string.Equals(a.Name, config.DefaultApp, StringComparison.OrdinalIgnoreCase));
        if (!matches)
        {
            config.DefaultApp = config.Apps[0].Name;
        }
    }

    private static int ToInt(string value, string key, int lineNo)
    {
        if (int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var n))
            return n;
        throw new FormatException($"{lineNo}行目: {key} は数値である必要があります（値: \"{value}\"）。");
    }

    private static string StripComment(string line)
    {
        bool inSingle = false, inDouble = false;
        for (int i = 0; i < line.Length; i++)
        {
            var c = line[i];
            if (c == '\'' && !inDouble) inSingle = !inSingle;
            else if (c == '"' && !inSingle) inDouble = !inDouble;
            else if (c == '#' && !inSingle && !inDouble)
                return line.Substring(0, i);
        }
        return line;
    }

    private static string ParseValue(string raw, int lineNo)
    {
        if (raw.Length >= 2 && raw[0] == '\'' && raw[^1] == '\'')
        {
            // リテラル文字列: エスケープ処理なし（Windowsパスに便利）
            return raw.Substring(1, raw.Length - 2);
        }

        if (raw.Length >= 2 && raw[0] == '"' && raw[^1] == '"')
        {
            var inner = raw.Substring(1, raw.Length - 2);
            var sb = new StringBuilder();
            for (int i = 0; i < inner.Length; i++)
            {
                if (inner[i] == '\\' && i + 1 < inner.Length)
                {
                    i++;
                    sb.Append(inner[i] switch
                    {
                        'n' => "\n",
                        't' => "\t",
                        '"' => "\"",
                        '\\' => "\\",
                        _ => inner[i].ToString()
                    });
                }
                else
                {
                    sb.Append(inner[i]);
                }
            }
            return sb.ToString();
        }

        // 引用符なし（数値・真偽値・多少ゆるい生の値もそのまま許容）
        return raw.Trim();
    }
}
