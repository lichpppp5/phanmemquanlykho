namespace PMTapHoa.Desktop.Forms;

public class MainForm : Form
{
    private readonly AppServices _services;
    private readonly ToolTip _permissionToolTip = new();
    private readonly Button _btnSales;
    private readonly Button _btnInventory;
    private readonly Button _btnReport;
    private readonly Button _btnDebt;
    private readonly Button _btnSalesHistory;
    private readonly Button _btnCategory;
    private readonly Button _btnChangePassword;
    private readonly Button _btnUserManagement;
    private readonly Button _btnAuditLogs;
    private readonly Button _btnDashboard;
    private readonly Button _btnBackupRestore;
    private readonly Button _btnSettings;
    private readonly Button _btnSeedDemo;
    private readonly Button _btnHealthCheck;
    private readonly Button _btnRestockRequests;
    private readonly Label _lblUser;

    public MainForm(AppServices services)
    {
        _services = services;

        Text = "PM Tạp Hóa - Menu chính";
        Width = 1260;
        Height = 820;
        MinimumSize = new Size(1160, 740);
        StartPosition = FormStartPosition.CenterScreen;
        WindowState = FormWindowState.Maximized;
        KeyPreview = true;
        BackColor = Color.WhiteSmoke;

        var title = new Label
        {
            Text = "PHẦN MỀM QUẢN LÝ KHO & BÁN HÀNG",
            Font = new Font("Segoe UI", 24, FontStyle.Bold),
            AutoSize = true,
            Location = new Point(220, 35)
        };
        _lblUser = new Label
        {
            Text = $"Đăng nhập: {_services.Session.CurrentUser?.FullName} ({_services.Session.CurrentUser?.Role})",
            AutoSize = true,
            ForeColor = Color.DimGray,
            Font = new Font("Segoe UI", 12, FontStyle.Regular),
            Location = new Point(35, 100)
        };

        _btnDashboard = CreateMenuButton("Dashboard tổng quan", UiStyle.Primary, Color.White);
        _btnDashboard.Click += (_, _) => new DashboardForm(_services).ShowDialog(this);

        _btnSales = CreateMenuButton("F1 - Bán hàng", UiStyle.SuccessBright, Color.White);
        _btnSales.Click += (_, _) => OpenSalesForm();

        _btnInventory = CreateMenuButton("Kho hàng", UiStyle.AccentYellow, Color.Black);
        _btnInventory.Click += (_, _) => new InventoryForm(_services).ShowDialog(this);

        _btnReport = CreateMenuButton("Báo cáo doanh thu", UiStyle.AccentPurple, Color.White);
        _btnReport.Click += (_, _) => new ReportForm(_services).ShowDialog(this);

        _btnDebt = CreateMenuButton("Quản lý công nợ", UiStyle.AccentOrange, Color.White);
        _btnDebt.Click += (_, _) => new DebtForm(_services).ShowDialog(this);

        _btnSalesHistory = CreateMenuButton("Lịch sử & in lại hóa đơn", UiStyle.AccentTeal, Color.White);
        _btnSalesHistory.Click += (_, _) => new SalesHistoryForm(_services).ShowDialog(this);

        _btnRestockRequests = CreateMenuButton("Mối nhập hàng", UiStyle.Warning, Color.Black);
        _btnRestockRequests.Click += (_, _) => new RestockRequestForm(_services).ShowDialog(this);

        _btnCategory = CreateMenuButton("Quản lý danh mục", UiStyle.AccentSlate, Color.White);
        _btnCategory.Click += (_, _) => new CategoryForm(_services).ShowDialog(this);

        _btnChangePassword = CreateMenuButton("Đổi mật khẩu", UiStyle.Neutral, Color.White);
        _btnChangePassword.Click += (_, _) => new ChangePasswordForm(_services).ShowDialog(this);

        _btnUserManagement = CreateMenuButton("Quản lý người dùng", UiStyle.Danger, Color.White);
        _btnUserManagement.Click += (_, _) => new UserManagementForm(_services).ShowDialog(this);

        _btnAuditLogs = CreateMenuButton("Nhật ký thao tác", UiStyle.AccentBlueDark, Color.White);
        _btnAuditLogs.Click += (_, _) => new AuditLogForm(_services).ShowDialog(this);

        _btnBackupRestore = CreateMenuButton("Sao lưu / Khôi phục", UiStyle.AccentPurple, Color.White);
        _btnBackupRestore.Click += (_, _) => new BackupRestoreForm(_services).ShowDialog(this);

        _btnSettings = CreateMenuButton("Cấu hình hệ thống", UiStyle.HeaderDark, Color.White);
        _btnSettings.Click += (_, _) => new SettingsForm(_services).ShowDialog(this);

        _btnSeedDemo = CreateMenuButton("Nạp dữ liệu demo", UiStyle.Neutral, Color.Black);
        _btnSeedDemo.Click += (_, _) => SeedDemoData();

        _btnHealthCheck = CreateMenuButton("Kiểm tra hệ thống", UiStyle.AccentTeal, Color.White);
        _btnHealthCheck.Click += (_, _) => new HealthCheckForm(_services).ShowDialog(this);

        var menuGrid = new TableLayoutPanel
        {
            Location = new Point(80, 150),
            Width = 1080,
            Height = 590,
            ColumnCount = 3,
            RowCount = 5
        };
        menuGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.33f));
        menuGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.33f));
        menuGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.34f));
        for (var i = 0; i < 5; i++)
        {
            menuGrid.RowStyles.Add(new RowStyle(SizeType.Percent, 20f));
        }

        menuGrid.Controls.Add(_btnDashboard, 0, 0);
        menuGrid.Controls.Add(_btnSales, 1, 0);
        menuGrid.Controls.Add(_btnInventory, 2, 0);
        menuGrid.Controls.Add(_btnReport, 0, 1);
        menuGrid.Controls.Add(_btnDebt, 1, 1);
        menuGrid.Controls.Add(_btnSalesHistory, 2, 1);
        menuGrid.Controls.Add(_btnRestockRequests, 0, 2);
        menuGrid.Controls.Add(_btnCategory, 1, 2);
        menuGrid.Controls.Add(_btnChangePassword, 2, 2);
        menuGrid.Controls.Add(_btnUserManagement, 0, 3);
        menuGrid.Controls.Add(_btnAuditLogs, 1, 3);
        menuGrid.Controls.Add(_btnBackupRestore, 2, 3);
        menuGrid.Controls.Add(_btnSettings, 0, 4);
        menuGrid.Controls.Add(_btnSeedDemo, 1, 4);
        menuGrid.Controls.Add(_btnHealthCheck, 2, 4);

        Controls.Add(title);
        Controls.Add(_lblUser);
        Controls.Add(menuGrid);

        ApplyRolePermissions();
        KeyDown += MainForm_KeyDown;
    }

    private void ApplyRolePermissions()
    {
        var isAdmin = _services.Session.IsAdmin;
        _btnReport.Enabled = isAdmin;
        _btnDebt.Enabled = isAdmin;
        _btnSalesHistory.Enabled = isAdmin;
        _btnRestockRequests.Enabled = isAdmin;
        _btnCategory.Enabled = isAdmin;
        _btnUserManagement.Enabled = isAdmin;
        _btnAuditLogs.Enabled = isAdmin;
        _btnBackupRestore.Enabled = isAdmin;
        _btnSettings.Enabled = isAdmin;
        _btnSeedDemo.Enabled = isAdmin;
        _btnHealthCheck.Enabled = isAdmin;
        _btnDashboard.Enabled = isAdmin;

        // User thường: chỉ bán hàng + thêm/nhập hàng trong kho (và đổi mật khẩu).
        _btnSales.Enabled = true;
        _btnInventory.Enabled = true;
        _btnChangePassword.Enabled = true;

        ApplyAdminOnlyTooltips(isAdmin);
    }

    private void ApplyAdminOnlyTooltips(bool isAdmin)
    {
        var adminOnlyButtons = new[]
        {
            _btnDashboard,
            _btnReport,
            _btnDebt,
            _btnSalesHistory,
            _btnRestockRequests,
            _btnCategory,
            _btnUserManagement,
            _btnAuditLogs,
            _btnBackupRestore,
            _btnSettings,
            _btnSeedDemo,
            _btnHealthCheck
        };

        foreach (var button in adminOnlyButtons)
        {
            _permissionToolTip.SetToolTip(button, isAdmin ? string.Empty : "Chỉ Admin");
        }
    }

    private void MainForm_KeyDown(object? sender, KeyEventArgs e)
    {
        if (e.KeyCode == Keys.F1)
        {
            OpenSalesForm();
            e.Handled = true;
        }
        else if (e.KeyCode == Keys.Escape)
        {
            Close();
        }
    }

    private void OpenSalesForm()
    {
        new SalesForm(_services).ShowDialog(this);
    }

    private void SeedDemoData()
    {
        try
        {
            var confirm = MessageBox.Show(
                "Chỉ nạp demo khi kho dữ liệu trống. Tiếp tục?",
                "Xác nhận",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);
            if (confirm != DialogResult.Yes)
            {
                return;
            }

            var created = _services.DemoDataService.SeedIfEmpty();
            if (!created)
            {
                MessageBox.Show("Dữ liệu sản phẩm đã tồn tại, bỏ qua nạp demo.", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            _services.AuditService.Log(_services.Session.CurrentUser?.Username, "SEED_DEMO_DATA", "Đã nạp dữ liệu demo mẫu.");
            MessageBox.Show("Nạp dữ liệu demo thành công.", "Thành công", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Nạp dữ liệu demo thất bại: {ex.Message}", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private static Button CreateMenuButton(string text, Color backColor, Color foreColor)
    {
        var button = new Button
        {
            Text = text,
            Dock = DockStyle.Fill,
            Margin = new Padding(12),
            Font = new Font("Segoe UI", 14, FontStyle.Regular),
            FlatStyle = FlatStyle.Flat,
            BackColor = backColor,
            ForeColor = foreColor,
            UseVisualStyleBackColor = false
        };

        button.FlatAppearance.BorderColor = UiStyle.BorderMuted;
        button.FlatAppearance.BorderSize = 1;
        button.FlatAppearance.MouseDownBackColor = ControlPaint.Dark(backColor);
        button.FlatAppearance.MouseOverBackColor = ControlPaint.Light(backColor);
        return button;
    }
}
