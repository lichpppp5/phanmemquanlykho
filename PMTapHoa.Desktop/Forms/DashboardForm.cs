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
        Width = 650;
        Height = 430;
        StartPosition = FormStartPosition.CenterParent;
        KeyPreview = true;

        _lblTodayRevenue = CreateMetricLabel("Doanh thu hôm nay: 0 VND", 25);
        _lblMonthRevenue = CreateMetricLabel("Doanh thu tháng này: 0 VND", 70);
        _lblTodayOrders = CreateMetricLabel("Số đơn hôm nay: 0", 115);
        _lblDebtOutstanding = CreateMetricLabel("Tổng công nợ: 0 VND", 160);
        _lblLowStock = CreateMetricLabel("Sản phẩm sắp hết hàng: 0", 205);
        _lblNearExpiry = CreateMetricLabel("Sản phẩm gần hết hạn (14 ngày): 0", 250);

        Controls.Add(_lblTodayRevenue);
        Controls.Add(_lblMonthRevenue);
        Controls.Add(_lblTodayOrders);
        Controls.Add(_lblDebtOutstanding);
        Controls.Add(_lblLowStock);
        Controls.Add(_lblNearExpiry);

        var btnRefresh = new Button
        {
            Text = "Làm mới",
            Width = 120,
            Height = 40,
            Location = new Point(30, 305)
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

    private static Label CreateMetricLabel(string text, int top)
    {
        return new Label
        {
            Text = text,
            AutoSize = true,
            Font = new Font("Segoe UI", 12, FontStyle.Bold),
            Location = new Point(30, top)
        };
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
