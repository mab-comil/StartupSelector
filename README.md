# Startup Selector

Windows起動時に「どのアプリを起動するか」を選ぶ、GRUBブートメニューのような
アプリ選択ランチャーです。C# (.NET 8, WinForms) 製、外部ライブラリ不要。

## できること

- 起動すると、`config.toml` に書いた候補アプリの一覧がウィンドウで表示されます。
- ↑↓キーまたは Tab / Shift+Tab で選択し、Enter で決定 → 選択したアプリを起動して
  このセレクター自身は終了します。
- 何も選ばずに `timeout_seconds` 秒が経過すると、`default_app` で指定した
  アプリを自動的に起動して終了します。
- Esc キーで何も起動せずに終了できます（誤操作時の避難用）。
- `path` には `.exe` のフルパスだけでなく、スタートメニューの `.lnk`
  ショートカットのパスもそのまま指定できます。

## ビルド方法（Windows側で行います）

このプロジェクトは、Windows専用のGUIフレームワーク（WinForms）を使っているため、
Windows上でビルドする必要があります（今回コードを書いたLinux環境では、Windows
向けの参照アセンブリをネットワークポリシー上ダウンロードできず、実機ビルドの
確認ができていません。もしビルド時にエラーが出た場合はエラーメッセージを
教えてください、すぐ直します）。

### 方法A: .NET SDK のコマンドラインだけでビルド（推奨・軽量）

1. [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) をインストール
   （「.NET SDK x64」をダウンロード）。
2. このフォルダ一式をWindows機にコピーし、フォルダをコマンドプロンプトか
   PowerShellで開く。
3. 以下を実行（単一の実行ファイルとして、.NETランタイム同梱で出力）:

   ```
   dotnet publish -c Release -r win-x64 --self-contained true ^
     -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true ^
     -o publish
   ```

4. `publish\StartupSelector.exe` と `publish\config.toml`（初回起動時に
   `config.sample.toml` から自動生成されます）が出来上がります。

   もし手元のPCに.NET 8 デスクトップランタイムが既に入っているなら、代わりに
   `dotnet publish -c Release -r win-x64 --self-contained false -o publish`
   とすると、より軽量（数百KB〜数MB程度）な実行ファイルになります。

### 方法B: Visual Studio でビルド

1. Visual Studio 2022 Community をインストールし、ワークロードで
   「.NET デスクトップ開発」を選択。
2. `StartupSelector.csproj` をダブルクリックして開く。
3. 構成を `Release` にして「ビルド」→「発行」、またはそのまま `F5` で動作確認。

## 設置とスタートアップ登録

1. ビルドで生成された `StartupSelector.exe` と `config.toml` を、任意の
   フォルダにまとめて置く（例: `C:\Tools\StartupSelector\`）。
2. `config.toml` をメモ帳で開き、起動したいアプリの一覧・既定アプリ・
   タイムアウト秒数を編集する（書式は `config.sample.toml` と、ファイル内の
   コメントを参照）。
3. **今まで各アプリ側でWindows起動時の自動起動を有効にしていた場合は、
   それらは全て無効化してください**（タスクマネージャーの「スタートアップ」
   タブ、または各アプリの設定）。これをしないと、セレクターとは別に
   アプリが二重に起動してしまいます。
4. `Win + R` → `shell:startup` と入力してスタートアップフォルダを開く。
5. `StartupSelector.exe` の**ショートカット**を作成し、そのショートカットを
   スタートアップフォルダに置く（exe本体を直接移動する必要はありません）。

これで次回のWindows起動時（サインイン後）に、このセレクターが起動し、
選択メニューが表示されるようになります。

## config.toml の書式

```toml
title = "起動するアプリを選択してください"
timeout_seconds = 10
default_app = "Slack"

[[apps]]
name = "Slack"
path = 'C:\Users\hiro\AppData\Local\Programs\slack\slack.exe'
args = ""
```

- 文字列は `'シングルクォート'`（バックスラッシュをそのまま書ける。Windowsの
  パスにおすすめ）か `"ダブルクォート"`（`\\` や `\"` はエスケープとして解釈）
  のどちらでも書けます。
- `#` から行末まではコメットとして無視されます。
- `[[apps]]` を必要なだけ繰り返して候補を増やせます。

## 今後拡張できそうな点（希望があれば対応します）

- 実行ファイル自身にスタートアップフォルダへの登録・解除ボタンを付ける
- 設定編集用のGUI画面を追加する（現状はconfig.tomlを直接編集）
- キー操作でタイムアウトカウントダウンを一時停止/リセットする
- 各アプリのアイコンを`.lnk`からより正確に解決する
