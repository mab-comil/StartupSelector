using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace StartupSelector;

public sealed class MainForm : Form
{
    private readonly SelectorConfig _config;
    private readonly ListBox _listBox;
    private readonly Label _titleLabel;
    private readonly Label _countdownLabel;
    private readonly System.Windows.Forms.Timer _timer;
    private readonly Dictionary<string, Icon?> _iconCache = new();
    private int _remainingSeconds;
    private bool _launched;

    private static readonly Color BgColor = Color.FromArgb(24, 24, 28);
    private static readonly Color PanelColor = Color.FromArgb(32, 32, 38);
    private static readonly Color TextColor = Color.FromArgb(235, 235, 240);
    private static readonly Color SubTextColor = Color.FromArgb(160, 160, 170);
    private static readonly Color AccentColor = Color.FromArgb(66, 133, 244);

    public MainForm(SelectorConfig config)
    {
        _config = config;
        _remainingSeconds = Math.Max(1, config.TimeoutSeconds);

        Text = "Startup Selector";
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        StartPosition = FormStartPosition.CenterScreen;
        BackColor = BgColor;
        ForeColor = TextColor;
        ClientSize = new Size(460, 120 + config.Apps.Count * 40 + 60);
        TopMost = true;
        KeyPreview = true;
        Font = new Font("Yu Gothic UI", 10.5f, FontStyle.Regular, GraphicsUnit.Point);

        _titleLabel = new Label
        {
            Text = config.Title,
            AutoSize = false,
            Dock = DockStyle.Top,
            Height = 48,
            TextAlign = ContentAlignment.MiddleLeft,
            Padding = new Padding(20, 0, 0, 0),
            ForeColor = TextColor,
            Font = new Font(Font.FontFamily, 12f, FontStyle.Bold)
        };

        _listBox = new ListBox
        {
            Dock = DockStyle.Fill,
            BackColor = PanelColor,
            ForeColor = TextColor,
            BorderStyle = BorderStyle.None,
            DrawMode = DrawMode.OwnerDrawFixed,
            ItemHeight = 40,
            IntegralHeight = false,
            Margin = new Padding(20, 0, 20, 0)
        };
        foreach (var app in config.Apps) _listBox.Items.Add(app);
        _listBox.DrawItem += ListBox_DrawItem;
        _listBox.KeyDown += ListBox_KeyDown;

        var defaultEntry = config.GetDefaultEntry();
        var defaultIndex = defaultEntry is null ? 0 : config.Apps.IndexOf(defaultEntry);
        _listBox.SelectedIndex = Math.Max(0, defaultIndex);

        var listPanel = new Panel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(20, 0, 20, 0),
            BackColor = BgColor
        };
        listPanel.Controls.Add(_listBox);

        _countdownLabel = new Label
        {
            Dock = DockStyle.Bottom,
            Height = 56,
            TextAlign = ContentAlignment.MiddleLeft,
            Padding = new Padding(20, 0, 0, 0),
            ForeColor = SubTextColor,
            Font = new Font(Font.FontFamily, 9.5f)
        };
        UpdateCountdownLabel();

        Controls.Add(listPanel);
        Controls.Add(_countdownLabel);
        Controls.Add(_titleLabel);

        _timer = new System.Windows.Forms.Timer { Interval = 1000 };
        _timer.Tick += Timer_Tick;

        Shown += (_, _) =>
        {
            _listBox.Focus();
            _timer.Start();
        };

        KeyDown += MainForm_KeyDown;
        FormClosing += (_, e) =>
        {
            // ×ボタンなどで閉じられたら何も起動せず終了（Escapeと同じ扱い）
            _timer.Stop();
        };
    }

    private void ListBox_DrawItem(object? sender, DrawItemEventArgs e)
    {
        if (e.Index < 0) return;
        var app = (AppEntry)_listBox.Items[e.Index];
        bool selected = (e.State & DrawItemState.Selected) == DrawItemState.Selected;
        bool isDefault = string.Equals(app.Name, _config.DefaultApp, StringComparison.OrdinalIgnoreCase);

        using var bg = new SolidBrush(selected ? AccentColor : PanelColor);
        e.Graphics.FillRectangle(bg, e.Bounds);

        var icon = GetIcon(app.Path);
        int textLeft = e.Bounds.Left + 16;
        if (icon != null)
        {
            var iconSize = 24;
            var iconY = e.Bounds.Top + (e.Bounds.Height - iconSize) / 2;
            e.Graphics.DrawIcon(icon, new Rectangle(e.Bounds.Left + 12, iconY, iconSize, iconSize));
            textLeft = e.Bounds.Left + 12 + iconSize + 12;
        }

        using var textBrush = new SolidBrush(selected ? Color.White : TextColor);
        var textRect = new RectangleF(textLeft, e.Bounds.Top, e.Bounds.Width - textLeft, e.Bounds.Height);
        using var sf = new StringFormat { LineAlignment = StringAlignment.Center };
        var label = isDefault ? $"{app.Name}（既定）" : app.Name;
        e.Graphics.DrawString(label, Font, textBrush, textRect, sf);

        if (selected)
        {
            using var pen = new Pen(Color.White, 1);
            e.Graphics.DrawRectangle(pen, e.Bounds.Left, e.Bounds.Top, e.Bounds.Width - 1, e.Bounds.Height - 1);
        }
    }

    private Icon? GetIcon(string path)
    {
        if (_iconCache.TryGetValue(path, out var cached)) return cached;
        Icon? icon = null;
        try
        {
            if (File.Exists(path))
                icon = Icon.ExtractAssociatedIcon(path);
        }
        catch
        {
            icon = null;
        }
        _iconCache[path] = icon;
        return icon;
    }

    private void ListBox_KeyDown(object? sender, KeyEventArgs e)
    {
        if (e.KeyCode == Keys.Enter)
        {
            e.Handled = true;
            e.SuppressKeyPress = true;
            ConfirmSelection();
        }
    }

    protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
    {
        // Tabは既定でフォーカス移動キーとして先に処理されてしまうため、
        // KeyDownイベントより前の段階（ProcessCmdKey）で奪う必要がある。
        if (keyData == Keys.Tab || keyData == (Keys.Tab | Keys.Shift))
        {
            MoveSelection(keyData.HasFlag(Keys.Shift) ? -1 : 1);
            return true;
        }
        return base.ProcessCmdKey(ref msg, keyData);
    }

    private void MainForm_KeyDown(object? sender, KeyEventArgs e)
    {
        switch (e.KeyCode)
        {
            case Keys.Enter:
                e.Handled = true;
                ConfirmSelection();
                break;
            case Keys.Escape:
                e.Handled = true;
                _timer.Stop();
                _launched = true; // 何も起動せず終了させるためのフラグ流用
                Close();
                break;
        }
    }

    private void MoveSelection(int delta)
    {
        if (_listBox.Items.Count == 0) return;
        int next = (_listBox.SelectedIndex + delta + _listBox.Items.Count) % _listBox.Items.Count;
        _listBox.SelectedIndex = next;
    }

    private void ConfirmSelection()
    {
        if (_listBox.SelectedItem is AppEntry app)
        {
            LaunchAndExit(app);
        }
    }

    private void Timer_Tick(object? sender, EventArgs e)
    {
        _remainingSeconds--;
        if (_remainingSeconds <= 0)
        {
            _timer.Stop();
            var def = _config.GetDefaultEntry();
            if (def != null) LaunchAndExit(def);
            else Close();
            return;
        }
        UpdateCountdownLabel();
    }

    private void UpdateCountdownLabel()
    {
        var def = _config.GetDefaultEntry();
        var name = def?.Name ?? "(未設定)";
        _countdownLabel.Text = $"{_remainingSeconds}秒後に既定のアプリ「{name}」を自動起動します　" +
                                "[↑↓/Tab: 選択] [Enter: 決定] [Esc: 何も起動せず終了]";
    }

    private void LaunchAndExit(AppEntry app)
    {
        if (_launched) return;
        _launched = true;
        _timer.Stop();

        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = app.Path,
                Arguments = app.Args ?? "",
                UseShellExecute = true,
            };
            if (!string.IsNullOrWhiteSpace(app.WorkingDirectory))
                psi.WorkingDirectory = app.WorkingDirectory;
            else
                psi.WorkingDirectory = Path.GetDirectoryName(app.Path) is { Length: > 0 } dir ? dir : "";

            Process.Start(psi);
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"「{app.Name}」の起動に失敗しました。\n\nパス: {app.Path}\n\nエラー: {ex.Message}",
                "Startup Selector",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
        finally
        {
            Close();
        }
    }
}
