using System.Drawing.Printing;

namespace PMTapHoa.Desktop.Forms;

public class SettingsForm : Form
{
    private readonly AppServices _services;
    private readonly ComboBox _cmbPrinters;
    private readonly ComboBox _cmbPaperWidth;
    private readonly CheckBox _chkLowStockReminder;

    public SettingsForm(AppServices services)
    {
        _services = services;

        Text = "Cấu hình hệ thống";
        Width = 560;
        Height = 340;
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        KeyPreview = true;

        Controls.Add(new Label
        {
            Text = "Máy in mặc định:",
            AutoSize = true,
            Location = new Point(30, 40)
        });
        _cmbPrinters = new ComboBox
        {
            Location = new Point(150, 36),
            Width = 360,
            DropDownStyle = ComboBoxStyle.DropDownList
        };
        Controls.Add(_cmbPrinters);

        Controls.Add(new Label
        {
            Text = "Khổ giấy mặc định:",
            AutoSize = true,
            Location = new Point(30, 90)
        });
        _cmbPaperWidth = new ComboBox
        {
            Location = new Point(150, 86),
            Width = 120,
            DropDownStyle = ComboBoxStyle.DropDownList
        };
        _cmbPaperWidth.Items.AddRange(["58", "80"]);
        Controls.Add(_cmbPaperWidth);

        _chkLowStockReminder = new CheckBox
        {
            Text = "Bật cảnh báo tự động hàng sắp hết khi mở màn hình Kho",
            AutoSize = true,
            Location = new Point(150, 135)
        };
        Controls.Add(_chkLowStockReminder);

        var btnSave = new Button
        {
            Text = "Lưu cấu hình",
            Width = 130,
            Height = 38,
            Location = new Point(150, 200)
        };
        btnSave.Click += (_, _) => SaveSettings();
        Controls.Add(btnSave);

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
    }

    private void SaveSettings()
    {
        var selectedPrinter = _cmbPrinters.SelectedIndex <= 0 ? null : _cmbPrinters.SelectedItem?.ToString();
        var width = int.TryParse(_cmbPaperWidth.Text, out var parsed) ? parsed : 58;

        _services.AppConfigService.DefaultPrinter = selectedPrinter;
        _services.AppConfigService.DefaultPaperWidth = width;
        _services.AppConfigService.LowStockReminderEnabled = _chkLowStockReminder.Checked;
        _services.AuditService.Log(
            _services.Session.CurrentUser?.Username,
            "UPDATE_SETTINGS",
            $"Printer={selectedPrinter ?? "none"}, Width={width}, LowStockReminder={_chkLowStockReminder.Checked}");
        MessageBox.Show("Đã lưu cấu hình.", "Thành công", MessageBoxButtons.OK, MessageBoxIcon.Information);
        Close();
    }
}
