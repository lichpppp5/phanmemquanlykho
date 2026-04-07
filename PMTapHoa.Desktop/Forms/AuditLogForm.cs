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
        if (!_services.Session.IsManager)
        {
            throw new InvalidOperationException("Chỉ quản lý mới được truy cập nhật ký thao tác.");
        }

        Text = "Nhật ký thao tác";
        Width = 980;
        Height = 620;
        StartPosition = FormStartPosition.CenterParent;
        KeyPreview = true;

        Controls.Add(new Label
        {
            Text = "Số dòng:",
            AutoSize = true,
            Location = new Point(20, 20)
        });

        _numLimit = new NumericUpDown
        {
            Location = new Point(75, 16),
            Width = 90,
            Minimum = 50,
            Maximum = 1000,
            Increment = 50,
            Value = 200
        };
        Controls.Add(_numLimit);

        var btnRefresh = new Button
        {
            Text = "Làm mới",
            Width = 100,
            Location = new Point(185, 15)
        };
        btnRefresh.Click += (_, _) => LoadLogs();
        Controls.Add(btnRefresh);

        _grid = new DataGridView
        {
            Location = new Point(20, 55),
            Width = 920,
            Height = 510,
            ReadOnly = true,
            AllowUserToAddRows = false,
            AutoGenerateColumns = false
        };
        _grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = nameof(AuditLog.LogTime), HeaderText = "Thời gian", Width = 180 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = nameof(AuditLog.Username), HeaderText = "User", Width = 140 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = nameof(AuditLog.Action), HeaderText = "Action", Width = 170 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = nameof(AuditLog.Detail), HeaderText = "Chi tiết", Width = 390 });
        Controls.Add(_grid);

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
