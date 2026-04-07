namespace PMTapHoa.Desktop.Forms;

public class MainForm : Form
{
    private readonly AppServices _services;
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
        Width = 980;
        Height = 650;
        StartPosition = FormStartPosition.CenterScreen;
        KeyPreview = true;

        var title = new Label
        {
            Text = "PHẦN MỀM QUẢN LÝ KHO & BÁN HÀNG",
            Font = new Font("Segoe UI", 14, FontStyle.Bold),
            AutoSize = true,
            Location = new Point(130, 40)
        };
        _lblUser = new Label
        {
            Text = $"Đăng nhập: {_services.Session.CurrentUser?.FullName} ({_services.Session.CurrentUser?.Role})",
            AutoSize = true,
            ForeColor = Color.DimGray,
            Location = new Point(20, 90)
        };

        _btnDashboard = new Button
        {
            Text = "Dashboard tổng quan",
            Width = 220,
            Height = 50,
            Location = new Point(80, 140)
        };
        _btnDashboard.Click += (_, _) => new DashboardForm(_services).ShowDialog(this);

        _btnSales = new Button
        {
            Text = "F1 - Bán hàng (POS)",
            Width = 220,
            Height = 50,
            Location = new Point(330, 140)
        };
        _btnSales.Click += (_, _) => OpenSalesForm();

        _btnInventory = new Button
        {
            Text = "Kho hàng",
            Width = 220,
            Height = 50,
            Location = new Point(580, 140)
        };
        _btnInventory.Click += (_, _) => new InventoryForm(_services).ShowDialog(this);

        _btnReport = new Button
        {
            Text = "Báo cáo doanh thu",
            Width = 220,
            Height = 50,
            Location = new Point(80, 225)
        };
        _btnReport.Click += (_, _) => new ReportForm(_services).ShowDialog(this);

        _btnDebt = new Button
        {
            Text = "Quản lý công nợ",
            Width = 220,
            Height = 50,
            Location = new Point(330, 225)
        };
        _btnDebt.Click += (_, _) => new DebtForm(_services).ShowDialog(this);

        _btnSalesHistory = new Button
        {
            Text = "Lịch sử & in lại hóa đơn",
            Width = 220,
            Height = 50,
            Location = new Point(580, 225)
        };
        _btnSalesHistory.Click += (_, _) => new SalesHistoryForm(_services).ShowDialog(this);

        _btnRestockRequests = new Button
        {
            Text = "Mối nhập hàng",
            Width = 220,
            Height = 50,
            Location = new Point(80, 310)
        };
        _btnRestockRequests.Click += (_, _) => new RestockRequestForm(_services).ShowDialog(this);

        _btnCategory = new Button
        {
            Text = "Quản lý danh mục",
            Width = 220,
            Height = 50,
            Location = new Point(330, 310)
        };
        _btnCategory.Click += (_, _) => new CategoryForm(_services).ShowDialog(this);

        _btnChangePassword = new Button
        {
            Text = "Đổi mật khẩu",
            Width = 220,
            Height = 50,
            Location = new Point(580, 310)
        };
        _btnChangePassword.Click += (_, _) => new ChangePasswordForm(_services).ShowDialog(this);

        _btnUserManagement = new Button
        {
            Text = "Quản lý người dùng",
            Width = 220,
            Height = 50,
            Location = new Point(80, 395)
        };
        _btnUserManagement.Click += (_, _) => new UserManagementForm(_services).ShowDialog(this);

        _btnAuditLogs = new Button
        {
            Text = "Nhật ký thao tác",
            Width = 220,
            Height = 50,
            Location = new Point(330, 395)
        };
        _btnAuditLogs.Click += (_, _) => new AuditLogForm(_services).ShowDialog(this);

        _btnBackupRestore = new Button
        {
            Text = "Sao lưu / Khôi phục",
            Width = 220,
            Height = 50,
            Location = new Point(580, 395)
        };
        _btnBackupRestore.Click += (_, _) => new BackupRestoreForm(_services).ShowDialog(this);

        _btnSettings = new Button
        {
            Text = "Cấu hình hệ thống",
            Width = 220,
            Height = 50,
            Location = new Point(80, 480)
        };
        _btnSettings.Click += (_, _) => new SettingsForm(_services).ShowDialog(this);

        _btnSeedDemo = new Button
        {
            Text = "Nạp dữ liệu demo",
            Width = 220,
            Height = 50,
            Location = new Point(330, 480)
        };
        _btnSeedDemo.Click += (_, _) => SeedDemoData();

        _btnHealthCheck = new Button
        {
            Text = "Kiểm tra hệ thống",
            Width = 220,
            Height = 50,
            Location = new Point(580, 480)
        };
        _btnHealthCheck.Click += (_, _) => new HealthCheckForm(_services).ShowDialog(this);

        Controls.Add(title);
        Controls.Add(_lblUser);
        Controls.Add(_btnDashboard);
        Controls.Add(_btnSales);
        Controls.Add(_btnInventory);
        Controls.Add(_btnReport);
        Controls.Add(_btnDebt);
        Controls.Add(_btnSalesHistory);
        Controls.Add(_btnRestockRequests);
        Controls.Add(_btnCategory);
        Controls.Add(_btnChangePassword);
        Controls.Add(_btnUserManagement);
        Controls.Add(_btnAuditLogs);
        Controls.Add(_btnBackupRestore);
        Controls.Add(_btnSettings);
        Controls.Add(_btnSeedDemo);
        Controls.Add(_btnHealthCheck);

        ApplyRolePermissions();
        KeyDown += MainForm_KeyDown;
    }

    private void ApplyRolePermissions()
    {
        var isManager = _services.Session.IsManager;
        _btnReport.Enabled = isManager;
        _btnDebt.Enabled = isManager;
        _btnCategory.Enabled = isManager;
        _btnUserManagement.Enabled = isManager;
        _btnAuditLogs.Enabled = isManager;
        _btnBackupRestore.Enabled = isManager;
        _btnSeedDemo.Enabled = isManager;
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
}
