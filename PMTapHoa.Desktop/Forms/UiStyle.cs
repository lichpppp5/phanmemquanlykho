using PMTapHoa.Desktop.Services;

namespace PMTapHoa.Desktop.Forms;

public static class UiStyle
{
    private const int ButtonMinHeight = 40;
    private const int ButtonMinWidth = 96;
    private const float FhdCompactScale = 0.90f;
    public static bool FullScreenEnabled { get; set; }

    // ── Nền & Layout ────────────────────────────────────────────────────────────
    public static Color Background => Color.FromArgb(245, 247, 250);
    public static Color HeaderDark => Color.FromArgb(30, 39, 46);
    public static Color HeaderDarkAlt => Color.FromArgb(44, 62, 80);
    public static Color GridAltRow => Color.FromArgb(248, 249, 252);
    public static Color PanelSoft => Color.FromArgb(236, 240, 246);
    public static Color BorderMuted => Color.FromArgb(180, 190, 200);
    public static Color SidebarBg => Color.FromArgb(26, 33, 40);
    public static Color CardBg => Color.White;

    // ── Màu chủ đạo ─────────────────────────────────────────────────────────────
    public static Color Primary => Color.FromArgb(41, 128, 185);
    public static Color PrimaryDark => Color.FromArgb(21, 85, 130);
    public static Color Success => Color.FromArgb(34, 153, 84);
    public static Color SuccessBright => Color.FromArgb(39, 174, 96);
    public static Color Warning => Color.FromArgb(230, 126, 34);
    public static Color WarningLight => Color.FromArgb(248, 196, 113);
    public static Color Danger => Color.FromArgb(192, 57, 43);
    public static Color DangerLight => Color.FromArgb(250, 219, 216);
    public static Color Neutral => Color.FromArgb(108, 117, 125);

    // ── Accent Colors ────────────────────────────────────────────────────────────
    public static Color AccentPurple => Color.FromArgb(125, 60, 152);
    public static Color AccentTeal => Color.FromArgb(22, 160, 133);
    public static Color AccentOrange => Color.FromArgb(211, 84, 0);
    public static Color AccentBlueDark => Color.FromArgb(41, 128, 185);
    public static Color AccentSlate => Color.FromArgb(44, 62, 80);
    public static Color AccentYellow => Color.FromArgb(212, 172, 13);
    public static Color AccentCyan => Color.FromArgb(0, 180, 216);
    public static Color AccentIndigo => Color.FromArgb(72, 52, 212);

    // ── Màu trạng thái tồn kho ───────────────────────────────────────────────────
    public static Color StockOk => Color.FromArgb(212, 250, 229);
    public static Color StockLow => Color.FromArgb(255, 243, 205);
    public static Color StockEmpty => Color.FromArgb(255, 218, 218);
    public static Color StockOkText => Color.FromArgb(21, 87, 36);
    public static Color StockLowText => Color.FromArgb(133, 77, 14);
    public static Color StockEmptyText => Color.FromArgb(114, 28, 36);

    // ── Áp dụng style chính ──────────────────────────────────────────────────────
    public static void ApplyMainFormStyle(Form form, string title, Size minSize)
    {
        form.Text = title;
        form.MinimumSize = minSize;
        form.StartPosition = FormStartPosition.CenterScreen;
        form.WindowState = FullScreenEnabled ? FormWindowState.Maximized : FormWindowState.Normal;
        form.AutoScaleMode = AutoScaleMode.Font;
        form.Font = new Font("Segoe UI", 10F, FontStyle.Regular, GraphicsUnit.Point);
        form.KeyPreview = true;
        form.BackColor = Background;
        EnsureConsistentButtons(form);
    }

    public static void ApplyDialogStyle(Form form, string title, Size size, bool sizable = false)
    {
        form.Text = title;
        form.Size = size;
        form.StartPosition = FormStartPosition.CenterParent;
        form.FormBorderStyle = sizable ? FormBorderStyle.Sizable : FormBorderStyle.FixedDialog;
        form.MaximizeBox = sizable;
        form.MinimizeBox = false;
        form.AutoScaleMode = AutoScaleMode.Font;
        form.Font = new Font("Segoe UI", 10F, FontStyle.Regular, GraphicsUnit.Point);
        form.KeyPreview = true;
        form.BackColor = Background;
        EnsureConsistentButtons(form);
    }

    public static void ApplyWindowMode(Form form)
    {
        form.WindowState = FullScreenEnabled ? FormWindowState.Maximized : FormWindowState.Normal;
        EnsureConsistentButtons(form);
    }

    // ── DataGridView ─────────────────────────────────────────────────────────────
    public static void StyleGrid(DataGridView grid, bool fillLastColumn = false)
    {
        grid.ReadOnly = true;
        grid.AllowUserToAddRows = false;
        grid.AutoGenerateColumns = false;
        grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        grid.MultiSelect = false;
        grid.DefaultCellStyle.Font = new Font("Segoe UI", 10, FontStyle.Regular);
        grid.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 10, FontStyle.Bold);
        grid.ColumnHeadersDefaultCellStyle.BackColor = HeaderDark;
        grid.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
        grid.EnableHeadersVisualStyles = false;
        grid.ColumnHeadersHeight = 36;
        grid.RowTemplate.Height = 32;
        grid.AlternatingRowsDefaultCellStyle.BackColor = GridAltRow;
        grid.GridColor = Color.FromArgb(220, 225, 235);
        grid.BorderStyle = BorderStyle.None;
        grid.CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal;

        if (fillLastColumn && grid.Columns.Count > 0)
        {
            grid.Columns[grid.Columns.Count - 1].AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
        }
    }

    // ── Buttons ──────────────────────────────────────────────────────────────────
    public static void StyleButton(Button button, Color backColor, Color? foreColor = null)
    {
        button.BackColor = backColor;
        button.ForeColor = foreColor ?? Color.White;
        button.FlatStyle = FlatStyle.Flat;
        button.FlatAppearance.BorderSize = 0;
        button.FlatAppearance.MouseOverBackColor = ControlPaint.Light(backColor, 0.1f);
        button.FlatAppearance.MouseDownBackColor = ControlPaint.Dark(backColor, 0.1f);
        if (button.MinimumSize.Height < ButtonMinHeight)
        {
            button.MinimumSize = new Size(
                Math.Max(button.MinimumSize.Width, ButtonMinWidth),
                ButtonMinHeight);
        }
        if (button.Font.Size < 10F)
        {
            button.Font = new Font("Segoe UI", 10, FontStyle.Bold);
        }
        if (button.Padding == Padding.Empty)
        {
            button.Padding = new Padding(10, 0, 10, 0);
        }
    }

    /// <summary>Tạo panel card với màu sắc, icon emoji và giá trị số lớn.</summary>
    public static Panel CreateDashCard(string title, string icon, Color bgColor, out Label valueLabel, bool darkText = false)
    {
        var card = new Panel
        {
            Margin = new Padding(8),
            Dock = DockStyle.Fill,
            BackColor = bgColor,
            Padding = new Padding(16, 12, 16, 12)
        };

        // Bo góc bằng cách vẽ lại
        card.Paint += (_, e) =>
        {
            using var brush = new SolidBrush(bgColor);
            e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
        };

        var iconLabel = new Label
        {
            Text = icon,
            Font = new Font("Segoe UI Emoji", 18),
            AutoSize = true,
            Location = new Point(14, 12),
            BackColor = Color.Transparent,
            ForeColor = darkText ? Color.FromArgb(60, 60, 60) : Color.FromArgb(220, 240, 255)
        };
        card.Controls.Add(iconLabel);

        var titleLabel = new Label
        {
            Text = title,
            Font = new Font("Segoe UI", 9, FontStyle.Regular),
            AutoSize = true,
            Location = new Point(14, 46),
            BackColor = Color.Transparent,
            ForeColor = darkText ? Color.FromArgb(80, 80, 80) : Color.FromArgb(200, 225, 255)
        };
        card.Controls.Add(titleLabel);

        valueLabel = new Label
        {
            Text = "–",
            Font = new Font("Segoe UI", 15, FontStyle.Bold),
            AutoSize = false,
            Location = new Point(14, 68),
            Size = new Size(card.Width - 28, 36),
            BackColor = Color.Transparent,
            ForeColor = darkText ? Color.Black : Color.White,
            Anchor = AnchorStyles.Left | AnchorStyles.Bottom | AnchorStyles.Right
        };
        card.Controls.Add(valueLabel);
        return card;
    }

    // ── Tạo StatusBar ───────────────────────────────────────────────────────────
    public static Panel CreateStatusBar(out Label statusLabel)
    {
        var bar = new Panel
        {
            Dock = DockStyle.Bottom,
            Height = 28,
            BackColor = HeaderDark,
            Padding = new Padding(10, 4, 10, 0)
        };
        statusLabel = new Label
        {
            Dock = DockStyle.Fill,
            ForeColor = Color.FromArgb(160, 200, 230),
            Font = new Font("Segoe UI", 9, FontStyle.Regular),
            TextAlign = ContentAlignment.MiddleLeft
        };
        bar.Controls.Add(statusLabel);
        return bar;
    }

    // ── Private helpers ──────────────────────────────────────────────────────────
    private static void EnsureConsistentButtons(Form form)
    {
        form.Shown -= Form_ShownNormalizeButtons;
        form.Shown += Form_ShownNormalizeButtons;
    }

    private static void Form_ShownNormalizeButtons(object? sender, EventArgs e)
    {
        if (sender is not Form form)
        {
            return;
        }

        ApplyCompactForFhd(form);
        NormalizeButtonsRecursive(form);
    }

    private static void ApplyCompactForFhd(Control parent)
    {
        var screenWidth = Screen.PrimaryScreen?.WorkingArea.Width ?? 0;
        if (screenWidth < 1920)
        {
            return;
        }

        CompactControlsRecursive(parent);
    }

    private static void CompactControlsRecursive(Control parent)
    {
        if (parent is TableLayoutPanel table)
        {
            foreach (RowStyle row in table.RowStyles)
            {
                if (row.SizeType == SizeType.Absolute && row.Height >= 60f)
                {
                    row.Height = (float)Math.Round(row.Height * FhdCompactScale, 1);
                }
            }

            foreach (ColumnStyle col in table.ColumnStyles)
            {
                if (col.SizeType == SizeType.Absolute && col.Width >= 120f)
                {
                    col.Width = (float)Math.Round(col.Width * FhdCompactScale, 1);
                }
            }
        }

        foreach (Control child in parent.Controls)
        {
            if (child is Label label && label.Font.Size > 11.5f)
            {
                label.Font = new Font(label.Font.FontFamily, Math.Max(10f, label.Font.Size * FhdCompactScale), label.Font.Style);
            }
            else if (child is TextBox or ComboBox or NumericUpDown or DateTimePicker)
            {
                if (child.Font.Size > 11.5f)
                {
                    child.Font = new Font(child.Font.FontFamily, Math.Max(10f, child.Font.Size * FhdCompactScale), child.Font.Style);
                }
                if (child.MinimumSize.Height > 36)
                {
                    child.MinimumSize = new Size(child.MinimumSize.Width, 34);
                }
            }
            else if (child is DataGridView grid)
            {
                if (grid.ColumnHeadersHeight > 40)
                {
                    grid.ColumnHeadersHeight = 38;
                }

                if (grid.RowTemplate.Height > 36)
                {
                    grid.RowTemplate.Height = 34;
                }
            }

            if (child.HasChildren)
            {
                CompactControlsRecursive(child);
            }
        }
    }

    private static void NormalizeButtonsRecursive(Control parent)
    {
        foreach (Control child in parent.Controls)
        {
            if (child is Button button)
            {
                if (button.MinimumSize.Height < ButtonMinHeight)
                {
                    button.MinimumSize = new Size(
                        Math.Max(button.MinimumSize.Width, ButtonMinWidth),
                        ButtonMinHeight);
                }

                if (button.Height < ButtonMinHeight && button.Dock == DockStyle.None)
                {
                    button.Height = ButtonMinHeight;
                }

                if (button.Font.Size < 10F)
                {
                    button.Font = new Font("Segoe UI", 10, FontStyle.Bold);
                }

                if (button.Padding == Padding.Empty)
                {
                    button.Padding = new Padding(10, 0, 10, 0);
                }
            }

            if (child.HasChildren)
            {
                NormalizeButtonsRecursive(child);
            }
        }
    }
}
