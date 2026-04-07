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
        Width = 1150;
        Height = 720;
        StartPosition = FormStartPosition.CenterParent;
        KeyPreview = true;

        var lblKeyword = new Label { Text = "Tìm HĐ/khách:", AutoSize = true, Location = new Point(20, 20) };
        _txtKeyword = new TextBox { Width = 180, Location = new Point(110, 16) };

        var lblFrom = new Label { Text = "Từ:", AutoSize = true, Location = new Point(310, 20) };
        _dtFrom = new DateTimePicker { Location = new Point(340, 16), Width = 130, Format = DateTimePickerFormat.Short, ShowCheckBox = true };

        var lblTo = new Label { Text = "Đến:", AutoSize = true, Location = new Point(490, 20) };
        _dtTo = new DateTimePicker { Location = new Point(530, 16), Width = 130, Format = DateTimePickerFormat.Short, ShowCheckBox = true };

        var btnFilter = new Button { Text = "Lọc", Width = 90, Location = new Point(680, 15) };
        btnFilter.Click += (_, _) => LoadSales();

        _gridSales = new DataGridView
        {
            Location = new Point(20, 55),
            Width = 1090,
            Height = 300,
            ReadOnly = true,
            AllowUserToAddRows = false,
            AutoGenerateColumns = false
        };
        _gridSales.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = nameof(SaleHistoryItem.SaleID), HeaderText = "Hóa đơn", Width = 90 });
        _gridSales.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = nameof(SaleHistoryItem.SaleDate), HeaderText = "Ngày bán", Width = 180 });
        _gridSales.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = nameof(SaleHistoryItem.CustomerName), HeaderText = "Khách hàng", Width = 300 });
        _gridSales.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = nameof(SaleHistoryItem.TotalAmount), HeaderText = "Tổng tiền", Width = 160 });
        _gridSales.Columns.Add(new DataGridViewCheckBoxColumn { DataPropertyName = nameof(SaleHistoryItem.IsDebt), HeaderText = "Ghi nợ", Width = 90 });
        _gridSales.SelectionChanged += (_, _) => LoadSaleDetails();

        _gridDetails = new DataGridView
        {
            Location = new Point(20, 375),
            Width = 1090,
            Height = 220,
            ReadOnly = true,
            AllowUserToAddRows = false,
            AutoGenerateColumns = false
        };
        _gridDetails.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = nameof(CartItem.Barcode), HeaderText = "Mã vạch", Width = 180 });
        _gridDetails.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = nameof(CartItem.ProductName), HeaderText = "Tên sản phẩm", Width = 430 });
        _gridDetails.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = nameof(CartItem.Quantity), HeaderText = "SL", Width = 100 });
        _gridDetails.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = nameof(CartItem.UnitPrice), HeaderText = "Đơn giá", Width = 180 });
        _gridDetails.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = nameof(CartItem.LineTotal), HeaderText = "Thành tiền", Width = 180 });

        var lblPaper = new Label { Text = "Khổ in:", AutoSize = true, Location = new Point(20, 620) };
        _cmbPaperWidth = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 80, Location = new Point(70, 616) };
        _cmbPaperWidth.Items.AddRange(["58", "80"]);
        _cmbPaperWidth.SelectedItem = _services.AppConfigService.DefaultPaperWidth.ToString();

        var btnReprint = new Button { Text = "In lại hóa đơn", Width = 150, Location = new Point(170, 614) };
        btnReprint.Click += (_, _) => ReprintSelectedSale();

        Controls.Add(lblKeyword);
        Controls.Add(_txtKeyword);
        Controls.Add(lblFrom);
        Controls.Add(_dtFrom);
        Controls.Add(lblTo);
        Controls.Add(_dtTo);
        Controls.Add(btnFilter);
        Controls.Add(_gridSales);
        Controls.Add(_gridDetails);
        Controls.Add(lblPaper);
        Controls.Add(_cmbPaperWidth);
        Controls.Add(btnReprint);

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
            var from = _dtFrom.Checked ? _dtFrom.Value.Date : null;
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
