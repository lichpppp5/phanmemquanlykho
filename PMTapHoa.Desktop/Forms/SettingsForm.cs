using System.Drawing.Printing;

namespace PMTapHoa.Desktop.Forms;

public class SettingsForm : Form
{
    private readonly AppServices _services;
    // Store info
    private readonly TextBox _txtStoreName;
    private readonly TextBox _txtStoreAddress;
    private readonly TextBox _txtStorePhone;
    private readonly TextBox _txtReceiptFooter;
    private readonly NumericUpDown _numNearExpiryDays;
    // Print
    private readonly ComboBox _cmbPrinters;
    private readonly ComboBox _cmbPaperWidth;
    private readonly ComboBox _cmbDisplayMode;
    private readonly CheckBox _chkLowStockReminder;
    // QR
    private readonly CheckBox _chkQrEnabled;
    private readonly TextBox _txtQrBankBin;
    private readonly TextBox _txtQrAccountNo;
    private readonly TextBox _txtQrAccountName;
    private readonly TextBox _txtQrPrefix;

    public SettingsForm(AppServices services)
    {
        _services = services;

        Text = "Cấu hình hệ thống";
        Width = 1240;
        Height = 860;
        MinimumSize = new Size(1080, 760);
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.Sizable;
        Font = new Font("Segoe UI", 11, FontStyle.Regular);
        KeyPreview = true;
        BackColor = Color.WhiteSmoke;
        UiStyle.ApplyWindowMode(this);

        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 4,
            Padding = new Padding(16)
        };
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 200f)); // Store info
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 220f)); // Print/display
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));  // QR
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 76f));  // Actions
        Controls.Add(root);

        // ── Store info group ───────────────────────────────────────────────────
        var storeBox = new GroupBox
        {
            Text = "🏪  Thông tin cửa hàng",
            Dock = DockStyle.Fill,
            Font = new Font("Segoe UI", 12, FontStyle.Bold),
            ForeColor = UiStyle.Primary
        };
        root.Controls.Add(storeBox, 0, 0);

        var storeLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 4,
            RowCount = 3,
            Padding = new Padding(12, 8, 12, 8)
        };
        storeLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 130f));
        storeLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 55f));
        storeLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 110f));
        storeLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 45f));
        for (var i = 0; i < 3; i++) storeLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 50f));
        storeBox.Controls.Add(storeLayout);

        Label MkLbl(string t) => new Label { Text = t, AutoSize = false, Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft, Font = new Font("Segoe UI", 10, FontStyle.Bold) };
        TextBox MkTxt() => new TextBox { Dock = DockStyle.Fill, Font = new Font("Segoe UI", 11) };

        storeLayout.Controls.Add(MkLbl("Tên cửa hàng:"), 0, 0);
        _txtStoreName = MkTxt();
        storeLayout.Controls.Add(_txtStoreName, 1, 0);
        storeLayout.Controls.Add(MkLbl("Số điện thoại:"), 2, 0);
        _txtStorePhone = MkTxt();
        storeLayout.Controls.Add(_txtStorePhone, 3, 0);

        storeLayout.Controls.Add(MkLbl("Địa chỉ:"), 0, 1);
        _txtStoreAddress = MkTxt();
        storeLayout.Controls.Add(_txtStoreAddress, 1, 1);
        storeLayout.SetColumnSpan(_txtStoreAddress, 3);

        storeLayout.Controls.Add(MkLbl("Chân hóa đơn:"), 0, 2);
        _txtReceiptFooter = MkTxt();
        storeLayout.Controls.Add(_txtReceiptFooter, 1, 2);
        storeLayout.Controls.Add(MkLbl("Cảnh báo HSD (ngày):"), 2, 2);
        _numNearExpiryDays = new NumericUpDown { Dock = DockStyle.Fill, Minimum = 1, Maximum = 180, Font = new Font("Segoe UI", 11) };
        storeLayout.Controls.Add(_numNearExpiryDays, 3, 2);

        var basicBox = new GroupBox
        {
            Text = "🖨️  Máy in & Hiển thị",
            Dock = DockStyle.Fill,
            Font = new Font("Segoe UI", 12, FontStyle.Bold),
            ForeColor = UiStyle.AccentSlate
        };
        root.Controls.Add(basicBox, 0, 1);

        var basicLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 4,
            Padding = new Padding(12, 10, 12, 10)
        };
        basicLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 280f));
        basicLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
        basicLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 52f));
        basicLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 52f));
        basicLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 52f));
        basicLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 52f));
        basicBox.Controls.Add(basicLayout);

        basicLayout.Controls.Add(new Label
        {
            Text = "Máy in mặc định:",
            AutoSize = false,
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft,
            Font = new Font("Segoe UI", 11, FontStyle.Bold)
        }, 0, 0);
        _cmbPrinters = new ComboBox
        {
            Dock = DockStyle.Fill,
            DropDownStyle = ComboBoxStyle.DropDownList,
            Font = new Font("Segoe UI", 11, FontStyle.Regular)
        };
        basicLayout.Controls.Add(_cmbPrinters, 1, 0);

        basicLayout.Controls.Add(new Label
        {
            Text = "Khổ giấy mặc định:",
            AutoSize = false,
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft,
            Font = new Font("Segoe UI", 11, FontStyle.Bold)
        }, 0, 1);
        _cmbPaperWidth = new ComboBox
        {
            Dock = DockStyle.Left,
            Width = 140,
            DropDownStyle = ComboBoxStyle.DropDownList,
            Font = new Font("Segoe UI", 11, FontStyle.Regular)
        };
        _cmbPaperWidth.Items.AddRange(["58", "80"]);
        basicLayout.Controls.Add(_cmbPaperWidth, 1, 1);

        basicLayout.Controls.Add(new Label
        {
            Text = "Chế độ hiển thị:",
            AutoSize = false,
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft,
            Font = new Font("Segoe UI", 11, FontStyle.Bold)
        }, 0, 2);
        _cmbDisplayMode = new ComboBox
        {
            Dock = DockStyle.Left,
            Width = 280,
            DropDownStyle = ComboBoxStyle.DropDownList,
            Font = new Font("Segoe UI", 11, FontStyle.Regular)
        };
        _cmbDisplayMode.Items.AddRange(["Giao diện chuẩn", "Toàn màn hình"]);
        basicLayout.Controls.Add(_cmbDisplayMode, 1, 2);

        _chkLowStockReminder = new CheckBox
        {
            Text = "Bật cảnh báo tự động hàng sắp hết khi mở màn hình Kho",
            AutoSize = true,
            Anchor = AnchorStyles.Left
        };
        basicLayout.Controls.Add(_chkLowStockReminder, 0, 3);
        basicLayout.SetColumnSpan(_chkLowStockReminder, 2);

        var qrBox = new GroupBox
        {
            Text = "📱  Cấu hình QR thanh toán (VietQR)",
            Dock = DockStyle.Fill,
            Font = new Font("Segoe UI", 12, FontStyle.Bold),
            ForeColor = UiStyle.AccentPurple
        };
        root.Controls.Add(qrBox, 0, 2);

        var qrLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 4,
            RowCount = 5,
            Padding = new Padding(12, 10, 12, 10)
        };
        qrLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 220f));
        qrLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50f));
        qrLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 200f));
        qrLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50f));
        qrLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 44f));
        qrLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 48f));
        qrLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 48f));
        qrLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 48f));
        qrLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 38f));
        qrBox.Controls.Add(qrLayout);

        _chkQrEnabled = new CheckBox
        {
            Text = "Bật hiển thị QR khi thanh toán",
            AutoSize = true,
            Anchor = AnchorStyles.Left
        };
        qrLayout.Controls.Add(_chkQrEnabled, 0, 0);
        qrLayout.SetColumnSpan(_chkQrEnabled, 4);

        qrLayout.Controls.Add(new Label { Text = "BIN ngân hàng:", AutoSize = false, Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft, Font = new Font("Segoe UI", 11, FontStyle.Bold) }, 0, 1);
        _txtQrBankBin = new TextBox { Dock = DockStyle.Fill, Font = new Font("Segoe UI", 11, FontStyle.Regular) };
        qrLayout.Controls.Add(_txtQrBankBin, 1, 1);

        qrLayout.Controls.Add(new Label { Text = "Số tài khoản:", AutoSize = false, Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft, Font = new Font("Segoe UI", 11, FontStyle.Bold) }, 2, 1);
        _txtQrAccountNo = new TextBox { Dock = DockStyle.Fill, Font = new Font("Segoe UI", 11, FontStyle.Regular) };
        qrLayout.Controls.Add(_txtQrAccountNo, 3, 1);

        qrLayout.Controls.Add(new Label { Text = "Tên nhận tiền:", AutoSize = false, Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft, Font = new Font("Segoe UI", 11, FontStyle.Bold) }, 0, 2);
        _txtQrAccountName = new TextBox { Dock = DockStyle.Fill, Font = new Font("Segoe UI", 11, FontStyle.Regular) };
        qrLayout.Controls.Add(_txtQrAccountName, 1, 2);
        qrLayout.SetColumnSpan(_txtQrAccountName, 3);

        qrLayout.Controls.Add(new Label { Text = "Tiền tố nội dung CK:", AutoSize = false, Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft, Font = new Font("Segoe UI", 11, FontStyle.Bold) }, 0, 3);
        _txtQrPrefix = new TextBox { Dock = DockStyle.Left, Width = 160, Font = new Font("Segoe UI", 11, FontStyle.Regular) };
        qrLayout.Controls.Add(_txtQrPrefix, 1, 3);

        var lblQrHint = new Label
        {
            Text = "Ví dụ BIN: 970436 (Vietcombank), 970422 (MB), 970418 (BIDV).",
            AutoSize = true,
            ForeColor = Color.DimGray,
            Anchor = AnchorStyles.Left
        };
        qrLayout.Controls.Add(lblQrHint, 0, 4);
        qrLayout.SetColumnSpan(lblQrHint, 4);

        var actionPanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            Padding = new Padding(4, 8, 4, 4)
        };
        root.Controls.Add(actionPanel, 0, 3);

        var btnSave = new Button
        {
            Text = "Lưu cấu hình",
            Width = 170,
            Height = 44,
            BackColor = UiStyle.Success,
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI", 10, FontStyle.Bold)
        };
        btnSave.Click += (_, _) => SaveSettings();
        actionPanel.Controls.Add(btnSave);

        Load += (_, _) => LoadSettings();
        KeyDown += (_, e) =>
        {
            if (e.KeyCode == Keys.Escape)
            {
                Close();
            }
        };
    }

    private void LoadSettings()
    {
        // Store info
        _txtStoreName.Text = _services.AppConfigService.StoreName;
        _txtStoreAddress.Text = _services.AppConfigService.StoreAddress;
        _txtStorePhone.Text = _services.AppConfigService.StorePhone;
        _txtReceiptFooter.Text = _services.AppConfigService.ReceiptFooter;
        _numNearExpiryDays.Value = Math.Max(1, Math.Min(180, _services.AppConfigService.NearExpiryWarningDays));

        // Print
        _cmbPrinters.Items.Clear();
        _cmbPrinters.Items.Add("(Không chọn)");
        foreach (string printer in PrinterSettings.InstalledPrinters)
        {
            _cmbPrinters.Items.Add(printer);
        }

        var defaultPrinter = _services.AppConfigService.DefaultPrinter;
        if (!string.IsNullOrWhiteSpace(defaultPrinter) && _cmbPrinters.Items.Contains(defaultPrinter))
            _cmbPrinters.SelectedItem = defaultPrinter;
        else
            _cmbPrinters.SelectedIndex = 0;

        var defaultWidth = _services.AppConfigService.DefaultPaperWidth;
        _cmbPaperWidth.SelectedItem = defaultWidth.ToString();
        _cmbDisplayMode.SelectedIndex = _services.AppConfigService.DisplayFullScreen ? 1 : 0;
        if (_cmbDisplayMode.SelectedIndex < 0) _cmbDisplayMode.SelectedIndex = 1;
        _chkLowStockReminder.Checked = _services.AppConfigService.LowStockReminderEnabled;
        _chkQrEnabled.Checked = _services.AppConfigService.QrPaymentEnabled;
        _txtQrBankBin.Text = _services.AppConfigService.QrBankBin;
        _txtQrAccountNo.Text = _services.AppConfigService.QrAccountNo;
        _txtQrAccountName.Text = _services.AppConfigService.QrAccountName;
        _txtQrPrefix.Text = _services.AppConfigService.QrTransferPrefix;
    }

    private void SaveSettings()
    {
        // Store info
        _services.AppConfigService.StoreName = _txtStoreName.Text.Trim();
        _services.AppConfigService.StoreAddress = _txtStoreAddress.Text.Trim();
        _services.AppConfigService.StorePhone = _txtStorePhone.Text.Trim();
        _services.AppConfigService.ReceiptFooter = _txtReceiptFooter.Text.Trim();
        _services.AppConfigService.NearExpiryWarningDays = (int)_numNearExpiryDays.Value;

        var selectedPrinter = _cmbPrinters.SelectedIndex <= 0 ? null : _cmbPrinters.SelectedItem?.ToString();
        var width = int.TryParse(_cmbPaperWidth.Text, out var parsed) ? parsed : 58;

        _services.AppConfigService.DefaultPrinter = selectedPrinter;
        _services.AppConfigService.DefaultPaperWidth = width;
        _services.AppConfigService.DisplayFullScreen = _cmbDisplayMode.SelectedIndex == 1;
        UiStyle.FullScreenEnabled = _services.AppConfigService.DisplayFullScreen;
        _services.AppConfigService.LowStockReminderEnabled = _chkLowStockReminder.Checked;
        _services.AppConfigService.QrPaymentEnabled = _chkQrEnabled.Checked;
        _services.AppConfigService.QrBankBin = _txtQrBankBin.Text.Trim();
        _services.AppConfigService.QrAccountNo = _txtQrAccountNo.Text.Trim();
        _services.AppConfigService.QrAccountName = _txtQrAccountName.Text.Trim();
        _services.AppConfigService.QrTransferPrefix = _txtQrPrefix.Text.Trim();
        _services.AuditService.Log(
            _services.Session.CurrentUser?.Username,
            "UPDATE_SETTINGS",
            $"Store={_services.AppConfigService.StoreName}, Printer={selectedPrinter ?? "none"}, Width={width}");
        MessageBox.Show("Đã lưu cấu hình thành công!", "Thành công", MessageBoxButtons.OK, MessageBoxIcon.Information);
        Close();
    }
}
