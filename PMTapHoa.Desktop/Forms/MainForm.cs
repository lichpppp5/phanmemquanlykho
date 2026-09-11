namespace PMTapHoa.Desktop.Forms;

public class MainForm : Form
{
    private readonly AppServices _services;
    private readonly ToolTip _permissionToolTip = new();
    public bool RequestLogout { get; private set; }
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
    private readonly Button _btnHealthCheck;
    private readonly Button _btnRestockRequests;
    private readonly Button _btnLogout;
    private readonly Button _btnImportHistory;

    private readonly Label _lblDateTime;
    private readonly Label _lblAlerts;
    private readonly System.Windows.Forms.Timer _clockTimer;
    private readonly System.Windows.Forms.Timer _alertTimer;

    public MainForm(AppServices services)
    {
        _services = services;

        Text = $"{_services.AppConfigService.StoreName} - Quản Lý";
        Width = 1300;
        Height = 860;
        MinimumSize = new Size(1180, 780);
        StartPosition = FormStartPosition.CenterScreen;
        WindowState = FormWindowState.Normal;
        AutoScaleMode = AutoScaleMode.Font;
        KeyPreview = true;
        BackColor = UiStyle.Background;
        UiStyle.ApplyWindowMode(this);

        // ── Header bar ──────────────────────────────────────────────────────────
        var headerPanel = new Panel
        {
            Dock = DockStyle.Top,
            Height = 72,
            BackColor = UiStyle.HeaderDark
        };
        var headerLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 3,
            RowCount = 1,
            Padding = new Padding(20, 0, 20, 0)
        };
        headerLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50f));
        headerLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 30f));
        headerLayout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));

        var storeName = _services.AppConfigService.StoreName;
        var storePhone = _services.AppConfigService.StorePhone;
        var storeText = string.IsNullOrWhiteSpace(storePhone) ? storeName : $"{storeName}  ·  {storePhone}";
        var lblStoreName = new Label
        {
            Text = storeText,
            ForeColor = Color.White,
            Font = new Font("Segoe UI", 17, FontStyle.Bold),
            AutoSize = true,
            Anchor = AnchorStyles.Left | AnchorStyles.None,
            TextAlign = ContentAlignment.MiddleLeft
        };

        _lblDateTime = new Label
        {
            Text = DateTime.Now.ToString("HH:mm  dd/MM/yyyy"),
            ForeColor = Color.FromArgb(160, 200, 230),
            Font = new Font("Segoe UI", 11, FontStyle.Regular),
            AutoSize = true,
            Anchor = AnchorStyles.Right | AnchorStyles.None,
            TextAlign = ContentAlignment.MiddleRight
        };

        _btnLogout = new Button
        {
            Text = "⏻  Đăng xuất",
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            MinimumSize = new Size(130, 40),
            Padding = new Padding(10, 0, 10, 0),
            BackColor = Color.FromArgb(192, 57, 43),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Anchor = AnchorStyles.Right,
            Font = new Font("Segoe UI", 10, FontStyle.Bold),
            Margin = new Padding(8, 16, 0, 16)
        };
        _btnLogout.FlatAppearance.BorderSize = 0;
        _btnLogout.Click += (_, _) => Logout();

        headerLayout.Controls.Add(lblStoreName, 0, 0);
        headerLayout.Controls.Add(_lblDateTime, 1, 0);
        headerLayout.Controls.Add(_btnLogout, 2, 0);
        headerPanel.Controls.Add(headerLayout);

        // ── Alert bar ───────────────────────────────────────────────────────────
        var alertBar = new Panel
        {
            Dock = DockStyle.Top,
            Height = 32,
            BackColor = Color.FromArgb(44, 62, 80),
            Visible = true
        };
        var alertBarLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            Padding = new Padding(20, 4, 20, 0)
        };
        alertBarLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
        alertBarLayout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));

        _lblAlerts = new Label
        {
            Dock = DockStyle.Fill,
            ForeColor = Color.FromArgb(255, 218, 102),
            Font = new Font("Segoe UI", 9.5f, FontStyle.Regular),
            TextAlign = ContentAlignment.MiddleLeft
        };
        var lblUser = new Label
        {
            Text = $"👤 {_services.Session.CurrentUser?.FullName}  ({_services.Session.CurrentUser?.Role})",
            ForeColor = Color.FromArgb(150, 180, 210),
            Font = new Font("Segoe UI", 9, FontStyle.Regular),
            AutoSize = true,
            Anchor = AnchorStyles.Right | AnchorStyles.None,
            TextAlign = ContentAlignment.MiddleRight
        };
        alertBarLayout.Controls.Add(_lblAlerts, 0, 0);
        alertBarLayout.Controls.Add(lblUser, 1, 0);
        alertBar.Controls.Add(alertBarLayout);

        // ── Main content ────────────────────────────────────────────────────────
        _btnDashboard = CreateMenuButton("📊  Dashboard Tổng Quan", UiStyle.Primary, Color.White);
        _btnDashboard.Click += (_, _) => new DashboardForm(_services).ShowDialog(this);

        _btnSales = CreateMenuButton("🛒  F1 - Bán Hàng (POS)", UiStyle.SuccessBright, Color.White);
        _btnSales.Click += (_, _) => OpenSalesForm();

        _btnInventory = CreateMenuButton("📦  Quản Lý Kho Hàng", UiStyle.AccentYellow, Color.White);
        _btnInventory.Click += (_, _) => new InventoryForm(_services).ShowDialog(this);

        _btnReport = CreateMenuButton("📈  Báo Cáo Doanh Thu", UiStyle.AccentPurple, Color.White);
        _btnReport.Click += (_, _) => new ReportForm(_services).ShowDialog(this);

        _btnDebt = CreateMenuButton("💰  Quản Lý Công Nợ", UiStyle.AccentOrange, Color.White);
        _btnDebt.Click += (_, _) => new DebtForm(_services).ShowDialog(this);

        _btnSalesHistory = CreateMenuButton("📋  Lịch Sử & In Lại HĐ", UiStyle.AccentTeal, Color.White);
        _btnSalesHistory.Click += (_, _) => new SalesHistoryForm(_services).ShowDialog(this);

        _btnRestockRequests = CreateMenuButton("🚚  Đặt Hàng Nhà Cung Cấp", UiStyle.Warning, Color.White);
        _btnRestockRequests.Click += (_, _) => new RestockRequestForm(_services).ShowDialog(this);

        _btnImportHistory = CreateMenuButton("🕒  Lịch Sử Nhập Hàng", UiStyle.AccentCyan, Color.White);
        _btnImportHistory.Click += (_, _) => new StockImportHistoryForm(_services).ShowDialog(this);

        _btnCategory = CreateMenuButton("🗂️  Quản Lý Danh Mục", UiStyle.AccentSlate, Color.White);
        _btnCategory.Click += (_, _) => new CategoryForm(_services).ShowDialog(this);

        _btnChangePassword = CreateMenuButton("🔑  Đổi Mật Khẩu", UiStyle.Neutral, Color.White);
        _btnChangePassword.Click += (_, _) => new ChangePasswordForm(_services).ShowDialog(this);

        _btnUserManagement = CreateMenuButton("👥  Quản Lý Người Dùng", UiStyle.Danger, Color.White);
        _btnUserManagement.Click += (_, _) => new UserManagementForm(_services).ShowDialog(this);

        _btnAuditLogs = CreateMenuButton("📝  Nhật Ký Thao Tác", UiStyle.AccentBlueDark, Color.White);
        _btnAuditLogs.Click += (_, _) => new AuditLogForm(_services).ShowDialog(this);

        _btnBackupRestore = CreateMenuButton("💾  Sao Lưu / Khôi Phục", UiStyle.AccentIndigo, Color.White);
        _btnBackupRestore.Click += (_, _) => new BackupRestoreForm(_services).ShowDialog(this);

        _btnSettings = CreateMenuButton("⚙️  Cấu Hình Hệ Thống", UiStyle.HeaderDark, Color.White);
        _btnSettings.Click += (_, _) =>
        {
            new SettingsForm(_services).ShowDialog(this);
            // Cập nhật tên cửa hàng trên header
            lblStoreName.Text = _services.AppConfigService.StoreName;
            Text = $"{_services.AppConfigService.StoreName} - Quản Lý";
        };

        _btnHealthCheck = CreateMenuButton("🩺  Kiểm Tra Hệ Thống", UiStyle.AccentTeal, Color.White);
        _btnHealthCheck.Click += (_, _) => new HealthCheckForm(_services).ShowDialog(this);

        var menuGrid = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 3,
            RowCount = 5,
            Margin = new Padding(0, 0, 0, 0),
            Padding = new Padding(16, 16, 16, 16)
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
        menuGrid.Controls.Add(_btnImportHistory, 1, 2);
        menuGrid.Controls.Add(_btnCategory, 2, 2);
        menuGrid.Controls.Add(_btnChangePassword, 0, 3);
        menuGrid.Controls.Add(_btnUserManagement, 1, 3);
        menuGrid.Controls.Add(_btnAuditLogs, 2, 3);
        menuGrid.Controls.Add(_btnBackupRestore, 0, 4);
        menuGrid.Controls.Add(_btnSettings, 1, 4);
        menuGrid.Controls.Add(_btnHealthCheck, 2, 4);

        Controls.Add(menuGrid);
        Controls.Add(alertBar);
        Controls.Add(headerPanel);

        ApplyRolePermissions();
        KeyDown += MainForm_KeyDown;

        // ── Timers ──────────────────────────────────────────────────────────────
        _clockTimer = new System.Windows.Forms.Timer { Interval = 10000 };
        _clockTimer.Tick += (_, _) => _lblDateTime.Text = DateTime.Now.ToString("HH:mm  dd/MM/yyyy");
        _clockTimer.Start();

        _alertTimer = new System.Windows.Forms.Timer { Interval = 300000 }; // 5 phút
        _alertTimer.Tick += (_, _) => RefreshAlerts();
        _alertTimer.Start();

        Load += (_, _) => RefreshAlerts();
    }

    private void RefreshAlerts()
    {
        try
        {
            var summary = _services.NotificationService.GetSummary(_services.AppConfigService.NearExpiryWarningDays);
            var text = _services.NotificationService.BuildAlertText(summary);
            _lblAlerts.Text = string.IsNullOrEmpty(text) ? "✅ Không có cảnh báo tồn kho nào." : text;
        }
        catch
        {
            // Ignore
        }
    }

    private void ApplyRolePermissions()
    {
        var isAdmin = _services.Session.IsAdmin;
        _btnReport.Enabled = isAdmin;
        _btnDebt.Enabled = isAdmin;
        _btnSalesHistory.Enabled = isAdmin;
        _btnRestockRequests.Enabled = isAdmin;
        _btnImportHistory.Enabled = isAdmin;
        _btnCategory.Enabled = isAdmin;
        _btnUserManagement.Enabled = isAdmin;
        _btnAuditLogs.Enabled = isAdmin;
        _btnBackupRestore.Enabled = isAdmin;
        _btnSettings.Enabled = isAdmin;
        _btnHealthCheck.Enabled = isAdmin;
        _btnDashboard.Enabled = isAdmin;

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
            _btnImportHistory,
            _btnCategory,
            _btnUserManagement,
            _btnAuditLogs,
            _btnBackupRestore,
            _btnSettings,
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
        RefreshAlerts();
    }

    private void Logout()
    {
        var confirm = MessageBox.Show(
            "Bạn muốn đăng xuất để chuyển tài khoản?",
            "Đăng xuất",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Question);
        if (confirm != DialogResult.Yes)
        {
            return;
        }

        _clockTimer.Stop();
        _alertTimer.Stop();
        _services.Session.Clear();
        RequestLogout = true;
        Close();
    }

    protected override void OnFormClosed(FormClosedEventArgs e)
    {
        _clockTimer.Stop();
        _alertTimer.Stop();
        base.OnFormClosed(e);
    }

    private static Button CreateMenuButton(string text, Color backColor, Color foreColor)
    {
        var button = new Button
        {
            Text = text,
            Dock = DockStyle.Fill,
            Margin = new Padding(10),
            Font = new Font("Segoe UI", 12, FontStyle.Bold),
            FlatStyle = FlatStyle.Flat,
            BackColor = backColor,
            ForeColor = foreColor,
            UseVisualStyleBackColor = false,
            TextAlign = ContentAlignment.MiddleLeft,
            Padding = new Padding(14, 0, 0, 0)
        };

        button.FlatAppearance.BorderColor = ControlPaint.Dark(backColor, 0.1f);
        button.FlatAppearance.BorderSize = 0;
        button.FlatAppearance.MouseDownBackColor = ControlPaint.Dark(backColor, 0.08f);
        button.FlatAppearance.MouseOverBackColor = ControlPaint.Light(backColor, 0.05f);
        return button;
    }
}
