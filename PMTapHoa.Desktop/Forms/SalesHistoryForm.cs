using PMTapHoa.Desktop.Models;

namespace PMTapHoa.Desktop.Forms;

public class SalesHistoryForm : Form
{
    private readonly AppServices _services;
    private readonly DataGridView _gridSales;
    private readonly DataGridView _gridDetails;
    private readonly TextBox _txtKeyword;
    private readonly DateTimePicker _dtFrom;
    private readonly DateTimePicker _dtTo;
    private readonly ComboBox _cmbPaperWidth;

    public SalesHistoryForm(AppServices services)
    {
        _services = services;

        Text = "Lịch sử hóa đơn & in lại";
        Width = 1280;
        Height = 820;
        MinimumSize = new Size(1160, 720);
        StartPosition = FormStartPosition.CenterParent;
        WindowState = FormWindowState.Maximized;
        KeyPreview = true;
        BackColor = Color.WhiteSmoke;

        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 4,
            Padding = new Padding(14)
        };
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 58f));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 52f));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 48f));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 58f));
        Controls.Add(root);

        var filterPanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            Padding = new Padding(4, 10, 4, 4)
        };
        root.Controls.Add(filterPanel, 0, 0);

        var lblKeyword = new Label { Text = "Tìm HĐ/khách:", AutoSize = true, Margin = new Padding(0, 8, 8, 0) };
        _txtKeyword = new TextBox { Width = 220, Margin = new Padding(0, 4, 18, 0), Font = new Font("Segoe UI", 10, FontStyle.Regular) };

        var lblFrom = new Label { Text = "Từ:", AutoSize = true, Margin = new Padding(0, 8, 6, 0) };
        _dtFrom = new DateTimePicker
        {
            Width = 140,
            Margin = new Padding(0, 4, 16, 0),
            Format = DateTimePickerFormat.Short,
            ShowCheckBox = true,
            Font = new Font("Segoe UI", 10, FontStyle.Regular)
        };

        var lblTo = new Label { Text = "Đến:", AutoSize = true, Margin = new Padding(0, 8, 6, 0) };
        _dtTo = new DateTimePicker
        {
            Width = 140,
            Margin = new Padding(0, 4, 16, 0),
            Format = DateTimePickerFormat.Short,
            ShowCheckBox = true,
            Font = new Font("Segoe UI", 10, FontStyle.Regular)
        };

        var btnFilter = new Button
        {
            Text = "Lọc",
            Width = 100,
            Height = 34,
            BackColor = UiStyle.Primary,
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Margin = new Padding(0, 2, 0, 0)
        };
        btnFilter.Click += (_, _) => LoadSales();
        filterPanel.Controls.Add(lblKeyword);
        filterPanel.Controls.Add(_txtKeyword);
        filterPanel.Controls.Add(lblFrom);
        filterPanel.Controls.Add(_dtFrom);
        filterPanel.Controls.Add(lblTo);
        filterPanel.Controls.Add(_dtTo);
        filterPanel.Controls.Add(btnFilter);

        _gridSales = new DataGridView
        {
            Dock = DockStyle.Fill,
            ReadOnly = true,
            AllowUserToAddRows = false,
            AutoGenerateColumns = false,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            MultiSelect = false
        };
        _gridSales.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = nameof(SaleHistoryItem.SaleID), HeaderText = "Hóa đơn", Width = 90 });
        _gridSales.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = nameof(SaleHistoryItem.SaleDate), HeaderText = "Ngày bán", Width = 180 });
        _gridSales.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = nameof(SaleHistoryItem.CustomerName), HeaderText = "Khách hàng", Width = 300 });
        _gridSales.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = nameof(SaleHistoryItem.TotalAmount), HeaderText = "Tổng tiền", Width = 160 });
        _gridSales.Columns.Add(new DataGridViewCheckBoxColumn { DataPropertyName = nameof(SaleHistoryItem.IsDebt), HeaderText = "Ghi nợ", Width = 90 });
        _gridSales.DefaultCellStyle.Font = new Font("Segoe UI", 10, FontStyle.Regular);
        _gridSales.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 10, FontStyle.Bold);
        _gridSales.ColumnHeadersHeight = 36;
        _gridSales.RowTemplate.Height = 32;
        _gridSales.AlternatingRowsDefaultCellStyle.BackColor = UiStyle.GridAltRow;
        _gridSales.SelectionChanged += (_, _) => LoadSaleDetails();
        root.Controls.Add(_gridSales, 0, 1);

        _gridDetails = new DataGridView
        {
            Dock = DockStyle.Fill,
            ReadOnly = true,
            AllowUserToAddRows = false,
            AutoGenerateColumns = false
        };
        _gridDetails.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = nameof(CartItem.Barcode), HeaderText = "Mã vạch", Width = 180 });
        _gridDetails.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = nameof(CartItem.ProductName), HeaderText = "Tên sản phẩm", Width = 430 });
        _gridDetails.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = nameof(CartItem.Quantity), HeaderText = "SL", Width = 100 });
        _gridDetails.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = nameof(CartItem.UnitPrice), HeaderText = "Đơn giá", Width = 180 });
        _gridDetails.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = nameof(CartItem.LineTotal), HeaderText = "Thành tiền", Width = 180 });
        _gridDetails.DefaultCellStyle.Font = new Font("Segoe UI", 10, FontStyle.Regular);
        _gridDetails.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 10, FontStyle.Bold);
        _gridDetails.ColumnHeadersHeight = 36;
        _gridDetails.RowTemplate.Height = 32;
        _gridDetails.AlternatingRowsDefaultCellStyle.BackColor = UiStyle.GridAltRow;
        root.Controls.Add(_gridDetails, 0, 2);

        var actionPanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            Padding = new Padding(4, 10, 4, 4)
        };
        root.Controls.Add(actionPanel, 0, 3);

        var lblPaper = new Label { Text = "Khổ in:", AutoSize = true, Margin = new Padding(0, 8, 8, 0) };
        _cmbPaperWidth = new ComboBox
        {
            DropDownStyle = ComboBoxStyle.DropDownList,
            Width = 90,
            Margin = new Padding(0, 4, 16, 0),
            Font = new Font("Segoe UI", 10, FontStyle.Regular)
        };
        _cmbPaperWidth.Items.AddRange(["58", "80"]);
        _cmbPaperWidth.SelectedItem = _services.AppConfigService.DefaultPaperWidth.ToString();

        var btnReprint = new Button
        {
            Text = "In lại hóa đơn",
            Width = 160,
            Height = 36,
            BackColor = UiStyle.Primary,
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Margin = new Padding(0, 2, 0, 0)
        };
        btnReprint.Click += (_, _) => ReprintSelectedSale();
        actionPanel.Controls.Add(lblPaper);
        actionPanel.Controls.Add(_cmbPaperWidth);
        actionPanel.Controls.Add(btnReprint);

        Load += (_, _) => LoadSales();
        KeyDown += (_, e) =>
        {
            if (e.KeyCode == Keys.Escape)
            {
                Close();
            }
        };
    }

    private void LoadSales()
    {
        try
        {
            DateTime? from = _dtFrom.Checked ? _dtFrom.Value.Date : null;
            DateTime? to = null;
            if (_dtTo.Checked)
            {
                to = _dtTo.Value.Date.AddDays(1).AddSeconds(-1);
            }

            var sales = _services.SalesService.GetSales(_txtKeyword.Text, from, to);
            _gridSales.DataSource = null;
            _gridSales.DataSource = sales;
            LoadSaleDetails();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Lỗi tải lịch sử hóa đơn: {ex.Message}", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void LoadSaleDetails()
    {
        if (_gridSales.CurrentRow?.DataBoundItem is not SaleHistoryItem selected)
        {
            _gridDetails.DataSource = null;
            return;
        }

        var details = _services.SalesService.GetSaleDetailsForReceipt(selected.SaleID);
        _gridDetails.DataSource = null;
        _gridDetails.DataSource = details;
    }

    private void ReprintSelectedSale()
    {
        try
        {
            if (_gridSales.CurrentRow?.DataBoundItem is not SaleHistoryItem selected)
            {
                MessageBox.Show("Vui lòng chọn hóa đơn cần in lại.", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var sale = _services.SalesService.GetSaleById(selected.SaleID);
            var details = _services.SalesService.GetSaleDetailsForReceipt(selected.SaleID);
            if (sale == null || details.Count == 0)
            {
                MessageBox.Show("Không tìm thấy dữ liệu hóa đơn để in.", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var width = int.TryParse(_cmbPaperWidth.Text, out var parsed) ? parsed : 58;
            _services.ReceiptService.PrintThermalReceipt(
                selected.SaleID,
                details,
                sale.TotalAmount,
                sale.CustomerName,
                sale.IsDebt,
                width,
                _services.AppConfigService.DefaultPrinter);

            _services.AuditService.Log(_services.Session.CurrentUser?.Username, "REPRINT_SALE", $"SaleID={selected.SaleID}");
            MessageBox.Show("Đã gửi lệnh in lại hóa đơn.", "Thành công", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"In lại thất bại: {ex.Message}", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }
}
