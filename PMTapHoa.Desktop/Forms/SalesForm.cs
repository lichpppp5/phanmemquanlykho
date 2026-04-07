using PMTapHoa.Desktop.Models;
using System.ComponentModel;
using System.Diagnostics;

namespace PMTapHoa.Desktop.Forms;

public class SalesForm : Form
{
    private readonly AppServices _services;
    private readonly BindingList<CartItem> _cartItems = [];
    private readonly TextBox _txtBarcode;
    private readonly TextBox _txtSearchName;
    private readonly TextBox _txtCustomer;
    private readonly CheckBox _chkDebt;
    private readonly DataGridView _grid;
    private readonly Label _lblTotal;
    private readonly Button _btnPay;
    private readonly Button _btnReprintLast;
    private readonly ComboBox _cmbPaperWidth;
    private int? _lastSaleId;

    public SalesForm(AppServices services)
    {
        _services = services;

        Text = "Màn hình bán hàng (POS)";
        Width = 1000;
        Height = 650;
        StartPosition = FormStartPosition.CenterParent;
        KeyPreview = true;

        var lblBarcode = new Label
        {
            Text = "Quét mã vạch:",
            AutoSize = true,
            Location = new Point(20, 20)
        };

        _txtBarcode = new TextBox
        {
            Width = 280,
            Location = new Point(120, 16)
        };
        _txtBarcode.KeyDown += TxtBarcode_KeyDown;

        var lblSearch = new Label
        {
            Text = "Tìm tên (F3):",
            AutoSize = true,
            Location = new Point(430, 20)
        };

        _txtSearchName = new TextBox
        {
            Width = 240,
            Location = new Point(515, 16)
        };
        _txtSearchName.TextChanged += (_, _) => ApplyNameFilter();
        _txtSearchName.KeyDown += TxtSearchName_KeyDown;

        var btnRefresh = new Button
        {
            Text = "Bỏ lọc",
            Width = 90,
            Location = new Point(770, 15)
        };
        btnRefresh.Click += (_, _) =>
        {
            _txtSearchName.Clear();
            BindGrid(_cartItems.ToList());
        };

        _grid = new DataGridView
        {
            Location = new Point(20, 60),
            Width = 940,
            Height = 420,
            ReadOnly = true,
            AllowUserToAddRows = false,
            AutoGenerateColumns = false
        };
        _grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = nameof(CartItem.Barcode), HeaderText = "Mã vạch", Width = 140 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = nameof(CartItem.ProductName), HeaderText = "Tên sản phẩm", Width = 320 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = nameof(CartItem.Quantity), HeaderText = "SL", Width = 80 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = nameof(CartItem.UnitPrice), HeaderText = "Đơn giá", Width = 140 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = nameof(CartItem.LineTotal), HeaderText = "Thành tiền", Width = 160 });
        _grid.DataSource = _cartItems;

        var lblCustomer = new Label
        {
            Text = "Khách hàng:",
            AutoSize = true,
            Location = new Point(20, 505)
        };
        _txtCustomer = new TextBox
        {
            Width = 220,
            Location = new Point(100, 501)
        };

        _chkDebt = new CheckBox
        {
            Text = "Ghi nợ",
            AutoSize = true,
            Location = new Point(340, 503)
        };

        var lblPaper = new Label
        {
            Text = "Khổ in:",
            AutoSize = true,
            Location = new Point(430, 505)
        };
        _cmbPaperWidth = new ComboBox
        {
            DropDownStyle = ComboBoxStyle.DropDownList,
            Location = new Point(485, 501),
            Width = 85
        };
        _cmbPaperWidth.Items.AddRange(["58", "80"]);
        var defaultPaperWidth = _services.AppConfigService.DefaultPaperWidth;
        _cmbPaperWidth.SelectedItem = defaultPaperWidth.ToString();

        _lblTotal = new Label
        {
            Text = "TỔNG TIỀN: 0 VND",
            Font = new Font("Segoe UI", 16, FontStyle.Bold),
            ForeColor = Color.DarkRed,
            AutoSize = true,
            Location = new Point(540, 495)
        };

        _btnPay = new Button
        {
            Text = "F2 - Thanh toán",
            Width = 180,
            Height = 45,
            Location = new Point(780, 545)
        };
        _btnPay.Click += (_, _) => PayAndPrint();

        _btnReprintLast = new Button
        {
            Text = "In lại HĐ gần nhất",
            Width = 180,
            Height = 45,
            Location = new Point(590, 545),
            Enabled = false
        };
        _btnReprintLast.Click += (_, _) => ReprintLastSale();

        var info = new Label
        {
            Text = "Phím tắt: F2 lưu+in | F3 focus tìm tên | Esc đóng cửa sổ",
            AutoSize = true,
            Location = new Point(20, 560)
        };

        Controls.Add(lblBarcode);
        Controls.Add(_txtBarcode);
        Controls.Add(lblSearch);
        Controls.Add(_txtSearchName);
        Controls.Add(btnRefresh);
        Controls.Add(_grid);
        Controls.Add(lblCustomer);
        Controls.Add(_txtCustomer);
        Controls.Add(_chkDebt);
        Controls.Add(lblPaper);
        Controls.Add(_cmbPaperWidth);
        Controls.Add(_lblTotal);
        Controls.Add(_btnReprintLast);
        Controls.Add(_btnPay);
        Controls.Add(info);

        KeyDown += SalesForm_KeyDown;
    }

    private void TxtBarcode_KeyDown(object? sender, KeyEventArgs e)
    {
        if (e.KeyCode == Keys.Enter)
        {
            e.Handled = true;
            e.SuppressKeyPress = true;
            AddByBarcode(_txtBarcode.Text.Trim());
        }
    }

    private void AddByBarcode(string barcode)
    {
        if (string.IsNullOrWhiteSpace(barcode))
        {
            return;
        }

        try
        {
            var product = _services.ProductService.GetByBarcode(barcode);
            if (product == null)
            {
                MessageBox.Show("Mã vạch không tồn tại trong hệ thống.", "Không tìm thấy", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                _txtBarcode.SelectAll();
                return;
            }

            AddProductToCart(product);
            UpdateTotalLabel();
            _txtBarcode.Clear();
            _txtBarcode.Focus();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Lỗi khi quét mã vạch: {ex.Message}", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void TxtSearchName_KeyDown(object? sender, KeyEventArgs e)
    {
        if (e.KeyCode != Keys.Enter)
        {
            return;
        }

        e.Handled = true;
        e.SuppressKeyPress = true;

        var keyword = _txtSearchName.Text.Trim();
        if (string.IsNullOrWhiteSpace(keyword))
        {
            return;
        }

        try
        {
            var product = _services.ProductService.Search(keyword).FirstOrDefault();
            if (product == null)
            {
                MessageBox.Show("Không tìm thấy sản phẩm theo tên.", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            AddProductToCart(product);
            UpdateTotalLabel();
            _txtSearchName.Clear();
            _txtBarcode.Focus();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Lỗi tìm kiếm sản phẩm: {ex.Message}", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void PayAndPrint()
    {
        try
        {
            if (_cartItems.Count == 0)
            {
                MessageBox.Show("Giỏ hàng trống.", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var saleId = _services.SalesService.SaveSale(_cartItems.ToList(), _txtCustomer.Text.Trim(), _chkDebt.Checked);
            var total = _cartItems.Sum(i => i.LineTotal);
            var receiptPath = _services.ReceiptService.GenerateTempReceiptFile(
                saleId,
                _cartItems.ToList(),
                total,
                _txtCustomer.Text.Trim(),
                _chkDebt.Checked);

            try
            {
                var paperWidth = int.TryParse(_cmbPaperWidth.Text, out var selectedWidth) ? selectedWidth : 58;
                _services.ReceiptService.PrintThermalReceipt(
                    saleId,
                    _cartItems.ToList(),
                    total,
                    _txtCustomer.Text.Trim(),
                    _chkDebt.Checked,
                    paperWidth,
                    _services.AppConfigService.DefaultPrinter);
            }
            catch (Exception printEx)
            {
                MessageBox.Show($"Đã lưu hóa đơn nhưng in thất bại: {printEx.Message}", "Cảnh báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }

            OpenFileForPrintPreview(receiptPath);
            MessageBox.Show($"Đã lưu hóa đơn #{saleId}.", "Thành công", MessageBoxButtons.OK, MessageBoxIcon.Information);
            _lastSaleId = saleId;
            _btnReprintLast.Enabled = true;
            _services.AuditService.Log(_services.Session.CurrentUser?.Username, "CREATE_SALE", $"SaleID={saleId}, Total={total}");

            _cartItems.Clear();
            _txtCustomer.Clear();
            _chkDebt.Checked = false;
            UpdateTotalLabel();
            BindGrid(_cartItems.ToList());
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Thanh toán thất bại: {ex.Message}", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void ReprintLastSale()
    {
        try
        {
            if (!_lastSaleId.HasValue)
            {
                MessageBox.Show("Chưa có hóa đơn nào để in lại.", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var sale = _services.SalesService.GetSaleById(_lastSaleId.Value);
            var details = _services.SalesService.GetSaleDetailsForReceipt(_lastSaleId.Value);
            if (sale == null || details.Count == 0)
            {
                MessageBox.Show("Không tìm thấy dữ liệu hóa đơn gần nhất.", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var paperWidth = int.TryParse(_cmbPaperWidth.Text, out var selectedWidth) ? selectedWidth : 58;
            _services.ReceiptService.PrintThermalReceipt(
                sale.SaleID,
                details,
                sale.TotalAmount,
                sale.CustomerName,
                sale.IsDebt,
                paperWidth,
                _services.AppConfigService.DefaultPrinter);

            _services.AuditService.Log(_services.Session.CurrentUser?.Username, "REPRINT_LAST_SALE", $"SaleID={sale.SaleID}");
            MessageBox.Show("Đã gửi lệnh in lại hóa đơn gần nhất.", "Thành công", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"In lại thất bại: {ex.Message}", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private static void OpenFileForPrintPreview(string filePath)
    {
        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = filePath,
                UseShellExecute = true
            });
        }
        catch
        {
            // Nếu không mở được file tự động, bỏ qua vì file vẫn đã được tạo.
        }
    }

    private void SalesForm_KeyDown(object? sender, KeyEventArgs e)
    {
        if (e.KeyCode == Keys.F2)
        {
            PayAndPrint();
            e.Handled = true;
        }
        else if (e.KeyCode == Keys.F3)
        {
            _txtSearchName.Focus();
            _txtSearchName.SelectAll();
            e.Handled = true;
        }
        else if (e.KeyCode == Keys.Escape)
        {
            Close();
        }
    }

    private void ApplyNameFilter()
    {
        var keyword = _txtSearchName.Text.Trim();
        if (string.IsNullOrWhiteSpace(keyword))
        {
            BindGrid(_cartItems.ToList());
            return;
        }

        var filtered = _cartItems
            .Where(i => i.ProductName.Contains(keyword, StringComparison.CurrentCultureIgnoreCase))
            .ToList();
        BindGrid(filtered);
    }

    private void UpdateTotalLabel()
    {
        var total = _cartItems.Sum(i => i.LineTotal);
        _lblTotal.Text = $"TỔNG TIỀN: {total:N0} VND";
    }

    private void RebindCart()
    {
        BindGrid(_cartItems.ToList());
    }

    private void AddProductToCart(Product product)
    {
        var existing = _cartItems.FirstOrDefault(x => x.ProductID == product.ProductID);
        if (existing != null)
        {
            existing.Quantity += 1;
        }
        else
        {
            _cartItems.Add(new CartItem
            {
                ProductID = product.ProductID,
                Barcode = product.Barcode ?? string.Empty,
                ProductName = product.ProductName,
                Quantity = 1,
                UnitPrice = product.SellingPrice
            });
        }

        RebindCart();
    }

    private void BindGrid(List<CartItem> items)
    {
        _grid.DataSource = null;
        _grid.DataSource = new BindingList<CartItem>(items);
    }
}
