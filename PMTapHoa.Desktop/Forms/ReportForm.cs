namespace PMTapHoa.Desktop.Forms;

public class ReportForm : Form
{
    private readonly AppServices _services;
    private readonly Label _lblRevenueToday;
    private readonly Label _lblOutstandingDebt;
    private readonly DateTimePicker _dtFrom;
    private readonly DateTimePicker _dtTo;

    public ReportForm(AppServices services)
    {
        _services = services;

        Text = "Báo cáo doanh thu";
        Width = 980;
        Height = 560;
        MinimumSize = new Size(900, 500);
        StartPosition = FormStartPosition.CenterParent;
        WindowState = FormWindowState.Maximized;
        AutoScroll = true;
        KeyPreview = true;
        BackColor = Color.WhiteSmoke;

        var title = new Label
        {
            Text = "BÁO CÁO DOANH THU",
            AutoSize = true,
            Font = new Font("Segoe UI", 20, FontStyle.Bold),
            Location = new Point(28, 20)
        };
        Controls.Add(title);

        var pnlRevenue = new Panel
        {
            Location = new Point(35, 80),
            Size = new Size(430, 110),
            BackColor = UiStyle.Success
        };
        _lblRevenueToday = new Label
        {
            Text = "Doanh thu hôm nay: 0 VND",
            Font = new Font("Segoe UI", 16, FontStyle.Bold),
            AutoSize = true,
            ForeColor = Color.White,
            Location = new Point(16, 36)
        };
        pnlRevenue.Controls.Add(_lblRevenueToday);
        Controls.Add(pnlRevenue);

        var pnlDebt = new Panel
        {
            Location = new Point(495, 80),
            Size = new Size(430, 110),
            BackColor = UiStyle.AccentOrange
        };
        _lblOutstandingDebt = new Label
        {
            Text = "Tổng công nợ còn lại: 0 VND",
            Font = new Font("Segoe UI", 16, FontStyle.Bold),
            AutoSize = true,
            ForeColor = Color.White,
            Location = new Point(16, 36)
        };
        pnlDebt.Controls.Add(_lblOutstandingDebt);
        Controls.Add(pnlDebt);

        var filterGroup = new GroupBox
        {
            Text = "Bộ lọc thời gian xuất báo cáo",
            Location = new Point(35, 215),
            Size = new Size(890, 90),
            Font = new Font("Segoe UI", 10, FontStyle.Bold)
        };
        Controls.Add(filterGroup);

        var lblFrom = new Label
        {
            Text = "Từ ngày:",
            AutoSize = true,
            Location = new Point(20, 42)
        };
        _dtFrom = new DateTimePicker
        {
            Location = new Point(80, 38),
            Width = 130,
            Format = DateTimePickerFormat.Short,
            ShowCheckBox = true
        };

        var lblTo = new Label
        {
            Text = "Đến ngày:",
            AutoSize = true,
            Location = new Point(240, 42)
        };
        _dtTo = new DateTimePicker
        {
            Location = new Point(305, 38),
            Width = 130,
            Format = DateTimePickerFormat.Short,
            ShowCheckBox = true
        };

        var btnRefresh = new Button
        {
            Text = "Làm mới",
            Width = 120,
            Height = 36,
            Location = new Point(470, 36),
            BackColor = UiStyle.Primary,
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat
        };
        btnRefresh.Click += (_, _) => LoadRevenue();

        var btnExportSales = new Button
        {
            Text = "Xuất DS hóa đơn (Excel)",
            Width = 170,
            Height = 36,
            Location = new Point(560, 36),
            BackColor = UiStyle.Success,
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat
        };
        btnExportSales.Click += (_, _) => ExportSales();

        var btnExportDebt = new Button
        {
            Text = "Xuất công nợ (Excel)",
            Width = 130,
            Height = 36,
            Location = new Point(740, 36),
            BackColor = UiStyle.AccentOrange,
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat
        };
        btnExportDebt.Click += (_, _) => ExportDebts();

        filterGroup.Controls.Add(lblFrom);
        filterGroup.Controls.Add(_dtFrom);
        filterGroup.Controls.Add(lblTo);
        filterGroup.Controls.Add(_dtTo);
        filterGroup.Controls.Add(btnRefresh);
        filterGroup.Controls.Add(btnExportSales);
        filterGroup.Controls.Add(btnExportDebt);

        Load += (_, _) => LoadRevenue();
        KeyDown += (_, e) =>
        {
            if (e.KeyCode == Keys.Escape)
            {
                Close();
            }
        };
    }

    private void LoadRevenue()
    {
        try
        {
            var value = _services.SalesService.GetTodayRevenue();
            var debt = _services.DebtService.GetTotalOutstanding();
            _lblRevenueToday.Text = $"Doanh thu hôm nay: {value:N0} VND";
            _lblOutstandingDebt.Text = $"Tổng công nợ còn lại: {debt:N0} VND";
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Không tải được báo cáo: {ex.Message}", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void ExportSales()
    {
        try
        {
            using var saveDialog = new SaveFileDialog
            {
                Filter = "Excel file (*.xlsx)|*.xlsx",
                FileName = $"sales_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx"
            };

            if (saveDialog.ShowDialog(this) != DialogResult.OK)
            {
                return;
            }

            DateTime? from = _dtFrom.Checked ? _dtFrom.Value.Date : null;
            DateTime? to = null;
            if (_dtTo.Checked)
            {
                to = _dtTo.Value.Date.AddDays(1).AddSeconds(-1);
            }

            var sales = _services.SalesService.GetSales(null, from, to);
            _services.ExportService.ExportSalesToExcel(saveDialog.FileName, sales);
            _services.AuditService.Log(_services.Session.CurrentUser?.Username, "EXPORT_SALES_EXCEL", saveDialog.FileName);
            MessageBox.Show("Đã xuất file Excel hóa đơn.", "Thành công", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Xuất file thất bại: {ex.Message}", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void ExportDebts()
    {
        try
        {
            using var saveDialog = new SaveFileDialog
            {
                Filter = "Excel file (*.xlsx)|*.xlsx",
                FileName = $"debt_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx"
            };

            if (saveDialog.ShowDialog(this) != DialogResult.OK)
            {
                return;
            }

            var debts = _services.DebtService.GetOutstandingDebts(null);
            _services.ExportService.ExportOutstandingDebtsToExcel(saveDialog.FileName, debts);
            _services.AuditService.Log(_services.Session.CurrentUser?.Username, "EXPORT_DEBTS_EXCEL", saveDialog.FileName);
            MessageBox.Show("Đã xuất file Excel công nợ.", "Thành công", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Xuất file thất bại: {ex.Message}", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }
}
