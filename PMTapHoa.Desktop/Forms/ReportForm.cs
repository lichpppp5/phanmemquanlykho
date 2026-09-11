namespace PMTapHoa.Desktop.Forms;

public class ReportForm : Form
{
    private readonly AppServices _services;
    private readonly Label _lblRevenueToday;
    private readonly Label _lblProfitToday;
    private readonly Label _lblOutstandingDebt;
    private readonly Label _lblTopSummary;
    private readonly DateTimePicker _dtFrom;
    private readonly DateTimePicker _dtTo;
    private readonly DataGridView _gridTop;

    public ReportForm(AppServices services)
    {
        _services = services;

        Text = "Báo cáo doanh thu & lợi nhuận";
        Width = 1100;
        Height = 740;
        MinimumSize = new Size(960, 640);
        StartPosition = FormStartPosition.CenterParent;
        WindowState = FormWindowState.Normal;
        AutoScaleMode = AutoScaleMode.Font;
        KeyPreview = true;
        BackColor = UiStyle.Background;
        UiStyle.ApplyWindowMode(this);

        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 4,
            Padding = new Padding(24, 18, 24, 18)
        };
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 54f));  // Title
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 120f)); // KPI summary
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 80f));  // Filter
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));  // Top table
        Controls.Add(root);

        // Title
        var title = new Label
        {
            Text = "📈  BÁO CÁO DOANH THU & LỢI NHUẬN",
            Dock = DockStyle.Fill,
            Font = new Font("Segoe UI", 18, FontStyle.Bold),
            ForeColor = UiStyle.HeaderDark,
            AutoSize = false,
            TextAlign = ContentAlignment.MiddleLeft
        };
        root.Controls.Add(title, 0, 0);

        // KPI cards
        var kpiGrid = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 3,
            RowCount = 1
        };
        kpiGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.33f));
        kpiGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.33f));
        kpiGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.34f));

        kpiGrid.Controls.Add(UiStyle.CreateDashCard("Doanh thu hôm nay", "💵", UiStyle.Success, out _lblRevenueToday), 0, 0);
        kpiGrid.Controls.Add(UiStyle.CreateDashCard("Lãi gộp hôm nay", "📈", UiStyle.Primary, out _lblProfitToday), 1, 0);
        kpiGrid.Controls.Add(UiStyle.CreateDashCard("Tổng công nợ", "💳", UiStyle.AccentOrange, out _lblOutstandingDebt), 2, 0);
        root.Controls.Add(kpiGrid, 0, 1);

        // Filter
        var filterGroup = new GroupBox
        {
            Text = "🗓️  Bộ lọc & xuất báo cáo",
            Dock = DockStyle.Fill,
            Font = new Font("Segoe UI", 10, FontStyle.Bold),
            ForeColor = UiStyle.Primary
        };
        root.Controls.Add(filterGroup, 0, 2);

        var filterLayout = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = true,
            Padding = new Padding(8, 6, 8, 4)
        };
        filterGroup.Controls.Add(filterLayout);

        filterLayout.Controls.Add(new Label { Text = "Từ ngày:", AutoSize = true, Margin = new Padding(0, 10, 8, 0) });
        _dtFrom = new DateTimePicker { Width = 148, Format = DateTimePickerFormat.Short, ShowCheckBox = true, Margin = new Padding(0, 4, 16, 0) };
        filterLayout.Controls.Add(_dtFrom);

        filterLayout.Controls.Add(new Label { Text = "Đến ngày:", AutoSize = true, Margin = new Padding(0, 10, 8, 0) });
        _dtTo = new DateTimePicker { Width = 148, Format = DateTimePickerFormat.Short, ShowCheckBox = true, Margin = new Padding(0, 4, 16, 0) };
        filterLayout.Controls.Add(_dtTo);

        Button MakeBtn(string text, Color color, int minW = 160) => new Button
        {
            Text = text, AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink,
            MinimumSize = new Size(minW, 36), Padding = new Padding(12, 0, 12, 0),
            BackColor = color, ForeColor = Color.White, FlatStyle = FlatStyle.Flat,
            Margin = new Padding(0, 2, 8, 0), Font = new Font("Segoe UI", 9.5f, FontStyle.Bold)
        };


        var btnLoad = MakeBtn("🔄  Nạp báo cáo", UiStyle.Primary, 160);
        btnLoad.FlatAppearance.BorderSize = 0;
        btnLoad.Click += (_, _) => LoadPeriodReport();
        filterLayout.Controls.Add(btnLoad);

        var btnExportSales = MakeBtn("📥  Xuất HĐ (Excel)", UiStyle.Success, 180);
        btnExportSales.FlatAppearance.BorderSize = 0;
        btnExportSales.Click += (_, _) => ExportSales();
        filterLayout.Controls.Add(btnExportSales);

        var btnExportProfit = MakeBtn("📊  Xuất lợi nhuận (Excel)", UiStyle.AccentPurple, 220);
        btnExportProfit.FlatAppearance.BorderSize = 0;
        btnExportProfit.Click += (_, _) => ExportProfitReport();
        filterLayout.Controls.Add(btnExportProfit);

        var btnExportInventory = MakeBtn("📦  Xuất tồn kho (Excel)", UiStyle.AccentTeal, 210);
        btnExportInventory.FlatAppearance.BorderSize = 0;
        btnExportInventory.Click += (_, _) => ExportInventory();
        filterLayout.Controls.Add(btnExportInventory);

        var btnExportDebt = MakeBtn("💰  Xuất công nợ (Excel)", UiStyle.AccentOrange, 200);
        btnExportDebt.FlatAppearance.BorderSize = 0;
        btnExportDebt.Click += (_, _) => ExportDebts();
        filterLayout.Controls.Add(btnExportDebt);

        // Top products table
        var topGroup = new GroupBox
        {
            Text = "  🏅 Top 10 sản phẩm bán chạy theo kỳ đã chọn",
            Dock = DockStyle.Fill,
            Font = new Font("Segoe UI", 10, FontStyle.Bold),
            Margin = new Padding(0, 4, 0, 0),
            ForeColor = UiStyle.AccentPurple
        };
        root.Controls.Add(topGroup, 0, 3);

        _lblTopSummary = new Label
        {
            Text = "Doanh thu: –    |    Giá vốn: –    |    Lợi nhuận: –",
            Font = new Font("Segoe UI", 11, FontStyle.Bold),
            ForeColor = UiStyle.Success,
            AutoSize = true,
            Padding = new Padding(8, 4, 0, 2)
        };

        _gridTop = new DataGridView
        {
            Dock = DockStyle.Fill,
            ReadOnly = true,
            AllowUserToAddRows = false,
            AutoGenerateColumns = false,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            RowHeadersVisible = false
        };
        _gridTop.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "#", Name = "Rank", Width = 42, DefaultCellStyle = { Alignment = DataGridViewContentAlignment.MiddleCenter } });
        _gridTop.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Tên sản phẩm", Name = "ProductName", AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill, MinimumWidth = 250 });
        _gridTop.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Số lượng bán", Name = "TotalQty", Width = 130, DefaultCellStyle = { Alignment = DataGridViewContentAlignment.MiddleRight } });
        _gridTop.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Doanh thu", Name = "Revenue", Width = 150, DefaultCellStyle = { Alignment = DataGridViewContentAlignment.MiddleRight } });
        UiStyle.StyleGrid(_gridTop);

        var topContent = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 2
        };
        topContent.RowStyles.Add(new RowStyle(SizeType.Absolute, 32f));
        topContent.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
        topContent.Controls.Add(_lblTopSummary, 0, 0);
        topContent.Controls.Add(_gridTop, 0, 1);
        topGroup.Controls.Add(topContent);

        Load += (_, _) => LoadRevenue();
        KeyDown += (_, e) => { if (e.KeyCode == Keys.Escape) Close(); };
    }

    private void LoadRevenue()
    {
        try
        {
            var summary = _services.SalesService.GetTodayProfitSummary();
            var debt = _services.DebtService.GetTotalOutstanding();
            _lblRevenueToday.Text = $"{summary.RevenueAmount:N0}\nVND";
            _lblProfitToday.Text = $"{summary.ProfitAmount:N0}\nVND";
            _lblOutstandingDebt.Text = $"{debt:N0}\nVND";
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Không tải được báo cáo: {ex.Message}", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void LoadPeriodReport()
    {
        try
        {
            DateTime? from = _dtFrom.Checked ? _dtFrom.Value.Date : null;
            DateTime? to = _dtTo.Checked ? _dtTo.Value.Date.AddDays(1).AddSeconds(-1) : null;

            var (revenue, capital, profit) = _services.SalesService.GetProfitReport(from, to);
            var topSelling = _services.SalesService.GetTopSelling(from, to, 10);

            _lblTopSummary.Text = $"Doanh thu: {revenue:N0} VND    |    Giá vốn: {capital:N0} VND    |    Lợi nhuận: {profit:N0} VND";
            _lblTopSummary.ForeColor = profit >= 0 ? UiStyle.Success : UiStyle.Danger;

            _gridTop.Rows.Clear();
            for (var i = 0; i < topSelling.Count; i++)
            {
                var (name, qty, rev) = topSelling[i];
                _gridTop.Rows.Add((i + 1).ToString(), name, $"{qty:N0}", $"{rev:N0} VND");
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Lỗi tải báo cáo: {ex.Message}", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void ExportSales()
    {
        try
        {
            using var dlg = new SaveFileDialog { Filter = "Excel (*.xlsx)|*.xlsx", FileName = $"hoadon_{DateTime.Now:yyyyMMdd}.xlsx" };
            if (dlg.ShowDialog(this) != DialogResult.OK) return;
            DateTime? from = _dtFrom.Checked ? _dtFrom.Value.Date : null;
            DateTime? to = _dtTo.Checked ? _dtTo.Value.Date.AddDays(1).AddSeconds(-1) : null;
            var sales = _services.SalesService.GetSales(null, from, to);
            _services.ExportService.ExportSalesToExcel(dlg.FileName, sales);
            _services.AuditService.Log(_services.Session.CurrentUser?.Username, "EXPORT_SALES_EXCEL", dlg.FileName);
            MessageBox.Show("Đã xuất danh sách hóa đơn ra Excel.", "Thành công", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception ex) { MessageBox.Show($"Xuất file thất bại: {ex.Message}", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error); }
    }

    private void ExportProfitReport()
    {
        try
        {
            using var dlg = new SaveFileDialog { Filter = "Excel (*.xlsx)|*.xlsx", FileName = $"loinhuan_{DateTime.Now:yyyyMMdd}.xlsx" };
            if (dlg.ShowDialog(this) != DialogResult.OK) return;
            DateTime? from = _dtFrom.Checked ? _dtFrom.Value.Date : null;
            DateTime? to = _dtTo.Checked ? _dtTo.Value.Date.AddDays(1).AddSeconds(-1) : null;
            var (revenue, capital, profit) = _services.SalesService.GetProfitReport(from, to);
            var top = _services.SalesService.GetTopSelling(from, to, 20);
            _services.ExportService.ExportProfitReportToExcel(dlg.FileName, revenue, capital, profit, from, to, top);
            _services.AuditService.Log(_services.Session.CurrentUser?.Username, "EXPORT_PROFIT_EXCEL", dlg.FileName);
            MessageBox.Show("Đã xuất báo cáo lợi nhuận ra Excel.", "Thành công", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception ex) { MessageBox.Show($"Xuất file thất bại: {ex.Message}", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error); }
    }

    private void ExportInventory()
    {
        try
        {
            using var dlg = new SaveFileDialog { Filter = "Excel (*.xlsx)|*.xlsx", FileName = $"tonkho_{DateTime.Now:yyyyMMdd}.xlsx" };
            if (dlg.ShowDialog(this) != DialogResult.OK) return;
            var products = _services.ProductService.GetAll();
            _services.ExportService.ExportInventoryToExcel(dlg.FileName, products);
            _services.AuditService.Log(_services.Session.CurrentUser?.Username, "EXPORT_INVENTORY_EXCEL", dlg.FileName);
            MessageBox.Show("Đã xuất tồn kho ra Excel.", "Thành công", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception ex) { MessageBox.Show($"Xuất file thất bại: {ex.Message}", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error); }
    }

    private void ExportDebts()
    {
        try
        {
            using var dlg = new SaveFileDialog { Filter = "Excel (*.xlsx)|*.xlsx", FileName = $"conno_{DateTime.Now:yyyyMMdd}.xlsx" };
            if (dlg.ShowDialog(this) != DialogResult.OK) return;
            var debts = _services.DebtService.GetOutstandingDebts(null);
            _services.ExportService.ExportOutstandingDebtsToExcel(dlg.FileName, debts);
            _services.AuditService.Log(_services.Session.CurrentUser?.Username, "EXPORT_DEBTS_EXCEL", dlg.FileName);
            MessageBox.Show("Đã xuất công nợ ra Excel.", "Thành công", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception ex) { MessageBox.Show($"Xuất file thất bại: {ex.Message}", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error); }
    }
}
