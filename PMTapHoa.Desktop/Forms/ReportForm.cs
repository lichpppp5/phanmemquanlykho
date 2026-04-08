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
        WindowState = FormWindowState.Normal;
        AutoScaleMode = AutoScaleMode.Font;
        KeyPreview = true;
        BackColor = Color.WhiteSmoke;
        UiStyle.ApplyWindowMode(this);

        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 3,
            Padding = new Padding(28, 20, 28, 20)
        };
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 210f));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 120f));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
        Controls.Add(root);

        var title = new Label
        {
            Text = "BÁO CÁO DOANH THU",
            Dock = DockStyle.Fill,
            Font = new Font("Segoe UI", 20, FontStyle.Bold),
            AutoSize = false,
            TextAlign = ContentAlignment.MiddleLeft
        };
        var metricsWrap = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 2
        };
        metricsWrap.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50f));
        metricsWrap.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50f));
        metricsWrap.RowStyles.Add(new RowStyle(SizeType.Absolute, 64f));
        metricsWrap.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
        metricsWrap.Controls.Add(title, 0, 0);
        metricsWrap.SetColumnSpan(title, 2);
        root.Controls.Add(metricsWrap, 0, 0);

        var pnlRevenue = new Panel
        {
            Dock = DockStyle.Fill,
            Margin = new Padding(6),
            BackColor = UiStyle.Success
        };
        _lblRevenueToday = new Label
        {
            Text = "Doanh thu hôm nay: 0 VND",
            Font = new Font("Segoe UI", 16, FontStyle.Bold),
            AutoSize = false,
            Dock = DockStyle.Fill,
            Padding = new Padding(16, 0, 16, 0),
            TextAlign = ContentAlignment.MiddleLeft,
            ForeColor = Color.White,
        };
        pnlRevenue.Controls.Add(_lblRevenueToday);
        metricsWrap.Controls.Add(pnlRevenue, 0, 1);

        var pnlDebt = new Panel
        {
            Dock = DockStyle.Fill,
            Margin = new Padding(6),
            BackColor = UiStyle.AccentOrange
        };
        _lblOutstandingDebt = new Label
        {
            Text = "Tổng công nợ còn lại: 0 VND",
            Font = new Font("Segoe UI", 16, FontStyle.Bold),
            AutoSize = false,
            Dock = DockStyle.Fill,
            Padding = new Padding(16, 0, 16, 0),
            TextAlign = ContentAlignment.MiddleLeft,
            ForeColor = Color.White,
        };
        pnlDebt.Controls.Add(_lblOutstandingDebt);
        metricsWrap.Controls.Add(pnlDebt, 1, 1);

        var filterGroup = new GroupBox
        {
            Text = "Bộ lọc thời gian xuất báo cáo",
            Dock = DockStyle.Fill,
            Font = new Font("Segoe UI", 10, FontStyle.Bold)
        };
        root.Controls.Add(filterGroup, 0, 1);

        var filterLayout = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = true,
            Padding = new Padding(10, 12, 10, 8)
        };
        filterGroup.Controls.Add(filterLayout);

        var lblFrom = new Label
        {
            Text = "Từ ngày:",
            AutoSize = true,
            Margin = new Padding(0, 10, 8, 0)
        };
        _dtFrom = new DateTimePicker
        {
            Width = 150,
            Format = DateTimePickerFormat.Short,
            ShowCheckBox = true,
            Margin = new Padding(0, 4, 14, 0)
        };

        var lblTo = new Label
        {
            Text = "Đến ngày:",
            AutoSize = true,
            Margin = new Padding(0, 10, 8, 0)
        };
        _dtTo = new DateTimePicker
        {
            Width = 150,
            Format = DateTimePickerFormat.Short,
            ShowCheckBox = true,
            Margin = new Padding(0, 4, 14, 0)
        };

        var btnRefresh = new Button
        {
            Text = "Làm mới",
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            MinimumSize = new Size(120, 38),
            Height = 36,
            Padding = new Padding(12, 0, 12, 0),
            BackColor = UiStyle.Primary,
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Margin = new Padding(0, 2, 10, 0)
        };
        btnRefresh.Click += (_, _) => LoadRevenue();

        var btnExportSales = new Button
        {
            Text = "Xuất DS hóa đơn (Excel)",
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            MinimumSize = new Size(220, 38),
            Height = 36,
            Padding = new Padding(12, 0, 12, 0),
            BackColor = UiStyle.Success,
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Margin = new Padding(0, 2, 10, 0)
        };
        btnExportSales.Click += (_, _) => ExportSales();

        var btnExportDebt = new Button
        {
            Text = "Xuất công nợ (Excel)",
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            MinimumSize = new Size(190, 38),
            Height = 36,
            Padding = new Padding(12, 0, 12, 0),
            BackColor = UiStyle.AccentOrange,
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Margin = new Padding(0, 2, 0, 0)
        };
        btnExportDebt.Click += (_, _) => ExportDebts();

        filterLayout.Controls.Add(lblFrom);
        filterLayout.Controls.Add(_dtFrom);
        filterLayout.Controls.Add(lblTo);
        filterLayout.Controls.Add(_dtTo);
        filterLayout.Controls.Add(btnRefresh);
        filterLayout.Controls.Add(btnExportSales);
        filterLayout.Controls.Add(btnExportDebt);

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
