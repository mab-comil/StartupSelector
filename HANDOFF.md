# 引き継ぎドキュメント: Startup Selector（Windows起動時アプリ選択メニュー）

このドキュメントは、クラウド上のClaude（Linux環境）からWindows機のClaude Codeへ
作業を引き継ぐためのものです。ここに書かれた内容をそのままコンテキストとして
渡せば、経緯を再説明しなくても作業を継続できるはずです。

## 1. これは何か

Windows起動時にGRUBブートメニューのような「アプリ選択画面」を出し、
キーボード操作で選んだアプリを起動する常駐しないランチャーアプリです。

- 起動すると `config.toml` に列挙したアプリの一覧をウィンドウ表示する
- ↑↓キー または Tab / Shift+Tab で選択、Enterで決定 → 選んだアプリを起動して
  このセレクター自身は終了する
- 何も選ばず `timeout_seconds` 秒が経過したら、`default_app` で指定した
  アプリを自動的に起動して終了する
- Escキーで、何も起動せずに終了できる（避難用）
- Windowsのスタートアップフォルダにはこのセレクターだけを登録し、
  各アプリ自体の自動起動は無効化しておく想定

## 2. 現在の状態（重要・2026-09-19時点で更新）

- 言語/技術: C# / .NET 8 / WinForms。外部NuGetパッケージは一切使用していない
  （TOML設定ファイルの読み込みも自前の軽量パーサーで実装済み）。
- **Windows機で `.NET 8 SDK`（8.0.425、winget導入）を用いてビルド・実機動作確認済み。**
  `dotnet build` はエラー0件・警告0件で成功。`dotnet publish -c Release -r win-x64
  --self-contained false -o publish` でフレームワーク依存の発行も完了している。
  以前クラウド環境でコンパイルできなかった経緯（NuGet接続不可）はこの時点で解消済み。
- 実機での動作確認は「5. 動作確認チェックリスト」の通りすべて完了。唯一
  「Tab / Shift+Tabキーでの選択」に不具合があり、修正済み（詳細は6節）。
- 高DPI環境・マルチモニター環境での見た目は**未検証のまま**。運用予定PCが
  本機とは別のため、そちらで後日確認する方針（ユーザー判断で今回は割愛）。

## 3. このマシンでの環境構築（.NET SDKが未インストール）

### 3-1. .NET 8 SDK のインストール

PowerShellで以下を実行（winget が使える場合）:

```powershell
winget install Microsoft.DotNet.SDK.8
```

winget が使えない、またはエラーになる場合は、ブラウザで
https://dotnet.microsoft.com/download/dotnet/8.0 を開き、
「.NET SDK 8.0.x — Windows x64」のインストーラーをダウンロードして実行する。

### 3-2. インストール確認

新しいPowerShell/コマンドプロンプトを開き直してから:

```powershell
dotnet --version
```

`8.0.x` のようなバージョンが表示されればOK。

## 4. プロジェクトの配置とビルド

1. 同梱の `StartupSelector-src.zip`（もしくはこのフォルダ一式）を、
   任意の作業フォルダに展開する。例: `C:\dev\StartupSelector`
2. そのフォルダで:

   ```powershell
   cd C:\dev\StartupSelector
   dotnet build
   ```

   まずはこれでコンパイルが通るか確認する。**エラーが出た場合はここで
   修正する**（後述の「5. 既知のリスク・要確認点」も参照）。

3. ビルドが通ったら、配布用に単一の実行ファイルとして発行する:

   ```powershell
   dotnet publish -c Release -r win-x64 --self-contained true `
     -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true `
     -o publish
   ```

   `.NET 8 デスクトップランタイム` が既にマシンに入っているなら、より軽量な
   フレームワーク依存ビルドでもよい:

   ```powershell
   dotnet publish -c Release -r win-x64 --self-contained false -o publish
   ```

4. `publish\StartupSelector.exe` が生成される。

## 5. 動作確認チェックリスト（2026-09-19 実機確認結果）

`publish` フォルダで `StartupSelector.exe` を手動でダブルクリックして確認。

- [x] 初回起動時: `config.toml` が無い状態で起動すると、`config.sample.toml`
  から `config.toml` が自動生成され、その旨のメッセージが出て終了すること。
- [x] `config.toml` を実際のアプリパスに書き換えてから再起動すると、
  アプリ一覧のウィンドウが表示されること。
- [x] ↑↓キーで選択が動くこと。
- [x] Tab / Shift+Tab でも選択が循環すること。
  → 初回確認時は**動作せず**、原因を修正済み（6節参照）。修正後は確認OK。
- [x] Enterキーで、選択中のアプリが実際に起動し、セレクター自身のウィンドウが
  閉じること。
- [x] 何も操作せず `timeout_seconds` 秒待つと、`default_app` に指定したアプリが
  自動起動してセレクターが終了すること。
- [x] Escキーで、何も起動せずにウィンドウが閉じること。
- [x] `config.toml` の中身をわざと壊す（例: `path` を削除する）と、
  分かりやすいエラーメッセージが出ること。
- [x] `path` に `.exe` を指定した場合と `.lnk`（スタートメニューのショートカット）
  を指定した場合の両方で、正しくアプリが起動すること。
- [x] 一覧の各アプリの左側にアイコンが表示されること（`.exe` / `.lnk` の
  両方で確認済み）。
- [ ] 高DPI環境でのレイアウト崩れ — **未確認**（本機は1台のみのため割愛。
  運用予定PCで後日確認）。
- [ ] マルチモニター環境での `CenterScreen` の挙動 — **未確認**（同上）。

## 6. 既知のリスク・要確認点

### 6-1. 修正済み: Tabキーで選択できない不具合

- **症状**: 実機確認で、↑↓キーでの選択は動くがTab / Shift+Tabキーでは
  選択が動かなかった。
- **原因**: WinFormsではTabキーは既定でフォーカス移動用の「ダイアログキー」
  として扱われ、`KeyPreview` + `Form.KeyDown` の `e.Handled = true` だけでは
  確実に奪えない。このフォームには選択可能なコントロールが `ListBox` しか
  無いため、Tabを押しても同じコントロールに留まったまま何も起きないように
  見えていた。
- **修正**: `MainForm.cs` に `ProcessCmdKey` のオーバーライドを追加し、
  Tab / Shift+Tab をより早い段階で確実に横取りするように変更（`KeyDown` 側の
  Tab処理は削除）。修正後、実機で正常動作を確認済み。

### 6-2. 未検証のまま残っている点

- 高DPI環境でのレイアウト崩れ（`ClientSize` を固定値で計算している）。
- マルチモニター環境での `CenterScreen` の挙動。
- 上記2点は運用予定PCが本機と異なるため今回は検証を割愛。運用PCで実際に
  スタートアップ導入する際に確認する。

### 6-3. 確認済み（当初「未確認」だった点）

- フォント `"Yu Gothic UI"` 指定は問題なく表示された。
- `Icon.ExtractAssociatedIcon` は `.exe` / `.lnk` の両方で正しくアイコンを
  取得できることを確認済み。

## 7. スタートアップへの導入手順

**未実施（2026-09-19時点）**: 運用予定PCが開発・検証を行った本機とは別のため、
以下の手順は運用PC上で実施する。`publish` フォルダの内容と
`config.sample.toml` を運用PCに持っていけばそのまま導入できる状態になっている。

1. `publish\StartupSelector.exe` と、編集済みの `config.toml` を、
   常設のフォルダにコピーする（例: `C:\Tools\StartupSelector\`）。
2. **今まで各アプリ（Slack, Discord など）側で有効にしていたWindows起動時の
   自動起動を、すべて無効化する**（タスクマネージャーの「スタートアップ」
   タブ、または各アプリの設定画面）。これをしないと、セレクター経由の起動と
   二重に起動してしまう。
3. `Win + R` → `shell:startup` でスタートアップフォルダを開く。
4. `StartupSelector.exe` の**ショートカット**を作成し、それをスタートアップ
   フォルダに置く。
5. サインアウト→サインインし直して、実際にWindows起動時の動作を確認する。

## 8. プロジェクト構成

| ファイル | 役割 |
|---|---|
| `StartupSelector.csproj` | プロジェクト定義。`net8.0-windows` / WinForms。外部NuGet依存なし |
| `Program.cs` | エントリポイント。config.toml の読み込み、初回起動時のサンプル生成 |
| `MainForm.cs` | 選択画面のUI本体（ListBox描画、キー操作、タイムアウト処理、アプリ起動） |
| `SelectorConfig.cs` | 設定全体のモデル（timeout_seconds, default_app, apps一覧など） |
| `AppEntry.cs` | 1アプリ分の情報（name, path, args, working_directory） |
| `TomlLiteConfigReader.cs` | 自前実装のTOML風パーサー（NuGetのTomlynが使えなかったための代替） |
| `config.sample.toml` | 設定サンプル。初回起動時に `config.toml` としてコピーされる |
| `README.md` | ビルド手順・導入手順（本ドキュメントと内容は近いが独立して読める） |

## 9. 今後の拡張候補（要望があれば対応）

- exe自身にスタートアップフォルダへの登録／解除ボタンを付ける
- `config.toml` を直接編集する代わりの設定GUI画面
- キー操作でタイムアウトのカウントダウンを一時停止・リセットする
- `.lnk` のリンク先を `IShellLink` 経由で解決し、より正確にアイコン・
  作業ディレクトリを取得する
- ダークテーマ以外の配色・フォントサイズの調整

## 10. 設計判断の理由（参考）

- **WinForms を選んだ理由**: 起動時に毎回一瞬で表示される必要があるため、
  Electron等の重量級フレームワークは不向き。WinFormsはネイティブに近い
  起動速度で、キー操作やプロセス起動などWindows APIとの親和性も高い。
- **TOML形式を選んだ理由**: JSONよりメモ帳での手編集がしやすく、YAMLより
  インデント起因のミスが起きにくい。Windowsパスをシングルクォートの
  リテラル文字列でエスケープなしに書けるのも実用上便利。
- **外部NuGetパッケージを使わなかった理由**: 開発環境（クラウドのLinux
  サンドボックス）から `nuget.org` に到達できず、パッケージ復元ができなかった
  ため、代わりに設定ファイルの読み込み部分を自前実装した。Windows機では
  NuGetが問題なく使えるはずなので、もし本格的なTOMLライブラリ（例:
  [Tomlyn](https://github.com/xoofx/Tomlyn)）に切り替えたい場合は
  `TomlLiteConfigReader.cs` を置き換えるだけで対応できる構成にしてある。
