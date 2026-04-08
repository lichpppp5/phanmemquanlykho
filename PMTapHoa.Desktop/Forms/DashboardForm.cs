namespace PMTapHoa.Desktop.Forms;

public class DashboardForm : Form
{
    private readonly AppServices _services;
    private readonly Label _lblTodayRevenue;
    private readonly Label _lblMonthRevenue;
    private readonly Label _lblTodayOrders;
    private readonly Label _lblDebtOutstanding;
    private readonly Label _lblLowStock;
    private readonly Label _lblNearExpiry;
    private readonly Label _lblTodayCapital;
    private readonly Label _lblTodayProfit;

    public DashboardForm(AppServices services)
    {
        _services = services;

        Text = "Dashboard tổng quan";
        Width = 1200;
        Height = 760;
        MinimumSize = new Size(1080, 700);
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
            Padding = new Padding(24, 18, 24, 18)
        };
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 54f));
        Controls.Add(root);

        var title = new Label
        {
            Text = "DASHBOARD TỔNG QUAN",
            AutoSize = true,
            Font = new Font("Segoe UI", 24, FontStyle.Bold),
            Margin = new Padding(0, 0, 0, 8)
        };
        root.Controls.Add(title, 0, 0);

        var grid = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 4
        };
        grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50f));
        grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50f));
        for (var i = 0; i < 4; i++)
        {
            grid.RowStyles.Add(new RowStyle(SizeType.Percent, 25f));
        }
        root.Controls.Add(grid, 0, 1);

        var cardTodayRevenue = CreateCard(UiStyle.Success, out _lblTodayRevenue);
        var cardMonthRevenue = CreateCard(UiStyle.AccentBlueDark, out _lblMonthRevenue);
        var cardTodayOrders = CreateCard(UiStyle.AccentSlate, out _lblTodayOrders);
        var cardDebt = CreateCard(UiStyle.AccentOrange, out _lblDebtOutstanding);
        var cardLowStock = CreateCard(UiStyle.AccentYellow, out _lblLowStock, darkText: true);
        var cardNearExpiry = CreateCard(UiStyle.AccentPurple, out _lblNearExpiry);
        var cardTodayCapital = CreateCard(UiStyle.Neutral, out _lblTodayCapital);
        var cardTodayProfit = CreateCard(UiStyle.SuccessBright, out _lblTodayProfit);

        grid.Controls.Add(cardTodayRevenue, 0, 0);
        grid.Controls.Add(cardMonthRevenue, 1, 0);
        grid.Controls.Add(cardTodayOrders, 0, 1);
        grid.Controls.Add(cardDebt, 1, 1);
        grid.Controls.Add(cardLowStock, 0, 2);
        grid.Controls.Add(cardNearExpiry, 1, 2);
        grid.Controls.Add(cardTodayCapital, 0, 3);
        grid.Controls.Add(cardTodayProfit, 1, 3);

        var btnRefresh = new Button
        {
            Text = "Làm mới",
            Width = 140,
            Height = 40,
            BackColor = UiStyle.Primary,
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat
        };
        btnRefresh.Click += (_, _) => LoadData();
        var actionPanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            Padding = new Padding(0, 8, 0, 0)
        };
        actionPanel.Controls.Add(btnRefresh);
        root.Controls.Add(actionPanel, 0, 2);

        Load += (_, _) => LoadData();
        KeyDown += (_, e) =>
        {
            if (e.KeyCode == Keys.Escape)
            {
                Close();
            }
        };
    }

    private static Panel CreateCard(Color color, out Label label, bool darkText = false)
    {
        var panel = new Panel
        {
            Margin = new Padding(10),
            Dock = DockStyle.Fill,
            BackColor = color
        };
        label = new Label
        {
            Text = "-",
            AutoSize = false,
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleCenter,
            Font = new Font("Segoe UI", 16, FontStyle.Bold),
            Padding = new Padding(10, 0, 10, 0),
            ForeColor = darkText ? Color.Black : Color.White
        };
        panel.Controls.Add(label);
        return panel;
    }

    private void LoadData()
    {
        try
        {
            var todayRevenue = _services.SalesService.GetTodayRevenue();
            var monthRevenue = _services.SalesService.GetCurrentMonthRevenue();
            var todaySummary = _services.SalesService.GetTodayProfitSummary();
            var monthSummary = _services.SalesService.GetCurrentMonthProfitSummary();
            var todayOrders = _services.SalesService.GetTodaySaleCount();
            var debt = _services.DebtService.GetTotalOutstanding();
            var products = _services.ProductService.GetAll();
            var lowStockCount = _services.InventoryService.GetLowStockProducts(products).Count;
            var nearExpiryCount = _services.InventoryService.GetNearExpiryProducts(products).Count;

            _lblTodayRevenue.Text = $"Doanh thu hôm nay: {todayRevenue:N0} VND";
            _lblMonthRevenue.Text = $"Doanh thu tháng này: {monthRevenue:N0} VND";
            _lblTodayOrders.Text = $"Số đơn hôm nay: {todayOrders}";
            _lblDebtOutstanding.Text = $"Tổng công nợ: {debt:N0} VND";
            _lblLowStock.Text = $"Sản phẩm sắp hết hàng: {lowStockCount}";
            _lblNearExpiry.Text = $"Sản phẩm gần hết hạn (14 ngày): {nearExpiryCount}";
            _lblTodayCapital.Text = $"Giá vốn hôm nay: {todaySummary.CapitalAmount:N0} VND\nGiá vốn tháng: {monthSummary.CapitalAmount:N0} VND";
            _lblTodayProfit.Text = $"Lãi hôm nay: {todaySummary.ProfitAmount:N0} VND\nLãi tháng: {monthSummary.ProfitAmount:N0} VND";
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Không tải được dashboard: {ex.Message}", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }
}
