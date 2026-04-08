using PMTapHoa.Desktop.Models;

namespace PMTapHoa.Desktop.Forms;

public class AuditLogForm : Form
{
    private readonly AppServices _services;
    private readonly DataGridView _grid;
    private readonly NumericUpDown _numLimit;

    public AuditLogForm(AppServices services)
    {
        _services = services;
        if (!_services.Session.IsAdmin)
        {
            throw new InvalidOperationException("Chỉ Admin mới được truy cập nhật ký thao tác.");
        }

        Text = "Nhật ký thao tác";
        Width = 1180;
        Height = 760;
        MinimumSize = new Size(980, 620);
        StartPosition = FormStartPosition.CenterParent;
        WindowState = FormWindowState.Maximized;
        AutoScaleMode = AutoScaleMode.Dpi;
        KeyPreview = true;
        BackColor = Color.WhiteSmoke;

        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 2,
            Padding = new Padding(14)
        };
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 56f));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
        Controls.Add(root);

        var topPanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = true,
            Padding = new Padding(4, 10, 4, 4)
        };
        root.Controls.Add(topPanel, 0, 0);

        topPanel.Controls.Add(new Label
        {
            Text = "Số dòng:",
            AutoSize = true,
            Margin = new Padding(0, 8, 8, 0)
        });

        _numLimit = new NumericUpDown
        {
            Width = 90,
            Minimum = 50,
            Maximum = 1000,
            Increment = 50,
            Value = 200,
            Margin = new Padding(0, 4, 12, 0)
        };
        topPanel.Controls.Add(_numLimit);

        var btnRefresh = new Button
        {
            Text = "Làm mới",
            Width = 100,
            Height = 34,
            BackColor = UiStyle.Primary,
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Margin = new Padding(0, 2, 0, 0)
        };
        btnRefresh.Click += (_, _) => LoadLogs();
        topPanel.Controls.Add(btnRefresh);

        _grid = new DataGridView
        {
            Dock = DockStyle.Fill,
            ReadOnly = true,
            AllowUserToAddRows = false,
            AutoGenerateColumns = false
        };
        _grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = nameof(AuditLog.LogTime), HeaderText = "Thời gian", Width = 180 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = nameof(AuditLog.Username), HeaderText = "User", Width = 140 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = nameof(AuditLog.Action), HeaderText = "Action", Width = 170 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = nameof(AuditLog.Detail), HeaderText = "Chi tiết", AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill });
        _grid.DefaultCellStyle.Font = new Font("Segoe UI", 10, FontStyle.Regular);
        _grid.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 10, FontStyle.Bold);
        _grid.ColumnHeadersHeight = 36;
        _grid.RowTemplate.Height = 32;
        _grid.AlternatingRowsDefaultCellStyle.BackColor = UiStyle.GridAltRow;
        root.Controls.Add(_grid, 0, 1);

        Load += (_, _) => LoadLogs();
        KeyDown += (_, e) =>
        {
            if (e.KeyCode == Keys.Escape)
            {
                Close();
            }
        };
    }

    private void LoadLogs()
    {
        var logs = _services.AuditService.GetRecent((int)_numLimit.Value);
        _grid.DataSource = null;
        _grid.DataSource = logs;
    }
}
