using PMTapHoa.Desktop.Models;

namespace PMTapHoa.Desktop.Forms;

public class HealthCheckForm : Form
{
    private readonly AppServices _services;
    private readonly DataGridView _grid;

    public HealthCheckForm(AppServices services)
    {
        _services = services;

        UiStyle.ApplyMainFormStyle(this, "Kiểm tra sức khỏe hệ thống", new Size(1080, 700));
        Width = 1280;
        Height = 840;
        Font = new Font("Segoe UI", 11, FontStyle.Regular);

        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 2,
            Padding = new Padding(14)
        };
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 68f));
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

        var btnRun = new Button
        {
            Text = "Chạy kiểm tra",
            Width = 150,
            Height = 40,
            Font = new Font("Segoe UI", 10, FontStyle.Bold)
        };
        UiStyle.StyleButton(btnRun, UiStyle.Primary);
        btnRun.Click += (_, _) => RunCheck();
        topPanel.Controls.Add(btnRun);

        _grid = new DataGridView
        {
            Dock = DockStyle.Fill
        };
        _grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = nameof(HealthCheckItem.CheckName), HeaderText = "Hạng mục", Width = 200 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = nameof(HealthCheckItem.Status), HeaderText = "Trạng thái", Width = 100 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = nameof(HealthCheckItem.Message), HeaderText = "Thông điệp" });
        UiStyle.StyleGrid(_grid, fillLastColumn: true);
        _grid.DefaultCellStyle.Font = new Font("Segoe UI", 11, FontStyle.Regular);
        _grid.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 11, FontStyle.Bold);
        _grid.ColumnHeadersHeight = 40;
        _grid.RowTemplate.Height = 36;
        _grid.RowPrePaint += Grid_RowPrePaint;
        root.Controls.Add(_grid, 0, 1);

        Load += (_, _) => RunCheck();
        KeyDown += (_, e) =>
        {
            if (e.KeyCode == Keys.Escape)
            {
                Close();
            }
        };
    }

    private void RunCheck()
    {
        var checks = _services.HealthCheckService.RunAll(_services.Session.CurrentUser);
        _grid.DataSource = null;
        _grid.DataSource = checks;
    }

    private void Grid_RowPrePaint(object? sender, DataGridViewRowPrePaintEventArgs e)
    {
        if (e.RowIndex < 0 || e.RowIndex >= _grid.Rows.Count)
        {
            return;
        }

        var row = _grid.Rows[e.RowIndex];
        if (row.DataBoundItem is not HealthCheckItem item)
        {
            return;
        }

        if (string.Equals(item.Status, "ERROR", StringComparison.OrdinalIgnoreCase))
        {
            row.DefaultCellStyle.BackColor = Color.MistyRose;
            row.DefaultCellStyle.ForeColor = Color.DarkRed;
        }
        else if (string.Equals(item.Status, "WARN", StringComparison.OrdinalIgnoreCase))
        {
            row.DefaultCellStyle.BackColor = Color.LemonChiffon;
            row.DefaultCellStyle.ForeColor = Color.DarkGoldenrod;
        }
        else
        {
            row.DefaultCellStyle.BackColor = Color.Honeydew;
            row.DefaultCellStyle.ForeColor = Color.DarkGreen;
        }
    }
}
