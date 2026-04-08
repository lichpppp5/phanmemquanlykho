using PMTapHoa.Desktop.Services;

namespace PMTapHoa.Desktop.Forms;

public static class UiStyle
{
    private const int BaseButtonMinHeight = 40;
    private const int BaseButtonMinWidth = 96;
    private const float BaseFontSize = 10.5F;
    public static bool FullScreenEnabled { get; set; }
    public static float UiScaleFactor { get; private set; } = 1.0f;

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

    public static void ConfigureDisplay(AppConfigService configService)
    {
        var primary = Screen.PrimaryScreen?.WorkingArea;
        var autoScale = 1.0f;
        if (primary.HasValue)
        {
            // Keep UI balanced on common displays (HD/FHD/QHD).
            autoScale = primary.Value.Width switch
            {
                <= 1366 => 1.15f,
                <= 1600 => 1.10f,
                <= 1920 => 1.05f,
                _ => 1.00f
            };
        }

        UiScaleFactor = configService.UiScalePercent > 0
            ? Math.Clamp(configService.UiScalePercent / 100f, 0.90f, 1.40f)
            : autoScale;
    }

    public static void ApplyMainFormStyle(Form form, string title, Size minSize)
    {
        form.Text = title;
        form.MinimumSize = minSize;
        form.StartPosition = FormStartPosition.CenterScreen;
        form.WindowState = FullScreenEnabled ? FormWindowState.Maximized : FormWindowState.Normal;
        form.AutoScaleMode = AutoScaleMode.Font;
        form.Font = new Font("Segoe UI", BaseFontSize * UiScaleFactor, FontStyle.Regular, GraphicsUnit.Point);
        form.KeyPreview = true;
        form.BackColor = Background;
        EnsureConsistentLayout(form);
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
        form.Font = new Font("Segoe UI", BaseFontSize * UiScaleFactor, FontStyle.Regular, GraphicsUnit.Point);
        form.KeyPreview = true;
        form.BackColor = Background;
        EnsureConsistentLayout(form);
    }

    public static void ApplyWindowMode(Form form)
    {
        form.WindowState = FullScreenEnabled ? FormWindowState.Maximized : FormWindowState.Normal;
        EnsureConsistentLayout(form);
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
        var minHeight = (int)Math.Round(BaseButtonMinHeight * UiScaleFactor);
        var minWidth = (int)Math.Round(BaseButtonMinWidth * UiScaleFactor);
        if (button.MinimumSize.Height < minHeight)
        {
            button.MinimumSize = new Size(
                Math.Max(button.MinimumSize.Width, minWidth),
                minHeight);
        }
        var minFont = 10f * UiScaleFactor;
        if (button.Font.Size < minFont)
        {
            button.Font = new Font("Segoe UI", minFont, FontStyle.Bold);
        }
        if (button.Padding == Padding.Empty)
        {
            button.Padding = new Padding(10, 0, 10, 0);
        }
    }

    private static void EnsureConsistentLayout(Form form)
    {
        form.Shown -= Form_ShownNormalizeControls;
        form.Shown += Form_ShownNormalizeControls;
    }

    private static void Form_ShownNormalizeControls(object? sender, EventArgs e)
    {
        if (sender is not Form form)
        {
            return;
        }

        NormalizeControlsRecursive(form);
    }

    private static void NormalizeControlsRecursive(Control parent)
    {
        foreach (Control child in parent.Controls)
        {
            if (child is Button button)
            {
                var minHeight = (int)Math.Round(BaseButtonMinHeight * UiScaleFactor);
                var minWidth = (int)Math.Round(BaseButtonMinWidth * UiScaleFactor);
                if (button.MinimumSize.Height < minHeight)
                {
                    button.MinimumSize = new Size(
                        Math.Max(button.MinimumSize.Width, minWidth),
                        minHeight);
                }

                if (button.Height < minHeight && button.Dock == DockStyle.None)
                {
                    button.Height = minHeight;
                }

                var minFont = 10f * UiScaleFactor;
                if (button.Font.Size < minFont)
                {
                    button.Font = new Font("Segoe UI", minFont, FontStyle.Bold);
                }

                if (button.Padding == Padding.Empty)
                {
                    button.Padding = new Padding(10, 0, 10, 0);
                }
            }
            else if (child is TextBox or ComboBox or NumericUpDown or DateTimePicker)
            {
                var minFieldHeight = (int)Math.Round(34 * UiScaleFactor);
                if (child.MinimumSize.Height < minFieldHeight)
                {
                    child.MinimumSize = new Size(child.MinimumSize.Width, minFieldHeight);
                }
                var minFont = 10.5f * UiScaleFactor;
                if (child.Font.Size < minFont)
                {
                    child.Font = new Font("Segoe UI", minFont, child.Font.Style);
                }
            }
            else if (child is Label label)
            {
                var minFont = 10.5f * UiScaleFactor;
                if (label.Font.Size < minFont)
                {
                    label.Font = new Font("Segoe UI", minFont, label.Font.Style);
                }
            }
            else if (child is DataGridView grid)
            {
                grid.ColumnHeadersHeight = Math.Max(grid.ColumnHeadersHeight, (int)Math.Round(38 * UiScaleFactor));
                grid.RowTemplate.Height = Math.Max(grid.RowTemplate.Height, (int)Math.Round(34 * UiScaleFactor));
                var cellFont = 10.5f * UiScaleFactor;
                if (grid.DefaultCellStyle.Font == null || grid.DefaultCellStyle.Font.Size < cellFont)
                {
                    grid.DefaultCellStyle.Font = new Font("Segoe UI", cellFont, FontStyle.Regular);
                }
                if (grid.ColumnHeadersDefaultCellStyle.Font == null || grid.ColumnHeadersDefaultCellStyle.Font.Size < cellFont)
                {
                    grid.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", cellFont, FontStyle.Bold);
                }
            }

            if (child.HasChildren)
            {
                NormalizeControlsRecursive(child);
            }
        }
    }

}
