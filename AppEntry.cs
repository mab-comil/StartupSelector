namespace StartupSelector;

/// <summary>
/// 選択メニューに表示する1つのアプリの情報。
/// </summary>
public sealed class AppEntry
{
    /// <summary>メニューに表示する名前。</summary>
    public string Name { get; set; } = "";

    /// <summary>
    /// 起動対象。exe のフルパスでも、.lnk ショートカットのフルパスでも構いません。
    /// スタートメニューのショートカットをそのまま指定できます。
    /// </summary>
    public string Path { get; set; } = "";

    /// <summary>起動時に渡す追加の引数（不要なら空のままでOK）。</summary>
    public string Args { get; set; } = "";

    /// <summary>作業ディレクトリ（省略可）。</summary>
    public string? WorkingDirectory { get; set; }

    public override string ToString() => Name;
}
