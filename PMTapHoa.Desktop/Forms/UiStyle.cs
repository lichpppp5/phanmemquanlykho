using PMTapHoa.Desktop.Services;

namespace PMTapHoa.Desktop.Forms;

public static class UiStyle
{
    private const int ButtonMinHeight = 40;
    private const int ButtonMinWidth = 96;
    private const float FhdCompactScale = 0.90f;
    public static bool FullScreenEnabled { get; set; }

    public static Color Background => Color.WhiteSmoke;
    public static Color HeaderDark => Color.FromArgb(44, 62, 80);
    public static Color GridAltRow => Color.FromArgb(248, 249, 250);
    public static Color PanelSoft => Color.FromArgb(236, 240, 241);
    public static Color BorderMuted => Color.FromArgb(90, 90, 90);

    public static Color Primary => Color.FromArgb(52, 152, 219);
    public static Color Success => Color.FromArgb(39, 174, 96);
    public static Color SuccessBright => Color.FromArgb(46, 204, 113);
    public static Color Warning => Color.FromArgb(243, 156, 18);
    public static Color Danger => Color.FromArgb(192, 57, 43);
    public static Color Neutral => Color.FromArgb(127, 140, 141);
    public static Color AccentPurple => Color.FromArgb(142, 68, 173);
    public static Color AccentTeal => Color.FromArgb(22, 160, 133);
    public static Color AccentOrange => Color.FromArgb(230, 126, 34);
    public static Color AccentBlueDark => Color.FromArgb(41, 128, 185);
    public static Color AccentSlate => Color.FromArgb(52, 73, 94);
    public static Color AccentYellow => Color.FromArgb(241, 196, 15);

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

    public static void StyleGrid(DataGridView grid, bool fillLastColumn = false)
    {
        grid.ReadOnly = true;
        grid.AllowUserToAddRows = false;
        grid.AutoGenerateColumns = false;
        grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        grid.MultiSelect = false;
        grid.DefaultCellStyle.Font = new Font("Segoe UI", 10, FontStyle.Regular);
        grid.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 10, FontStyle.Bold);
        grid.ColumnHeadersHeight = 36;
        grid.RowTemplate.Height = 32;
        grid.AlternatingRowsDefaultCellStyle.BackColor = GridAltRow;

        if (fillLastColumn && grid.Columns.Count > 0)
        {
            grid.Columns[grid.Columns.Count - 1].AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
        }
    }

    public static void StyleButton(Button button, Color backColor, Color? foreColor = null)
    {
        button.BackColor = backColor;
        button.ForeColor = foreColor ?? Color.White;
        button.FlatStyle = FlatStyle.Flat;
        button.FlatAppearance.BorderSize = 0;
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
