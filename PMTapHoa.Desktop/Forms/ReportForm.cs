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
        Width = 620;
        Height = 400;
        StartPosition = FormStartPosition.CenterParent;
        KeyPreview = true;

        _lblRevenueToday = new Label
        {
            Text = "Doanh thu hôm nay: 0 VND",
            Font = new Font("Segoe UI", 15, FontStyle.Bold),
            AutoSize = true,
            Location = new Point(35, 70)
        };

        _lblOutstandingDebt = new Label
        {
            Text = "Tổng công nợ còn lại: 0 VND",
            Font = new Font("Segoe UI", 13, FontStyle.Bold),
            AutoSize = true,
            Location = new Point(35, 120)
        };

        var lblFrom = new Label
        {
            Text = "Từ ngày:",
            AutoSize = true,
            Location = new Point(35, 185)
        };
        _dtFrom = new DateTimePicker
        {
            Location = new Point(90, 181),
            Width = 130,
            Format = DateTimePickerFormat.Short,
            ShowCheckBox = true
        };

        var lblTo = new Label
        {
            Text = "Đến ngày:",
            AutoSize = true,
            Location = new Point(245, 185)
        };
        _dtTo = new DateTimePicker
        {
            Location = new Point(310, 181),
            Width = 130,
            Format = DateTimePickerFormat.Short,
            ShowCheckBox = true
        };

        var btnRefresh = new Button
        {
            Text = "Làm mới",
            Width = 120,
            Height = 40,
            Location = new Point(35, 240)
        };
        btnRefresh.Click += (_, _) => LoadRevenue();

        var btnExportSales = new Button
        {
            Text = "Xuất DS hóa đơn (Excel)",
            Width = 180,
            Height = 40,
            Location = new Point(170, 240)
        };
        btnExportSales.Click += (_, _) => ExportSales();

        var btnExportDebt = new Button
        {
            Text = "Xuất công nợ (Excel)",
            Width = 160,
            Height = 40,
            Location = new Point(365, 240)
        };
        btnExportDebt.Click += (_, _) => ExportDebts();

        Controls.Add(_lblRevenueToday);
        Controls.Add(_lblOutstandingDebt);
        Controls.Add(lblFrom);
        Controls.Add(_dtFrom);
        Controls.Add(lblTo);
        Controls.Add(_dtTo);
        Controls.Add(btnRefresh);
        Controls.Add(btnExportSales);
        Controls.Add(btnExportDebt);

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

            var from = _dtFrom.Checked ? _dtFrom.Value.Date : null;
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
