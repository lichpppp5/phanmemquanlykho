using PMTapHoa.Desktop.Models;

namespace PMTapHoa.Desktop.Forms;

public class HealthCheckForm : Form
{
    private readonly AppServices _services;
    private readonly DataGridView _grid;

    public HealthCheckForm(AppServices services)
    {
        _services = services;

        Text = "Kiểm tra sức khỏe hệ thống";
        Width = 860;
        Height = 520;
        StartPosition = FormStartPosition.CenterParent;
        KeyPreview = true;

        var btnRun = new Button
        {
            Text = "Chạy kiểm tra",
            Width = 120,
            Location = new Point(20, 20)
        };
        btnRun.Click += (_, _) => RunCheck();
        Controls.Add(btnRun);

        _grid = new DataGridView
        {
            Location = new Point(20, 60),
            Width = 800,
            Height = 390,
            ReadOnly = true,
            AllowUserToAddRows = false,
            AutoGenerateColumns = false
        };
        _grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = nameof(HealthCheckItem.CheckName), HeaderText = "Hạng mục", Width = 200 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = nameof(HealthCheckItem.Status), HeaderText = "Trạng thái", Width = 100 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = nameof(HealthCheckItem.Message), HeaderText = "Thông điệp", Width = 470 });
        _grid.RowPrePaint += Grid_RowPrePaint;
        Controls.Add(_grid);

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
