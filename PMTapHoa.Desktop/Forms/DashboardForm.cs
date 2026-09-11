namespace PMTapHoa.Desktop.Forms;

public class DashboardForm : Form
{
    private readonly AppServices _services;

    // KPI Cards
    private Label _lblTodayRevenue = null!;
    private Label _lblMonthRevenue = null!;
    private Label _lblTodayOrders = null!;
    private Label _lblDebtOutstanding = null!;
    private Label _lblLowStock = null!;
    private Label _lblNearExpiry = null!;
    private Label _lblTodayProfit = null!;
    private Label _lblMonthProfit = null!;

    // Chart & tables
    private Panel _chartPanel = null!;
    private List<(DateTime Date, decimal Revenue)> _chartData = [];
    private DataGridView _gridTopSelling = null!;
    private DataGridView _gridLowStock = null!;

    private readonly System.Windows.Forms.Timer _refreshTimer;

    public DashboardForm(AppServices services)
    {
        _services = services;

        Text = "Dashboard Tổng Quan";
        Width = 1360;
        Height = 860;
        MinimumSize = new Size(1100, 720);
        StartPosition = FormStartPosition.CenterParent;
        WindowState = FormWindowState.Normal;
        AutoScaleMode = AutoScaleMode.Font;
        KeyPreview = true;
        BackColor = UiStyle.Background;
        UiStyle.ApplyWindowMode(this);

        BuildLayout();

        _refreshTimer = new System.Windows.Forms.Timer { Interval = 300_000 }; // 5 phút
        _refreshTimer.Tick += (_, _) => LoadData();
        _refreshTimer.Start();

        Load += (_, _) => LoadData();
        KeyDown += (_, e) => { if (e.KeyCode == Keys.Escape) Close(); };
    }

    private void BuildLayout()
    {
        // ── Header ────────────────────────────────────────────────────────────
        var header = new Panel { Dock = DockStyle.Top, Height = 62, BackColor = UiStyle.HeaderDark };
        var lblTitle = new Label
        {
            Text = "📊  DASHBOARD TỔNG QUAN",
            ForeColor = Color.White,
            Font = new Font("Segoe UI", 18, FontStyle.Bold),
            Dock = DockStyle.Left,
            AutoSize = true,
            TextAlign = ContentAlignment.MiddleLeft,
            Padding = new Padding(22, 0, 0, 0)
        };
        var btnRefresh = new Button
        {
            Text = "🔄  Làm mới",
            Width = 130, Height = 38,
            BackColor = UiStyle.Primary, ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI", 10, FontStyle.Bold),
            Anchor = AnchorStyles.Right | AnchorStyles.Top,
            Location = new Point(header.Width - 160, 12)
        };
        btnRefresh.FlatAppearance.BorderSize = 0;
        btnRefresh.Click += (_, _) => LoadData();
        btnRefresh.Anchor = AnchorStyles.Right | AnchorStyles.Top;
        header.SizeChanged += (_, _) => btnRefresh.Left = header.Width - 155;
        header.Controls.Add(lblTitle);
        header.Controls.Add(btnRefresh);

        // ── Root layout ───────────────────────────────────────────────────────
        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 3,
            Padding = new Padding(16, 12, 16, 12)
        };
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 110f));  // KPI cards
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 55f));    // Chart + top selling
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 45f));    // Low stock table

        // ── Row 0: KPI Cards ─────────────────────────────────────────────────
        var kpiPanel = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 4,
            RowCount = 2,
            Padding = new Padding(0)
        };
        for (var i = 0; i < 4; i++)
            kpiPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25f));
        kpiPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 50f));
        kpiPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 50f));

        kpiPanel.Controls.Add(UiStyle.CreateDashCard("Doanh thu hôm nay", "💵", UiStyle.Success, out _lblTodayRevenue), 0, 0);
        kpiPanel.Controls.Add(UiStyle.CreateDashCard("Doanh thu tháng", "📅", UiStyle.Primary, out _lblMonthRevenue), 1, 0);
        kpiPanel.Controls.Add(UiStyle.CreateDashCard("Số đơn hôm nay", "🛒", UiStyle.AccentTeal, out _lblTodayOrders), 2, 0);
        kpiPanel.Controls.Add(UiStyle.CreateDashCard("Tổng công nợ", "💳", UiStyle.AccentOrange, out _lblDebtOutstanding), 3, 0);
        kpiPanel.Controls.Add(UiStyle.CreateDashCard("Lãi hôm nay", "📈", UiStyle.SuccessBright, out _lblTodayProfit), 0, 1);
        kpiPanel.Controls.Add(UiStyle.CreateDashCard("Lãi tháng", "🏆", UiStyle.AccentIndigo, out _lblMonthProfit), 1, 1);
        kpiPanel.Controls.Add(UiStyle.CreateDashCard("Hàng sắp hết kho", "⚠️", UiStyle.Warning, out _lblLowStock), 2, 1);
        kpiPanel.Controls.Add(UiStyle.CreateDashCard("Hàng gần hết hạn", "📅", UiStyle.Danger, out _lblNearExpiry), 3, 1);

        root.Controls.Add(kpiPanel, 0, 0);

        // ── Row 1: Chart + Top Selling ────────────────────────────────────────
        var midPanel = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 1,
            Padding = new Padding(0)
        };
        midPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 60f));
        midPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 40f));

        // Chart panel
        var chartGroup = new GroupBox
        {
            Text = "  Doanh thu 7 ngày gần nhất",
            Dock = DockStyle.Fill,
            Font = new Font("Segoe UI", 10, FontStyle.Bold),
            Margin = new Padding(0, 6, 6, 0),
            ForeColor = UiStyle.AccentBlueDark
        };
        _chartPanel = new Panel { Dock = DockStyle.Fill, BackColor = Color.White };
        _chartPanel.Paint += DrawRevenueChart;
        chartGroup.Controls.Add(_chartPanel);
        midPanel.Controls.Add(chartGroup, 0, 0);

        // Top selling
        var topGroup = new GroupBox
        {
            Text = "  🏅 Top 5 bán chạy hôm nay",
            Dock = DockStyle.Fill,
            Font = new Font("Segoe UI", 10, FontStyle.Bold),
            Margin = new Padding(6, 6, 0, 0),
            ForeColor = UiStyle.AccentPurple
        };
        _gridTopSelling = new DataGridView
        {
            Dock = DockStyle.Fill,
            ReadOnly = true,
            AllowUserToAddRows = false,
            AutoGenerateColumns = false,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            RowHeadersVisible = false,
            BorderStyle = BorderStyle.None
        };
        _gridTopSelling.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Tên sản phẩm", Name = "ProductName", AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill, MinimumWidth = 140 });
        _gridTopSelling.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "SL bán", Name = "TotalQty", Width = 65, DefaultCellStyle = { Alignment = DataGridViewContentAlignment.MiddleCenter } });
        _gridTopSelling.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Doanh thu", Name = "Revenue", Width = 100, DefaultCellStyle = { Alignment = DataGridViewContentAlignment.MiddleRight } });
        UiStyle.StyleGrid(_gridTopSelling, fillLastColumn: false);
        topGroup.Controls.Add(_gridTopSelling);
        midPanel.Controls.Add(topGroup, 1, 0);
        root.Controls.Add(midPanel, 0, 1);

        // ── Row 2: Low stock table ────────────────────────────────────────────
        var lowStockGroup = new GroupBox
        {
            Text = "  ⚠️ Danh sách hàng sắp hết kho (cần nhập thêm)",
            Dock = DockStyle.Fill,
            Font = new Font("Segoe UI", 10, FontStyle.Bold),
            Margin = new Padding(0, 6, 0, 0),
            ForeColor = UiStyle.Warning
        };
        _gridLowStock = new DataGridView
        {
            Dock = DockStyle.Fill,
            ReadOnly = true,
            AllowUserToAddRows = false,
            AutoGenerateColumns = false,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            RowHeadersVisible = false,
            BorderStyle = BorderStyle.None
        };
        _gridLowStock.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Tên sản phẩm", Name = "ProductName", AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill, MinimumWidth = 200 });
        _gridLowStock.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Danh mục", Name = "Category", Width = 130 });
        _gridLowStock.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Tồn kho", Name = "Stock", Width = 85, DefaultCellStyle = { Alignment = DataGridViewContentAlignment.MiddleCenter } });
        _gridLowStock.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Tối thiểu", Name = "MinStock", Width = 85, DefaultCellStyle = { Alignment = DataGridViewContentAlignment.MiddleCenter } });
        _gridLowStock.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Giá nhập", Name = "CostPrice", Width = 110, DefaultCellStyle = { Alignment = DataGridViewContentAlignment.MiddleRight } });
        _gridLowStock.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Hạn sử dụng", Name = "Expiry", Width = 120, DefaultCellStyle = { Alignment = DataGridViewContentAlignment.MiddleCenter } });
        UiStyle.StyleGrid(_gridLowStock, fillLastColumn: false);
        lowStockGroup.Controls.Add(_gridLowStock);
        root.Controls.Add(lowStockGroup, 0, 2);

        Controls.Add(root);
        Controls.Add(header);
    }

    private void DrawRevenueChart(object? sender, PaintEventArgs e)
    {
        if (_chartData.Count == 0) return;

        var g = e.Graphics;
        g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
        g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

        var panel = (Panel)sender!;
        var rect = panel.ClientRectangle;
        var padLeft = 70; var padRight = 20; var padTop = 20; var padBottom = 50;
        var chartWidth = rect.Width - padLeft - padRight;
        var chartHeight = rect.Height - padTop - padBottom;
        if (chartWidth <= 0 || chartHeight <= 0) return;

        var maxRevenue = _chartData.Max(d => d.Revenue);
        if (maxRevenue <= 0) maxRevenue = 1;

        // Vẽ lưới Y
        using var gridPen = new Pen(Color.FromArgb(230, 234, 240), 1);
        var gridLines = 5;
        for (var i = 0; i <= gridLines; i++)
        {
            var y = padTop + chartHeight - (int)(chartHeight * i / gridLines);
            g.DrawLine(gridPen, padLeft, y, padLeft + chartWidth, y);
            var value = maxRevenue * i / gridLines;
            var label = value >= 1_000_000
                ? $"{value / 1_000_000:0.#}M"
                : value >= 1000
                    ? $"{value / 1000:0.#}K"
                    : value.ToString("0");
            g.DrawString(label, new Font("Segoe UI", 7.5f), Brushes.Gray,
                new RectangleF(2, y - 8, padLeft - 4, 16),
                new StringFormat { Alignment = StringAlignment.Far });
        }

        // Vẽ bars
        var barCount = _chartData.Count;
        var barAreaWidth = chartWidth / barCount;
        var barWidth = (int)(barAreaWidth * 0.60f);
        var barOffset = (barAreaWidth - barWidth) / 2;

        var today = DateTime.Today;
        for (var i = 0; i < barCount; i++)
        {
            var (date, revenue) = _chartData[i];
            var barH = (int)(chartHeight * revenue / maxRevenue);
            var x = padLeft + i * barAreaWidth + barOffset;
            var y = padTop + chartHeight - barH;

            var isToday = date.Date == today;
            using var barBrush = new SolidBrush(isToday ? UiStyle.Success : UiStyle.Primary);
            g.FillRectangle(barBrush, x, y, barWidth, barH);

            // Ngày
            var dayLabel = date.Day == today.Day ? "Hôm nay" : date.ToString("dd/MM");
            g.DrawString(dayLabel, new Font("Segoe UI", 8f, isToday ? FontStyle.Bold : FontStyle.Regular),
                isToday ? Brushes.DarkGreen : Brushes.Gray,
                new RectangleF(x - 4, padTop + chartHeight + 4, barWidth + 8, 20),
                new StringFormat { Alignment = StringAlignment.Center });

            // Giá trị
            if (barH > 20 && revenue > 0)
            {
                var valLabel = revenue >= 1_000_000
                    ? $"{revenue / 1_000_000:0.#}M"
                    : $"{revenue / 1000:0}K";
                g.DrawString(valLabel, new Font("Segoe UI", 7.5f, FontStyle.Bold), Brushes.White,
                    new RectangleF(x, y + 2, barWidth, 14),
                    new StringFormat { Alignment = StringAlignment.Center });
            }
        }

        // Trục Y
        using var axisPen = new Pen(Color.FromArgb(180, 190, 200), 1.5f);
        g.DrawLine(axisPen, padLeft, padTop, padLeft, padTop + chartHeight);
        g.DrawLine(axisPen, padLeft, padTop + chartHeight, padLeft + chartWidth, padTop + chartHeight);
    }

    private void LoadData()
    {
        try
        {
            var todaySummary = _services.SalesService.GetTodayProfitSummary();
            var monthSummary = _services.SalesService.GetCurrentMonthProfitSummary();
            var todayOrders = _services.SalesService.GetTodaySaleCount();
            var debt = _services.DebtService.GetTotalOutstanding();
            var expiryDays = _services.AppConfigService.NearExpiryWarningDays;
            var products = _services.ProductService.GetAll();
            var lowStockList = _services.InventoryService.GetLowStockProducts(products);
            var nearExpiryList = _services.InventoryService.GetNearExpiryProducts(products, expiryDays);

            _lblTodayRevenue.Text = $"{todaySummary.RevenueAmount:N0}\nVND";
            _lblMonthRevenue.Text = $"{_services.SalesService.GetCurrentMonthRevenue():N0}\nVND";
            _lblTodayOrders.Text = $"{todayOrders}\nĐơn hàng";
            _lblDebtOutstanding.Text = $"{debt:N0}\nVND";
            _lblTodayProfit.Text = $"{todaySummary.ProfitAmount:N0}\nVND";
            _lblMonthProfit.Text = $"{monthSummary.ProfitAmount:N0}\nVND";
            _lblLowStock.Text = $"{lowStockList.Count}\nMặt hàng";
            _lblNearExpiry.Text = $"{nearExpiryList.Count}\nMặt hàng ({expiryDays} ngày)";

            // Chart data
            _chartData = _services.SalesService.GetLast7DaysRevenue();
            _chartPanel.Invalidate();

            // Top selling
            var topSelling = _services.SalesService.GetTopSellingToday(5);
            _gridTopSelling.Rows.Clear();
            foreach (var (name, qty, rev) in topSelling)
            {
                _gridTopSelling.Rows.Add(name, qty.ToString(), $"{rev:N0}");
            }

            // Low stock grid
            _gridLowStock.Rows.Clear();
            foreach (var p in lowStockList.Take(50))
            {
                var expiry = p.ExpiryDate.HasValue ? p.ExpiryDate.Value.ToString("dd/MM/yyyy") : "–";
                var rowIdx = _gridLowStock.Rows.Add(
                    p.ProductName, p.CategoryName ?? "–",
                    p.StockQuantity.ToString("0.#"), p.MinStock.ToString(),
                    p.CostPrice.ToString("N0"), expiry);

                // Màu dòng: hết hàng = đỏ, sắp hết = vàng
                if (p.StockQuantity <= 0)
                    _gridLowStock.Rows[rowIdx].DefaultCellStyle.BackColor = UiStyle.StockEmpty;
                else
                    _gridLowStock.Rows[rowIdx].DefaultCellStyle.BackColor = UiStyle.StockLow;
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Không tải được dashboard: {ex.Message}", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    protected override void OnFormClosed(FormClosedEventArgs e)
    {
        _refreshTimer.Stop();
        base.OnFormClosed(e);
    }
}
