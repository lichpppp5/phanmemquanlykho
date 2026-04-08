using System.Drawing.Printing;

namespace PMTapHoa.Desktop.Forms;

public class SettingsForm : Form
{
    private readonly AppServices _services;
    private readonly ComboBox _cmbPrinters;
    private readonly ComboBox _cmbPaperWidth;
    private readonly CheckBox _chkLowStockReminder;
    private readonly CheckBox _chkQrEnabled;
    private readonly TextBox _txtQrBankBin;
    private readonly TextBox _txtQrAccountNo;
    private readonly TextBox _txtQrAccountName;
    private readonly TextBox _txtQrPrefix;

    public SettingsForm(AppServices services)
    {
        _services = services;

        Text = "Cấu hình hệ thống";
        Width = 980;
        Height = 700;
        MinimumSize = new Size(860, 620);
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.Sizable;
        KeyPreview = true;
        BackColor = Color.WhiteSmoke;

        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 3,
            Padding = new Padding(14)
        };
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 150f));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 62f));
        Controls.Add(root);

        var basicBox = new GroupBox
        {
            Text = "Cấu hình chung",
            Dock = DockStyle.Fill,
            Font = new Font("Segoe UI", 10, FontStyle.Bold)
        };
        root.Controls.Add(basicBox, 0, 0);

        var basicLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 3,
            Padding = new Padding(12, 10, 12, 10)
        };
        basicLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 160f));
        basicLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
        basicBox.Controls.Add(basicLayout);

        basicLayout.Controls.Add(new Label
        {
            Text = "Máy in mặc định:",
            AutoSize = true,
            Anchor = AnchorStyles.Left
        }, 0, 0);
        _cmbPrinters = new ComboBox
        {
            Dock = DockStyle.Fill,
            DropDownStyle = ComboBoxStyle.DropDownList,
            Font = new Font("Segoe UI", 10, FontStyle.Regular)
        };
        basicLayout.Controls.Add(_cmbPrinters, 1, 0);

        basicLayout.Controls.Add(new Label
        {
            Text = "Khổ giấy mặc định:",
            AutoSize = true,
            Anchor = AnchorStyles.Left
        }, 0, 1);
        _cmbPaperWidth = new ComboBox
        {
            Dock = DockStyle.Left,
            Width = 120,
            DropDownStyle = ComboBoxStyle.DropDownList,
            Font = new Font("Segoe UI", 10, FontStyle.Regular)
        };
        _cmbPaperWidth.Items.AddRange(["58", "80"]);
        basicLayout.Controls.Add(_cmbPaperWidth, 1, 1);

        _chkLowStockReminder = new CheckBox
        {
            Text = "Bật cảnh báo tự động hàng sắp hết khi mở màn hình Kho",
            AutoSize = true,
            Anchor = AnchorStyles.Left
        };
        basicLayout.Controls.Add(_chkLowStockReminder, 1, 2);

        var qrBox = new GroupBox
        {
            Text = "Cấu hình QR thanh toán (VietQR)",
            Dock = DockStyle.Fill,
            Font = new Font("Segoe UI", 10, FontStyle.Bold)
        };
        root.Controls.Add(qrBox, 0, 1);

        var qrLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 4,
            RowCount = 5,
            Padding = new Padding(12, 10, 12, 10)
        };
        qrLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 140f));
        qrLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50f));
        qrLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 120f));
        qrLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50f));
        qrBox.Controls.Add(qrLayout);

        _chkQrEnabled = new CheckBox
        {
            Text = "Bật hiển thị QR khi thanh toán",
            AutoSize = true,
            Anchor = AnchorStyles.Left
        };
        qrLayout.Controls.Add(_chkQrEnabled, 0, 0);
        qrLayout.SetColumnSpan(_chkQrEnabled, 4);

        qrLayout.Controls.Add(new Label { Text = "BIN ngân hàng:", AutoSize = true, Anchor = AnchorStyles.Left }, 0, 1);
        _txtQrBankBin = new TextBox { Dock = DockStyle.Fill, Font = new Font("Segoe UI", 10, FontStyle.Regular) };
        qrLayout.Controls.Add(_txtQrBankBin, 1, 1);

        qrLayout.Controls.Add(new Label { Text = "Số tài khoản:", AutoSize = true, Anchor = AnchorStyles.Left }, 2, 1);
        _txtQrAccountNo = new TextBox { Dock = DockStyle.Fill, Font = new Font("Segoe UI", 10, FontStyle.Regular) };
        qrLayout.Controls.Add(_txtQrAccountNo, 3, 1);

        qrLayout.Controls.Add(new Label { Text = "Tên nhận tiền:", AutoSize = true, Anchor = AnchorStyles.Left }, 0, 2);
        _txtQrAccountName = new TextBox { Dock = DockStyle.Fill, Font = new Font("Segoe UI", 10, FontStyle.Regular) };
        qrLayout.Controls.Add(_txtQrAccountName, 1, 2);
        qrLayout.SetColumnSpan(_txtQrAccountName, 3);

        qrLayout.Controls.Add(new Label { Text = "Tiền tố nội dung CK:", AutoSize = true, Anchor = AnchorStyles.Left }, 0, 3);
        _txtQrPrefix = new TextBox { Dock = DockStyle.Left, Width = 130, Font = new Font("Segoe UI", 10, FontStyle.Regular) };
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
        root.Controls.Add(actionPanel, 0, 2);

        var btnSave = new Button
        {
            Text = "Lưu cấu hình",
            Width = 130,
            Height = 38,
            BackColor = UiStyle.Success,
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat
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
        _cmbPrinters.Items.Clear();
        _cmbPrinters.Items.Add("(Không chọn)");
        foreach (string printer in PrinterSettings.InstalledPrinters)
        {
            _cmbPrinters.Items.Add(printer);
        }

        var defaultPrinter = _services.AppConfigService.DefaultPrinter;
        if (!string.IsNullOrWhiteSpace(defaultPrinter) && _cmbPrinters.Items.Contains(defaultPrinter))
        {
            _cmbPrinters.SelectedItem = defaultPrinter;
        }
        else
        {
            _cmbPrinters.SelectedIndex = 0;
        }

        var defaultWidth = _services.AppConfigService.DefaultPaperWidth;
        _cmbPaperWidth.SelectedItem = defaultWidth.ToString();
        _chkLowStockReminder.Checked = _services.AppConfigService.LowStockReminderEnabled;
        _chkQrEnabled.Checked = _services.AppConfigService.QrPaymentEnabled;
        _txtQrBankBin.Text = _services.AppConfigService.QrBankBin;
        _txtQrAccountNo.Text = _services.AppConfigService.QrAccountNo;
        _txtQrAccountName.Text = _services.AppConfigService.QrAccountName;
        _txtQrPrefix.Text = _services.AppConfigService.QrTransferPrefix;
    }

    private void SaveSettings()
    {
        var selectedPrinter = _cmbPrinters.SelectedIndex <= 0 ? null : _cmbPrinters.SelectedItem?.ToString();
        var width = int.TryParse(_cmbPaperWidth.Text, out var parsed) ? parsed : 58;

        _services.AppConfigService.DefaultPrinter = selectedPrinter;
        _services.AppConfigService.DefaultPaperWidth = width;
        _services.AppConfigService.LowStockReminderEnabled = _chkLowStockReminder.Checked;
        _services.AppConfigService.QrPaymentEnabled = _chkQrEnabled.Checked;
        _services.AppConfigService.QrBankBin = _txtQrBankBin.Text.Trim();
        _services.AppConfigService.QrAccountNo = _txtQrAccountNo.Text.Trim();
        _services.AppConfigService.QrAccountName = _txtQrAccountName.Text.Trim();
        _services.AppConfigService.QrTransferPrefix = _txtQrPrefix.Text.Trim();
        _services.AuditService.Log(
            _services.Session.CurrentUser?.Username,
            "UPDATE_SETTINGS",
            $"Printer={selectedPrinter ?? "none"}, Width={width}, LowStockReminder={_chkLowStockReminder.Checked}, QREnabled={_chkQrEnabled.Checked}");
        MessageBox.Show("Đã lưu cấu hình.", "Thành công", MessageBoxButtons.OK, MessageBoxIcon.Information);
        Close();
    }
}
