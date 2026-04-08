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

    public DashboardForm(AppServices services)
    {
        _services = services;

        Text = "Dashboard tổng quan";
        Width = 980;
        Height = 620;
        MinimumSize = new Size(900, 560);
        StartPosition = FormStartPosition.CenterParent;
        WindowState = FormWindowState.Maximized;
        AutoScroll = true;
        KeyPreview = true;
        BackColor = Color.WhiteSmoke;

        var title = new Label
        {
            Text = "DASHBOARD TỔNG QUAN",
            AutoSize = true,
            Font = new Font("Segoe UI", 20, FontStyle.Bold),
            Location = new Point(26, 18)
        };
        Controls.Add(title);

        var grid = new TableLayoutPanel
        {
            Location = new Point(24, 72),
            Size = new Size(920, 420),
            ColumnCount = 2,
            RowCount = 3
        };
        grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50f));
        grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50f));
        for (var i = 0; i < 3; i++)
        {
            grid.RowStyles.Add(new RowStyle(SizeType.Percent, 33.33f));
        }
        Controls.Add(grid);

        var cardTodayRevenue = CreateCard(UiStyle.Success, out _lblTodayRevenue);
        var cardMonthRevenue = CreateCard(UiStyle.AccentBlueDark, out _lblMonthRevenue);
        var cardTodayOrders = CreateCard(UiStyle.AccentSlate, out _lblTodayOrders);
        var cardDebt = CreateCard(UiStyle.AccentOrange, out _lblDebtOutstanding);
        var cardLowStock = CreateCard(UiStyle.AccentYellow, out _lblLowStock, darkText: true);
        var cardNearExpiry = CreateCard(UiStyle.AccentPurple, out _lblNearExpiry);

        grid.Controls.Add(cardTodayRevenue, 0, 0);
        grid.Controls.Add(cardMonthRevenue, 1, 0);
        grid.Controls.Add(cardTodayOrders, 0, 1);
        grid.Controls.Add(cardDebt, 1, 1);
        grid.Controls.Add(cardLowStock, 0, 2);
        grid.Controls.Add(cardNearExpiry, 1, 2);

        var btnRefresh = new Button
        {
            Text = "Làm mới",
            Width = 140,
            Height = 40,
            Location = new Point(24, 510),
            BackColor = UiStyle.Primary,
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat
        };
        btnRefresh.Click += (_, _) => LoadData();
        Controls.Add(btnRefresh);

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
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Không tải được dashboard: {ex.Message}", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }
}
