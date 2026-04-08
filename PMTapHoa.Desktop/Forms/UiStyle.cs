using PMTapHoa.Desktop.Services;

namespace PMTapHoa.Desktop.Forms;

public static class UiStyle
{
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
    }

    public static void ApplyWindowMode(Form form)
    {
        form.WindowState = FullScreenEnabled ? FormWindowState.Maximized : FormWindowState.Normal;
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
    }
}
