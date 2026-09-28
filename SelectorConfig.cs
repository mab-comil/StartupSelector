using System;
using System.Collections.Generic;
using System.Linq;

namespace StartupSelector;

/// <summary>
/// config.toml の内容全体。
/// </summary>
public sealed class SelectorConfig
{
    /// <summary>何も選択しなかった場合に自動起動するまでの待ち時間（秒）。</summary>
    public int TimeoutSeconds { get; set; } = 10;

    /// <summary>タイムアウト時に起動するアプリの名前（Apps の Name のいずれかと一致させる）。</summary>
    public string DefaultApp { get; set; } = "";

    /// <summary>選択画面の見出し文言（省略可）。</summary>
    public string Title { get; set; } = "起動するアプリを選択してください";

    /// <summary>選択肢のアプリ一覧。</summary>
    public List<AppEntry> Apps { get; set; } = new();

    /// <summary>DefaultApp に対応する AppEntry を返す。見つからなければ先頭のエントリ。</summary>
    public AppEntry? GetDefaultEntry()
    {
        if (Apps.Count == 0) return null;
        var found = Apps.FirstOrDefault(a =>
            string.Equals(a.Name, DefaultApp, StringComparison.OrdinalIgnoreCase));
        return found ?? Apps[0];
    }
}
