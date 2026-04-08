using PMTapHoa.Desktop.Models;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;

namespace PMTapHoa.Desktop.Forms;

public class SalesForm : Form
{
    private const int PreferredLeftMin = 620;
    private const int PreferredRightMin = 420;
    private readonly AppServices _services;
    private readonly BindingList<CartItem> _cartItems = [];
    private readonly TextBox _txtBarcode;
    private readonly TextBox _txtSearchName;
    private readonly TextBox _txtCustomer;
    private readonly CheckBox _chkDebt;
    private readonly DataGridView _grid;
    private readonly Label _lblTotal;
    private readonly Button _btnPayCash;
    private readonly Button _btnPayQr;
    private readonly Button _btnReprintLast;
    private readonly ComboBox _cmbPaperWidth;
    private readonly Label _lblItemCount;
    private readonly Button _btnIncreaseQty;
    private readonly Button _btnDecreaseQty;
    private readonly Button _btnRemoveLine;
    private readonly SplitContainer _mainSplit;
    private int? _lastSaleId;

    public SalesForm(AppServices services)
    {
        _services = services;

        Text = "Màn hình bán hàng (POS)";
        Width = 1360;
        Height = 820;
        MinimumSize = new Size(1240, 740);
        StartPosition = FormStartPosition.CenterParent;
        WindowState = FormWindowState.Normal;
        AutoScaleMode = AutoScaleMode.Font;
        KeyPreview = true;
        BackColor = Color.WhiteSmoke;
        UiStyle.ApplyWindowMode(this);

        var topHeader = new Panel
        {
            Dock = DockStyle.Top,
            Height = 76,
            BackColor = UiStyle.HeaderDark
        };
        var lblTitle = new Label
        {
            Text = "POS BÁN HÀNG",
            ForeColor = Color.White,
            Font = new Font("Segoe UI", 20, FontStyle.Bold),
            AutoSize = true,
            Location = new Point(20, 18)
        };
        var lblShortcut = new Label
        {
            Text = "Phím tắt: F2 Tiền mặt | F4 QR | F3 Tìm nhanh | Esc Đóng",
            ForeColor = Color.Gainsboro,
            Font = new Font("Segoe UI", 11, FontStyle.Regular),
            AutoSize = true,
            Location = new Point(820, 27)
        };
        topHeader.Controls.Add(lblTitle);
        topHeader.Controls.Add(lblShortcut);
        Controls.Add(topHeader);

        _mainSplit = new SplitContainer
        {
            Dock = DockStyle.Fill,
            BackColor = Color.WhiteSmoke
        };
        // Avoid setting PanelMinSize too early (can throw before real size is calculated).
        _mainSplit.Panel1MinSize = 0;
        _mainSplit.Panel2MinSize = 0;
        Controls.Add(_mainSplit);

        var leftPanel = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 3,
            Padding = new Padding(14, 14, 8, 14)
        };
        leftPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 126f));
        leftPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
        leftPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 74f));
        _mainSplit.Panel1.Controls.Add(leftPanel);

        var scanGroup = new GroupBox
        {
            Text = "Quét mã & tìm kiếm",
            Dock = DockStyle.Fill,
            Font = new Font("Segoe UI", 10, FontStyle.Bold)
        };
        leftPanel.Controls.Add(scanGroup, 0, 0);

        var scanLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 6,
            RowCount = 2,
            Padding = new Padding(8)
        };
        scanLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 44f));
        scanLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 36f));
        scanLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 120));
        scanLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 55));
        scanLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 90));
        scanLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 45));
        scanLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 90));
        scanLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 80));
        scanGroup.Controls.Add(scanLayout);

        var lblBarcode = new Label { Text = "Mã vạch:", AutoSize = true, Anchor = AnchorStyles.Left, Font = new Font("Segoe UI", 11, FontStyle.Bold) };
        _txtBarcode = new TextBox { Dock = DockStyle.Fill, Font = new Font("Segoe UI", 15, FontStyle.Bold) };
        _txtBarcode.KeyDown += TxtBarcode_KeyDown;

        var lblSearch = new Label { Text = "Tên hàng:", AutoSize = true, Anchor = AnchorStyles.Left, Font = new Font("Segoe UI", 11, FontStyle.Bold) };
        _txtSearchName = new TextBox { Dock = DockStyle.Fill, Font = new Font("Segoe UI", 12, FontStyle.Regular) };
        _txtSearchName.TextChanged += (_, _) => ApplyNameFilter();
        _txtSearchName.KeyDown += TxtSearchName_KeyDown;

        var btnRefresh = new Button { Text = "Bỏ lọc", Dock = DockStyle.Fill, Font = new Font("Segoe UI", 10, FontStyle.Bold) };
        btnRefresh.Click += (_, _) =>
        {
            _txtSearchName.Clear();
            BindGrid(_cartItems.ToList());
        };

        scanLayout.Controls.Add(lblBarcode, 0, 0);
        scanLayout.Controls.Add(_txtBarcode, 1, 0);
        scanLayout.SetColumnSpan(_txtBarcode, 2);
        scanLayout.Controls.Add(lblSearch, 3, 0);
        scanLayout.Controls.Add(_txtSearchName, 4, 0);
        scanLayout.SetColumnSpan(_txtSearchName, 2);
        var hintScan = new Label { Text = "Enter để thêm nhanh vào giỏ", AutoSize = true, ForeColor = Color.DimGray, Anchor = AnchorStyles.Left, Font = new Font("Segoe UI", 10, FontStyle.Regular) };
        scanLayout.Controls.Add(hintScan, 1, 1);
        scanLayout.SetColumnSpan(hintScan, 3);
        scanLayout.Controls.Add(btnRefresh, 5, 1);

        _grid = new DataGridView
        {
            Dock = DockStyle.Fill,
            ReadOnly = true,
            AllowUserToAddRows = false,
            AutoGenerateColumns = false,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            MultiSelect = false
        };
        _grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = nameof(CartItem.Barcode), HeaderText = "Mã vạch", Width = 140 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = nameof(CartItem.ProductName), HeaderText = "Tên sản phẩm", Width = 340 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = nameof(CartItem.Quantity), HeaderText = "SL", Width = 80 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = nameof(CartItem.UnitPrice), HeaderText = "Đơn giá", Width = 150 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = nameof(CartItem.LineTotal), HeaderText = "Thành tiền", Width = 170 });
        _grid.DataSource = _cartItems;
        _grid.DefaultCellStyle.Font = new Font("Segoe UI", 11, FontStyle.Regular);
        _grid.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 11, FontStyle.Bold);
        _grid.ColumnHeadersHeight = 42;
        _grid.RowTemplate.Height = 38;
        _grid.AlternatingRowsDefaultCellStyle.BackColor = UiStyle.GridAltRow;
        leftPanel.Controls.Add(_grid, 0, 1);

        var quickActions = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.LeftToRight,
            Padding = new Padding(6),
            WrapContents = false
        };
        _btnIncreaseQty = new Button { Text = "Tăng SL (+1)", Width = 150, Height = 44, BackColor = UiStyle.SuccessBright, ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI", 10, FontStyle.Bold) };
        _btnIncreaseQty.Click += (_, _) => AdjustSelectedItemQuantity(1);
        _btnDecreaseQty = new Button { Text = "Giảm SL (-1)", Width = 150, Height = 44, BackColor = UiStyle.Warning, ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI", 10, FontStyle.Bold) };
        _btnDecreaseQty.Click += (_, _) => AdjustSelectedItemQuantity(-1);
        _btnRemoveLine = new Button { Text = "Xóa dòng", Width = 130, Height = 44, BackColor = UiStyle.Danger, ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI", 10, FontStyle.Bold) };
        _btnRemoveLine.Click += (_, _) => RemoveSelectedItem();
        _lblItemCount = new Label
        {
            Text = "Số món: 0",
            AutoSize = true,
            Font = new Font("Segoe UI", 11, FontStyle.Bold),
            Padding = new Padding(18, 10, 0, 0)
        };
        quickActions.Controls.Add(_btnIncreaseQty);
        quickActions.Controls.Add(_btnDecreaseQty);
        quickActions.Controls.Add(_btnRemoveLine);
        quickActions.Controls.Add(_lblItemCount);
        leftPanel.Controls.Add(quickActions, 0, 2);

        var rightPanel = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 4,
            Padding = new Padding(8, 14, 14, 14)
        };
        rightPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 220f));
        rightPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 140f));
        rightPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
        rightPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 180f));
        _mainSplit.Panel2.Controls.Add(rightPanel);

        var customerGroup = new GroupBox
        {
            Text = "Thông tin thanh toán",
            Dock = DockStyle.Fill,
            Font = new Font("Segoe UI", 10, FontStyle.Bold)
        };
        rightPanel.Controls.Add(customerGroup, 0, 0);
        var customerLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 3,
            Padding = new Padding(10)
        };
        customerLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 130f));
        customerLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
        customerLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 52f));
        customerLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 52f));
        customerLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 52f));
        customerGroup.Controls.Add(customerLayout);
        customerLayout.Controls.Add(new Label { Text = "Khách hàng:", AutoSize = true, Anchor = AnchorStyles.Left, Font = new Font("Segoe UI", 11, FontStyle.Bold) }, 0, 0);
        _txtCustomer = new TextBox { Dock = DockStyle.Fill, Font = new Font("Segoe UI", 11, FontStyle.Regular) };
        customerLayout.Controls.Add(_txtCustomer, 1, 0);
        _chkDebt = new CheckBox
        {
            Text = "Ghi nợ",
            AutoSize = true
        };
        _chkDebt.CheckedChanged += (_, _) => UpdateQrButtonState();
        customerLayout.Controls.Add(new Label { Text = "Loại hóa đơn:", AutoSize = true, Anchor = AnchorStyles.Left, Font = new Font("Segoe UI", 11, FontStyle.Bold) }, 0, 1);
        customerLayout.Controls.Add(_chkDebt, 1, 1);
        customerLayout.Controls.Add(new Label { Text = "Khổ in:", AutoSize = true, Anchor = AnchorStyles.Left, Font = new Font("Segoe UI", 11, FontStyle.Bold) }, 0, 2);
        _cmbPaperWidth = new ComboBox
        {
            DropDownStyle = ComboBoxStyle.DropDownList,
            Width = 85,
            Dock = DockStyle.Left
        };
        _cmbPaperWidth.Items.AddRange(["58", "80"]);
        var defaultPaperWidth = _services.AppConfigService.DefaultPaperWidth;
        _cmbPaperWidth.SelectedItem = defaultPaperWidth.ToString();
        customerLayout.Controls.Add(_cmbPaperWidth, 1, 2);

        var totalPanel = new Panel { Dock = DockStyle.Fill, BackColor = UiStyle.PanelSoft };
        _lblTotal = new Label
        {
            Text = "TỔNG TIỀN: 0 VND",
            Font = new Font("Segoe UI", 36, FontStyle.Bold),
            ForeColor = Color.DarkRed,
            AutoSize = true,
            Location = new Point(18, 36)
        };
        totalPanel.Controls.Add(_lblTotal);
        rightPanel.Controls.Add(totalPanel, 0, 1);

        rightPanel.Controls.Add(new Panel { Dock = DockStyle.Fill }, 0, 2);

        var payPanel = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 3,
            Padding = new Padding(0)
        };
        payPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 34f));
        payPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 33f));
        payPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 33f));
        _btnPayCash = new Button
        {
            Text = "F2 - Thanh toán tiền mặt",
            Dock = DockStyle.Fill,
            Font = new Font("Segoe UI", 14, FontStyle.Bold),
            BackColor = UiStyle.Success,
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            MinimumSize = new Size(0, 52),
            Margin = new Padding(0, 3, 0, 3)
        };
        _btnPayCash.Click += (_, _) => ProcessPayment(useQrFlow: false);

        _btnPayQr = new Button
        {
            Text = "F4 - Thanh toán QR",
            Dock = DockStyle.Fill,
            Font = new Font("Segoe UI", 14, FontStyle.Bold),
            BackColor = UiStyle.AccentPurple,
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            MinimumSize = new Size(0, 52),
            Margin = new Padding(0, 3, 0, 3)
        };
        _btnPayQr.Click += (_, _) => ProcessPayment(useQrFlow: true);

        _btnReprintLast = new Button
        {
            Text = "In lại HĐ gần nhất",
            Dock = DockStyle.Fill,
            Font = new Font("Segoe UI", 14, FontStyle.Bold),
            BackColor = UiStyle.Primary,
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            MinimumSize = new Size(0, 52),
            Margin = new Padding(0, 3, 0, 3),
            Enabled = false
        };
        _btnReprintLast.Click += (_, _) => ReprintLastSale();
        payPanel.Controls.Add(_btnPayCash, 0, 0);
        payPanel.Controls.Add(_btnPayQr, 0, 1);
        payPanel.Controls.Add(_btnReprintLast, 0, 2);
        rightPanel.Controls.Add(payPanel, 0, 3);

        KeyDown += SalesForm_KeyDown;
        Shown += (_, _) => BeginInvoke(AdjustSplitLayout);
        Resize += (_, _) => AdjustSplitLayout();
        UpdateTotalLabel();
        UpdateQrButtonState();
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

    private void ProcessPayment(bool useQrFlow)
    {
        try
        {
            if (_cartItems.Count == 0)
            {
                MessageBox.Show("Giỏ hàng trống.", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var total = _cartItems.Sum(i => i.LineTotal);
            if (useQrFlow && !_chkDebt.Checked && !ShowQrAndConfirmPayment(total))
            {
                return;
            }

            var saleId = _services.SalesService.SaveSale(_cartItems.ToList(), _txtCustomer.Text.Trim(), _chkDebt.Checked);
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
            _txtSearchName.Clear();
            UpdateTotalLabel();
            BindGrid(_cartItems.ToList());
            UpdateQrButtonState();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Thanh toán thất bại: {ex.Message}", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private bool ShowQrAndConfirmPayment(decimal total)
    {
        if (!_services.QrPaymentService.IsConfigured(out _))
        {
            return true;
        }

        var transferContent = _services.QrPaymentService.BuildTransferContent();
        using var qrImage = _services.QrPaymentService.GenerateVietQrImage(total, transferContent);
        using var qrForm = new QrDisplayForm(qrImage, total, transferContent);
        qrForm.ShowOnBestScreen();

        var confirmation = MessageBox.Show(
            this,
            "Đã nhận thanh toán QR từ khách chưa?",
            "Xác nhận thanh toán QR",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Question);

        qrForm.Close();
        return confirmation == DialogResult.Yes;
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
            ProcessPayment(useQrFlow: false);
            e.Handled = true;
        }
        else if (e.KeyCode == Keys.F4)
        {
            ProcessPayment(useQrFlow: true);
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
        if (string.IsNullOrWhiteSpace(_txtSearchName.Text))
        {
            BindGrid(_cartItems.ToList());
            return;
        }

        ApplyNameFilter();
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

    private void AdjustSelectedItemQuantity(int delta)
    {
        if (_grid.CurrentRow?.DataBoundItem is not CartItem item)
        {
            return;
        }

        item.Quantity += delta;
        if (item.Quantity <= 0)
        {
            _cartItems.Remove(item);
        }

        RebindCart();
        UpdateTotalLabel();
    }

    private void RemoveSelectedItem()
    {
        if (_grid.CurrentRow?.DataBoundItem is not CartItem item)
        {
            return;
        }

        _cartItems.Remove(item);
        RebindCart();
        UpdateTotalLabel();
    }

    private void UpdateQrButtonState()
    {
        var isDebt = _chkDebt.Checked;
        _btnPayQr.Enabled = !isDebt;
        _btnPayQr.Text = isDebt ? "F4 - QR (không áp dụng cho ghi nợ)" : "F4 - Thanh toán QR";
    }

    private void AdjustSplitLayout()
    {
        if (!IsHandleCreated || _mainSplit.IsDisposed)
        {
            return;
        }

        var containerWidth = _mainSplit.ClientSize.Width;
        if (containerWidth <= 0)
        {
            return;
        }

        var splitterWidth = Math.Max(0, _mainSplit.SplitterWidth);
        var minLeft = Math.Min(PreferredLeftMin, Math.Max(160, (containerWidth - splitterWidth) / 2));
        var minRight = Math.Min(PreferredRightMin, Math.Max(220, (containerWidth - splitterWidth) / 3));

        var maxLeft = containerWidth - splitterWidth - minRight;
        if (maxLeft < minLeft)
        {
            return;
        }

        var desiredLeft = (int)(containerWidth * 0.62);
        desiredLeft = Math.Max(minLeft, Math.Min(maxLeft, desiredLeft));

        if (_mainSplit.SplitterDistance == desiredLeft)
        {
            return;
        }

        try
        {
            _mainSplit.SplitterDistance = desiredLeft;
        }
        catch (ArgumentException)
        {
            // Ignore transient resize states where WinForms updates bounds asynchronously.
        }
    }
}
