using PMTapHoa.Desktop.Models;
using PMTapHoa.Desktop.Services;
using System.ComponentModel;
using System.Diagnostics;

namespace PMTapHoa.Desktop.Forms;

public class SalesForm : Form
{
    private const int PreferredLeftMin = 620;
    private const int PreferredRightMin = 540;
    private readonly AppServices _services;
    private readonly BindingList<CartItem> _cartItems = [];
    private readonly TextBox _txtBarcode;
    private readonly TextBox _txtSearchName;
    private readonly TextBox _txtCustomer;
    private readonly TextBox _txtCustomerPhone;
    private readonly CheckBox _chkDebt;
    private readonly DataGridView _grid;
    private readonly Label _lblTotal;
    private readonly Label _lblChange;
    private readonly Label _lblSubtotal;
    private readonly TextBox _txtCashReceived;
    private readonly NumericUpDown _numDiscount;
    private readonly ComboBox _cmbDiscountType;
    private readonly Label _lblDiscountAmt;
    private readonly Button _btnPayCash;
    private readonly Button _btnPayQr;
    private readonly Button _btnReprintLast;
    private readonly Button _btnClearCart;
    private readonly ComboBox _cmbPaperWidth;
    private readonly Label _lblItemCount;
    private readonly Button _btnIncreaseQty;
    private readonly Button _btnDecreaseQty;
    private readonly Button _btnRemoveLine;
    private readonly SplitContainer _mainSplit;
    private int? _lastSaleId;
    private decimal _currentDiscountAmount;
    private string? _currentDiscountNote;

    public SalesForm(AppServices services)
    {
        _services = services;

        Text = "Màn hình bán hàng (POS)";
        Width = 1400;
        Height = 860;
        MinimumSize = new Size(1260, 760);
        StartPosition = FormStartPosition.CenterParent;
        WindowState = FormWindowState.Normal;
        AutoScaleMode = AutoScaleMode.Font;
        KeyPreview = true;
        BackColor = UiStyle.Background;
        UiStyle.ApplyWindowMode(this);

        // ── Header ────────────────────────────────────────────────────────────
        var topHeader = new Panel
        {
            Dock = DockStyle.Top,
            Height = 68,
            BackColor = UiStyle.HeaderDark
        };
        var headerLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 1,
            Padding = new Padding(18, 0, 18, 0)
        };
        headerLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
        headerLayout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        var lblTitle = new Label
        {
            Text = "🛒  POS BÁN HÀNG",
            ForeColor = Color.White,
            Font = new Font("Segoe UI", 19, FontStyle.Bold),
            AutoSize = true,
            Anchor = AnchorStyles.Left
        };
        var lblShortcut = new Label
        {
            Text = "F2 Tiền mặt  |  F4 QR  |  F3 Tìm nhanh  |  Del Xóa dòng  |  Esc Đóng",
            ForeColor = Color.FromArgb(160, 200, 230),
            Font = new Font("Segoe UI", 10, FontStyle.Regular),
            AutoSize = true,
            Anchor = AnchorStyles.Right
        };
        headerLayout.Controls.Add(lblTitle, 0, 0);
        headerLayout.Controls.Add(lblShortcut, 1, 0);
        topHeader.Controls.Add(headerLayout);
        Controls.Add(topHeader);

        _mainSplit = new SplitContainer
        {
            Dock = DockStyle.Fill,
            SplitterWidth = 8,
            BackColor = UiStyle.Background
        };
        _mainSplit.Panel1MinSize = 0;
        _mainSplit.Panel2MinSize = 0;
        Controls.Add(_mainSplit);

        // ══════════════════ LEFT PANEL ══════════════════════════════════════════
        var leftPanel = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 3,
            Padding = new Padding(14, 14, 8, 14)
        };
        leftPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 118f));
        leftPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
        leftPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 68f));
        _mainSplit.Panel1.Controls.Add(leftPanel);

        // Scan group
        var scanGroup = new GroupBox
        {
            Text = "🔍  Quét mã & tìm kiếm sản phẩm",
            Dock = DockStyle.Fill,
            Font = new Font("Segoe UI", 10, FontStyle.Bold),
            ForeColor = UiStyle.AccentBlueDark
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
        scanLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 90));
        scanLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 55));
        scanLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 20));
        scanLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 45));
        scanLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 80));
        scanLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 110));
        scanGroup.Controls.Add(scanLayout);

        var lblBarcode = new Label { Text = "Mã vạch:", AutoSize = true, Anchor = AnchorStyles.Left, Font = new Font("Segoe UI", 11, FontStyle.Bold) };
        _txtBarcode = new TextBox { Dock = DockStyle.Fill, Font = new Font("Segoe UI", 15, FontStyle.Bold) };
        _txtBarcode.KeyDown += TxtBarcode_KeyDown;

        var lblSearch = new Label { Text = "Tên hàng:", AutoSize = true, Anchor = AnchorStyles.Left, Font = new Font("Segoe UI", 11, FontStyle.Bold) };
        _txtSearchName = new TextBox { Dock = DockStyle.Fill, Font = new Font("Segoe UI", 12, FontStyle.Regular) };
        _txtSearchName.TextChanged += (_, _) => ApplyNameFilter();
        _txtSearchName.KeyDown += TxtSearchName_KeyDown;

        var btnClearSearch = new Button { Text = "Bỏ lọc", Dock = DockStyle.Fill, Font = new Font("Segoe UI", 10, FontStyle.Bold) };
        btnClearSearch.Click += (_, _) =>
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
        var hintScan = new Label { Text = "Enter để thêm nhanh vào giỏ hàng", AutoSize = true, ForeColor = Color.DimGray, Anchor = AnchorStyles.Left, Font = new Font("Segoe UI", 10) };
        scanLayout.Controls.Add(hintScan, 1, 1);
        scanLayout.SetColumnSpan(hintScan, 3);
        scanLayout.Controls.Add(btnClearSearch, 5, 1);

        // Cart grid
        _grid = new DataGridView
        {
            Dock = DockStyle.Fill,
            ReadOnly = true,
            AllowUserToAddRows = false,
            AutoGenerateColumns = false,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            MultiSelect = false,
            BorderStyle = BorderStyle.None,
            CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal,
            GridColor = Color.FromArgb(220, 225, 235)
        };
        _grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = nameof(CartItem.Barcode), HeaderText = "Mã vạch", Width = 130 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = nameof(CartItem.ProductName), HeaderText = "Tên sản phẩm", Width = 320 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = nameof(CartItem.Quantity), HeaderText = "SL", Width = 65, DefaultCellStyle = { Alignment = DataGridViewContentAlignment.MiddleCenter } });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = nameof(CartItem.UnitPrice), HeaderText = "Đơn giá", Width = 130, DefaultCellStyle = { Alignment = DataGridViewContentAlignment.MiddleRight, Format = "N0" } });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = nameof(CartItem.LineTotal), HeaderText = "Thành tiền", Width = 150, DefaultCellStyle = { Alignment = DataGridViewContentAlignment.MiddleRight, Format = "N0" } });
        _grid.DataSource = _cartItems;
        _grid.DefaultCellStyle.Font = new Font("Segoe UI", 11, FontStyle.Regular);
        _grid.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 11, FontStyle.Bold);
        _grid.ColumnHeadersDefaultCellStyle.BackColor = UiStyle.HeaderDark;
        _grid.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
        _grid.EnableHeadersVisualStyles = false;
        _grid.ColumnHeadersHeight = 42;
        _grid.RowTemplate.Height = 38;
        _grid.AlternatingRowsDefaultCellStyle.BackColor = UiStyle.GridAltRow;
        leftPanel.Controls.Add(_grid, 0, 1);

        // Quick actions
        var quickActions = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.LeftToRight,
            Padding = new Padding(6),
            WrapContents = false
        };
        _btnIncreaseQty = new Button { Text = "▲  +1", Width = 110, Height = 46, BackColor = UiStyle.SuccessBright, ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI", 10, FontStyle.Bold) };
        _btnIncreaseQty.FlatAppearance.BorderSize = 0;
        _btnIncreaseQty.Click += (_, _) => AdjustSelectedItemQuantity(1);
        _btnDecreaseQty = new Button { Text = "▼  -1", Width = 110, Height = 46, BackColor = UiStyle.Warning, ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI", 10, FontStyle.Bold) };
        _btnDecreaseQty.FlatAppearance.BorderSize = 0;
        _btnDecreaseQty.Click += (_, _) => AdjustSelectedItemQuantity(-1);
        _btnRemoveLine = new Button { Text = "✕  Xóa dòng", Width = 130, Height = 46, BackColor = UiStyle.Danger, ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI", 10, FontStyle.Bold) };
        _btnRemoveLine.FlatAppearance.BorderSize = 0;
        _btnRemoveLine.Click += (_, _) => RemoveSelectedItem();
        _btnClearCart = new Button { Text = "🗑  Xóa tất cả", Width = 140, Height = 46, BackColor = Color.FromArgb(127, 140, 141), ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI", 10, FontStyle.Bold) };
        _btnClearCart.FlatAppearance.BorderSize = 0;
        _btnClearCart.Click += (_, _) => ClearCart();
        _lblItemCount = new Label
        {
            Text = "Số món: 0",
            AutoSize = true,
            Font = new Font("Segoe UI", 11, FontStyle.Bold),
            Padding = new Padding(14, 10, 0, 0)
        };
        quickActions.Controls.AddRange(new Control[] { _btnIncreaseQty, _btnDecreaseQty, _btnRemoveLine, _btnClearCart, _lblItemCount });
        leftPanel.Controls.Add(quickActions, 0, 2);

        // ══════════════════ RIGHT PANEL ═════════════════════════════════════════
        var rightPanel = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 5,
            Padding = new Padding(8, 14, 14, 14)
        };
        rightPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 210f)); // Customer info
        rightPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 116f)); // Discount
        rightPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 130f)); // Total
        rightPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 90f));  // Cash received
        rightPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));  // Pay buttons
        _mainSplit.Panel2.Controls.Add(rightPanel);

        // Customer info
        var customerGroup = new GroupBox
        {
            Text = "👤  Thông tin khách hàng",
            Dock = DockStyle.Fill,
            Font = new Font("Segoe UI", 10, FontStyle.Bold),
            ForeColor = UiStyle.AccentTeal
        };
        rightPanel.Controls.Add(customerGroup, 0, 0);
        var customerLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 4,
            Padding = new Padding(10)
        };
        customerLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 100f));
        customerLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
        for (var i = 0; i < 4; i++)
            customerLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 44f));
        customerGroup.Controls.Add(customerLayout);

        customerLayout.Controls.Add(new Label { Text = "Tên khách:", AutoSize = true, Anchor = AnchorStyles.Left, Font = new Font("Segoe UI", 10, FontStyle.Bold) }, 0, 0);
        _txtCustomer = new TextBox { Dock = DockStyle.Fill, Font = new Font("Segoe UI", 11) };
        customerLayout.Controls.Add(_txtCustomer, 1, 0);

        customerLayout.Controls.Add(new Label { Text = "SĐT:", AutoSize = true, Anchor = AnchorStyles.Left, Font = new Font("Segoe UI", 10, FontStyle.Bold) }, 0, 1);
        _txtCustomerPhone = new TextBox { Dock = DockStyle.Fill, Font = new Font("Segoe UI", 11) };
        _txtCustomerPhone.Leave += TxtPhone_Leave;
        customerLayout.Controls.Add(_txtCustomerPhone, 1, 1);

        customerLayout.Controls.Add(new Label { Text = "Loại HĐ:", AutoSize = true, Anchor = AnchorStyles.Left, Font = new Font("Segoe UI", 10, FontStyle.Bold) }, 0, 2);
        _chkDebt = new CheckBox { Text = "📌 Ghi nợ", AutoSize = true, Font = new Font("Segoe UI", 10, FontStyle.Regular) };
        _chkDebt.CheckedChanged += (_, _) => UpdateQrButtonState();
        customerLayout.Controls.Add(_chkDebt, 1, 2);

        customerLayout.Controls.Add(new Label { Text = "Khổ in:", AutoSize = true, Anchor = AnchorStyles.Left, Font = new Font("Segoe UI", 10, FontStyle.Bold) }, 0, 3);
        _cmbPaperWidth = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 100, Dock = DockStyle.Left };
        _cmbPaperWidth.Items.AddRange(["58", "80"]);
        _cmbPaperWidth.SelectedItem = _services.AppConfigService.DefaultPaperWidth.ToString();
        customerLayout.Controls.Add(_cmbPaperWidth, 1, 3);

        // Discount panel
        var discountGroup = new GroupBox
        {
            Text = "🏷️  Giảm giá",
            Dock = DockStyle.Fill,
            Font = new Font("Segoe UI", 10, FontStyle.Bold),
            ForeColor = UiStyle.AccentOrange
        };
        rightPanel.Controls.Add(discountGroup, 0, 1);
        var discountLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 4,
            RowCount = 2,
            Padding = new Padding(10, 8, 10, 0)
        };
        discountLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 60f));
        discountLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 120f));
        discountLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 110f));
        discountLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
        discountLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 44f));
        discountLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 28f));
        discountGroup.Controls.Add(discountLayout);

        discountLayout.Controls.Add(new Label { Text = "Kiểu:", AutoSize = true, Anchor = AnchorStyles.Left, Font = new Font("Segoe UI", 10, FontStyle.Bold) }, 0, 0);
        _cmbDiscountType = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Dock = DockStyle.Fill, Font = new Font("Segoe UI", 10) };
        _cmbDiscountType.Items.AddRange(["Không giảm", "Theo % (phần trăm)", "Theo số tiền VND"]);
        _cmbDiscountType.SelectedIndex = 0;
        _cmbDiscountType.SelectedIndexChanged += (_, _) => RecalcDiscount();
        discountLayout.Controls.Add(_cmbDiscountType, 1, 0);

        _numDiscount = new NumericUpDown
        {
            Dock = DockStyle.Fill,
            Font = new Font("Segoe UI", 11, FontStyle.Bold),
            Minimum = 0, Maximum = 100_000_000, DecimalPlaces = 0, ThousandsSeparator = true
        };
        _numDiscount.ValueChanged += (_, _) => RecalcDiscount();
        discountLayout.Controls.Add(_numDiscount, 2, 0);

        _lblDiscountAmt = new Label
        {
            Text = "Giảm: 0 VND",
            AutoSize = true,
            Font = new Font("Segoe UI", 10, FontStyle.Regular),
            ForeColor = UiStyle.AccentOrange,
            Anchor = AnchorStyles.Left,
            Padding = new Padding(6, 0, 0, 0)
        };
        discountLayout.Controls.Add(_lblDiscountAmt, 3, 0);

        var lblDiscHint = new Label
        {
            Text = "Chọn kiểu giảm giá → Nhập giá trị",
            ForeColor = Color.Gray,
            Font = new Font("Segoe UI", 8.5f),
            AutoSize = true
        };
        discountLayout.Controls.Add(lblDiscHint, 0, 1);
        discountLayout.SetColumnSpan(lblDiscHint, 4);

        // Total panel
        var totalPanel = new Panel { Dock = DockStyle.Fill, BackColor = UiStyle.HeaderDark, Padding = new Padding(14, 8, 14, 8) };
        _lblSubtotal = new Label
        {
            Text = "Tạm tính: 0 VND",
            Font = new Font("Segoe UI", 12, FontStyle.Regular),
            ForeColor = Color.FromArgb(180, 210, 240),
            AutoSize = true,
            Location = new Point(14, 8)
        };
        _lblTotal = new Label
        {
            Text = "TỔNG THANH TOÁN: 0 VND",
            Font = new Font("Segoe UI", 19, FontStyle.Bold),
            ForeColor = Color.FromArgb(255, 200, 60),
            AutoSize = true,
            Location = new Point(14, 38)
        };
        totalPanel.Controls.Add(_lblSubtotal);
        totalPanel.Controls.Add(_lblTotal);
        rightPanel.Controls.Add(totalPanel, 0, 2);

        // Cash received
        var cashGroup = new GroupBox
        {
            Text = "💵  Tiền khách đưa & Tiền thối",
            Dock = DockStyle.Fill,
            Font = new Font("Segoe UI", 10, FontStyle.Bold),
            ForeColor = UiStyle.Success
        };
        rightPanel.Controls.Add(cashGroup, 0, 3);
        var cashLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 4,
            RowCount = 1,
            Padding = new Padding(10, 6, 10, 6)
        };
        cashLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 100f));
        cashLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 45f));
        cashLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 70f));
        cashLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 55f));
        cashGroup.Controls.Add(cashLayout);
        cashLayout.Controls.Add(new Label { Text = "KH đưa:", AutoSize = true, Anchor = AnchorStyles.Left, Font = new Font("Segoe UI", 10, FontStyle.Bold) }, 0, 0);
        _txtCashReceived = new TextBox { Dock = DockStyle.Fill, Font = new Font("Segoe UI", 13, FontStyle.Bold), Text = "0" };
        _txtCashReceived.TextChanged += (_, _) => UpdateChangeLabel();
        cashLayout.Controls.Add(_txtCashReceived, 1, 0);
        cashLayout.Controls.Add(new Label { Text = "Tiền thối:", AutoSize = true, Anchor = AnchorStyles.Left, Font = new Font("Segoe UI", 10, FontStyle.Bold) }, 2, 0);
        _lblChange = new Label
        {
            Text = "0 VND",
            Font = new Font("Segoe UI", 14, FontStyle.Bold),
            ForeColor = UiStyle.SuccessBright,
            AutoSize = true,
            Anchor = AnchorStyles.Left,
            Padding = new Padding(6, 4, 0, 0)
        };
        cashLayout.Controls.Add(_lblChange, 3, 0);

        // Pay buttons
        var payPanel = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 3,
            Padding = new Padding(0, 4, 0, 0)
        };
        payPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 34f));
        payPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 33f));
        payPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 33f));

        _btnPayCash = new Button
        {
            Text = "F2  –  THANH TOÁN TIỀN MẶT",
            Dock = DockStyle.Fill,
            Font = new Font("Segoe UI", 14, FontStyle.Bold),
            BackColor = UiStyle.Success,
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            MinimumSize = new Size(0, 52),
            Margin = new Padding(0, 3, 0, 3)
        };
        _btnPayCash.FlatAppearance.BorderSize = 0;
        _btnPayCash.Click += (_, _) => ProcessPayment(useQrFlow: false);

        _btnPayQr = new Button
        {
            Text = "F4  –  THANH TOÁN QR CODE",
            Dock = DockStyle.Fill,
            Font = new Font("Segoe UI", 14, FontStyle.Bold),
            BackColor = UiStyle.AccentPurple,
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            MinimumSize = new Size(0, 52),
            Margin = new Padding(0, 3, 0, 3)
        };
        _btnPayQr.FlatAppearance.BorderSize = 0;
        _btnPayQr.Click += (_, _) => ProcessPayment(useQrFlow: true);

        _btnReprintLast = new Button
        {
            Text = "🖨  In lại hóa đơn gần nhất",
            Dock = DockStyle.Fill,
            Font = new Font("Segoe UI", 13, FontStyle.Bold),
            BackColor = UiStyle.Primary,
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            MinimumSize = new Size(0, 52),
            Margin = new Padding(0, 3, 0, 3),
            Enabled = false
        };
        _btnReprintLast.FlatAppearance.BorderSize = 0;
        _btnReprintLast.Click += (_, _) => ReprintLastSale();
        payPanel.Controls.Add(_btnPayCash, 0, 0);
        payPanel.Controls.Add(_btnPayQr, 0, 1);
        payPanel.Controls.Add(_btnReprintLast, 0, 2);
        rightPanel.Controls.Add(payPanel, 0, 4);

        KeyDown += SalesForm_KeyDown;
        Shown += (_, _) => BeginInvoke(AdjustSplitLayout);
        Resize += (_, _) => AdjustSplitLayout();
        UpdateTotalLabel();
        UpdateQrButtonState();
    }

    // ── Phone auto-fill ────────────────────────────────────────────────────────
    private void TxtPhone_Leave(object? sender, EventArgs e)
    {
        var phone = _txtCustomerPhone.Text.Trim();
        if (string.IsNullOrWhiteSpace(phone)) return;
        var customer = _services.CustomerService.GetByPhone(phone);
        if (customer != null && string.IsNullOrWhiteSpace(_txtCustomer.Text))
        {
            _txtCustomer.Text = customer.CustomerName;
        }
    }

    // ── Barcode ────────────────────────────────────────────────────────────────
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
        if (string.IsNullOrWhiteSpace(barcode)) return;
        try
        {
            var product = _services.ProductService.GetByBarcode(barcode);
            if (product == null)
            {
                MessageBox.Show("Mã vạch không tồn tại trong hệ thống.", "Không tìm thấy", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                _txtBarcode.SelectAll();
                return;
            }

            WarnLowStockIfNeeded(product);
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
        if (e.KeyCode != Keys.Enter) return;
        e.Handled = true;
        e.SuppressKeyPress = true;

        var keyword = _txtSearchName.Text.Trim();
        if (string.IsNullOrWhiteSpace(keyword)) return;
        try
        {
            var product = _services.ProductService.Search(keyword).FirstOrDefault();
            if (product == null)
            {
                MessageBox.Show("Không tìm thấy sản phẩm theo tên.", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            WarnLowStockIfNeeded(product);
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

    private static void WarnLowStockIfNeeded(Models.Product product)
    {
        if (product.StockQuantity <= product.MinStock && product.StockQuantity > 0)
        {
            MessageBox.Show(
                $"⚠️  Sản phẩm '{product.ProductName}' đang sắp hết hàng!\n" +
                $"Tồn kho: {product.StockQuantity:0.#}  |  Mức tối thiểu: {product.MinStock}",
                "Cảnh báo tồn kho",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
        }
    }

    // ── Discount ───────────────────────────────────────────────────────────────
    private void RecalcDiscount()
    {
        var subtotal = _cartItems.Sum(i => i.LineTotal);
        var type = _cmbDiscountType.SelectedIndex switch
        {
            1 => DiscountService.DiscountType.Percent,
            2 => DiscountService.DiscountType.FixedAmount,
            _ => DiscountService.DiscountType.None
        };

        var maxVal = type == DiscountService.DiscountType.Percent ? 100 : (decimal)subtotal;
        _numDiscount.Enabled = type != DiscountService.DiscountType.None;
        if (type == DiscountService.DiscountType.Percent)
        {
            _numDiscount.Maximum = 100;
            _numDiscount.DecimalPlaces = 1;
        }
        else
        {
            _numDiscount.Maximum = (decimal)Math.Max(subtotal, 1);
            _numDiscount.DecimalPlaces = 0;
        }

        var result = DiscountService.Calculate(subtotal, type, _numDiscount.Value);
        _currentDiscountAmount = result.DiscountAmount;
        _currentDiscountNote = result.Note;
        _lblDiscountAmt.Text = _currentDiscountAmount > 0
            ? $"Giảm: {_currentDiscountAmount:N0} VND"
            : "Không giảm giá";
        UpdateTotalLabel();
    }

    // ── Payment ────────────────────────────────────────────────────────────────
    private void ProcessPayment(bool useQrFlow)
    {
        try
        {
            if (_cartItems.Count == 0)
            {
                MessageBox.Show("Giỏ hàng trống.", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var subtotal = _cartItems.Sum(i => i.LineTotal);
            var total = DiscountService.FinalAmount(subtotal, _currentDiscountAmount);

            if (useQrFlow && !_chkDebt.Checked && !ShowQrAndConfirmPayment(total))
                return;

            // Lưu/lấy khách hàng
            int? customerId = null;
            var customerName = _txtCustomer.Text.Trim();
            var customerPhone = _txtCustomerPhone.Text.Trim();
            if (!string.IsNullOrWhiteSpace(customerPhone))
            {
                var existing = _services.CustomerService.GetByPhone(customerPhone);
                if (existing != null)
                {
                    customerId = existing.CustomerID;
                    if (string.IsNullOrWhiteSpace(customerName)) customerName = existing.CustomerName;
                }
                else if (!string.IsNullOrWhiteSpace(customerName))
                {
                    customerId = _services.CustomerService.Create(new Models.Customer
                    {
                        CustomerName = customerName,
                        Phone = customerPhone
                    });
                }
            }

            var saleId = _services.SalesService.SaveSale(
                _cartItems.ToList(), customerName, _chkDebt.Checked,
                _currentDiscountAmount, _currentDiscountNote, customerId);

            var receiptPath = _services.ReceiptService.GenerateTempReceiptFile(
                saleId, _cartItems.ToList(), total, customerName, _chkDebt.Checked,
                _currentDiscountAmount, _currentDiscountNote,
                _services.AppConfigService.StoreName,
                _services.AppConfigService.StoreAddress,
                _services.AppConfigService.StorePhone,
                _services.AppConfigService.ReceiptFooter);

            try
            {
                var paperWidth = int.TryParse(_cmbPaperWidth.Text, out var w) ? w : 58;
                _services.ReceiptService.PrintThermalReceipt(
                    saleId, _cartItems.ToList(), total, customerName, _chkDebt.Checked, paperWidth,
                    _services.AppConfigService.DefaultPrinter,
                    _currentDiscountAmount, _currentDiscountNote,
                    _services.AppConfigService.StoreName,
                    _services.AppConfigService.StoreAddress,
                    _services.AppConfigService.StorePhone,
                    _services.AppConfigService.ReceiptFooter);
            }
            catch (Exception printEx)
            {
                MessageBox.Show($"Đã lưu hóa đơn nhưng in thất bại: {printEx.Message}", "Cảnh báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }

            OpenFileForPrintPreview(receiptPath);

            var changeText = "";
            if (decimal.TryParse(_txtCashReceived.Text, out var cash) && cash > 0 && cash >= total)
                changeText = $"\n💵 Tiền thối: {(cash - total):N0} VND";

            MessageBox.Show($"✅ Đã lưu hóa đơn #{saleId}.{changeText}", "Thành công", MessageBoxButtons.OK, MessageBoxIcon.Information);
            _lastSaleId = saleId;
            _btnReprintLast.Enabled = true;
            _services.AuditService.Log(_services.Session.CurrentUser?.Username, "CREATE_SALE", $"SaleID={saleId}, Total={total}");

            _cartItems.Clear();
            _txtCustomer.Clear();
            _txtCustomerPhone.Clear();
            _chkDebt.Checked = false;
            _txtSearchName.Clear();
            _txtCashReceived.Text = "0";
            _numDiscount.Value = 0;
            _cmbDiscountType.SelectedIndex = 0;
            _currentDiscountAmount = 0;
            _currentDiscountNote = null;
            UpdateTotalLabel();
            BindGrid(_cartItems.ToList());
            UpdateQrButtonState();
            _txtBarcode.Focus();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Thanh toán thất bại: {ex.Message}", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private bool ShowQrAndConfirmPayment(decimal total)
    {
        if (!_services.QrPaymentService.IsConfigured(out _)) return true;
        var transferContent = _services.QrPaymentService.BuildTransferContent();
        using var qrImage = _services.QrPaymentService.GenerateVietQrImage(total, transferContent);
        using var qrForm = new QrDisplayForm(qrImage, total, transferContent);
        return qrForm.ShowOnBestScreen(this) == DialogResult.OK;
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

            var paperWidth = int.TryParse(_cmbPaperWidth.Text, out var w) ? w : 58;
            _services.ReceiptService.PrintThermalReceipt(
                sale.SaleID, details, sale.TotalAmount, sale.CustomerName, sale.IsDebt, paperWidth,
                _services.AppConfigService.DefaultPrinter,
                sale.DiscountAmount, sale.DiscountNote,
                _services.AppConfigService.StoreName,
                _services.AppConfigService.StoreAddress,
                _services.AppConfigService.StorePhone,
                _services.AppConfigService.ReceiptFooter);

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
        try { Process.Start(new ProcessStartInfo { FileName = filePath, UseShellExecute = true }); }
        catch { /* ignore */ }
    }

    private void SalesForm_KeyDown(object? sender, KeyEventArgs e)
    {
        if (e.KeyCode == Keys.F2) { ProcessPayment(useQrFlow: false); e.Handled = true; }
        else if (e.KeyCode == Keys.F4) { ProcessPayment(useQrFlow: true); e.Handled = true; }
        else if (e.KeyCode == Keys.F3) { _txtSearchName.Focus(); _txtSearchName.SelectAll(); e.Handled = true; }
        else if (e.KeyCode == Keys.Delete) { RemoveSelectedItem(); e.Handled = true; }
        else if (e.KeyCode == Keys.Escape) Close();
    }

    private void ApplyNameFilter()
    {
        var keyword = _txtSearchName.Text.Trim();
        if (string.IsNullOrWhiteSpace(keyword)) { BindGrid(_cartItems.ToList()); return; }
        var filtered = _cartItems.Where(i => i.ProductName.Contains(keyword, StringComparison.CurrentCultureIgnoreCase)).ToList();
        BindGrid(filtered);
    }

    private void UpdateTotalLabel()
    {
        var subtotal = _cartItems.Sum(i => i.LineTotal);
        var total = DiscountService.FinalAmount(subtotal, _currentDiscountAmount);
        _lblSubtotal.Text = _currentDiscountAmount > 0 ? $"Tạm tính: {subtotal:N0} VND" : string.Empty;
        _lblTotal.Text = $"TỔNG THANH TOÁN: {total:N0} VND";
        _lblItemCount.Text = $"Số món: {_cartItems.Sum(i => i.Quantity)}";
        UpdateChangeLabel();
    }

    private void UpdateChangeLabel()
    {
        var subtotal = _cartItems.Sum(i => i.LineTotal);
        var total = DiscountService.FinalAmount(subtotal, _currentDiscountAmount);
        if (decimal.TryParse(_txtCashReceived.Text.Replace(",", "").Trim(), out var cash) && cash > 0)
        {
            var change = cash - total;
            _lblChange.Text = change >= 0 ? $"{change:N0} VND" : "Chưa đủ!";
            _lblChange.ForeColor = change >= 0 ? UiStyle.SuccessBright : UiStyle.Danger;
        }
        else
        {
            _lblChange.Text = "–";
            _lblChange.ForeColor = Color.Gray;
        }
    }

    private void RebindCart()
    {
        if (string.IsNullOrWhiteSpace(_txtSearchName.Text)) BindGrid(_cartItems.ToList());
        else ApplyNameFilter();
    }

    private void AddProductToCart(Models.Product product)
    {
        var existing = _cartItems.FirstOrDefault(x => x.ProductID == product.ProductID);
        if (existing != null) existing.Quantity += 1;
        else _cartItems.Add(new CartItem
        {
            ProductID = product.ProductID,
            Barcode = product.Barcode ?? string.Empty,
            ProductName = product.ProductName,
            Quantity = 1,
            UnitPrice = product.SellingPrice
        });
        RebindCart();
    }

    private void BindGrid(List<CartItem> items)
    {
        _grid.DataSource = null;
        _grid.DataSource = new BindingList<CartItem>(items);
    }

    private void AdjustSelectedItemQuantity(int delta)
    {
        if (_grid.CurrentRow?.DataBoundItem is not CartItem item) return;
        item.Quantity += delta;
        if (item.Quantity <= 0) _cartItems.Remove(item);
        RebindCart();
        UpdateTotalLabel();
    }

    private void RemoveSelectedItem()
    {
        if (_grid.CurrentRow?.DataBoundItem is not CartItem item) return;
        _cartItems.Remove(item);
        RebindCart();
        UpdateTotalLabel();
    }

    private void ClearCart()
    {
        if (_cartItems.Count == 0) return;
        if (MessageBox.Show("Xóa toàn bộ giỏ hàng?", "Xác nhận", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
            return;
        _cartItems.Clear();
        _numDiscount.Value = 0;
        _cmbDiscountType.SelectedIndex = 0;
        _currentDiscountAmount = 0;
        BindGrid(_cartItems.ToList());
        UpdateTotalLabel();
        _txtBarcode.Focus();
    }

    private void UpdateQrButtonState()
    {
        var isDebt = _chkDebt.Checked;
        _btnPayQr.Enabled = !isDebt;
        _btnPayQr.Text = isDebt ? "F4 – QR (không áp dụng cho ghi nợ)" : "F4  –  THANH TOÁN QR CODE";
    }

    private void AdjustSplitLayout()
    {
        if (!IsHandleCreated || _mainSplit.IsDisposed) return;
        var containerWidth = _mainSplit.ClientSize.Width;
        if (containerWidth <= 0) return;
        var splitterWidth = Math.Max(0, _mainSplit.SplitterWidth);
        var minLeft = Math.Min(PreferredLeftMin, Math.Max(160, (containerWidth - splitterWidth) / 2));
        var minRight = Math.Min(PreferredRightMin, Math.Max(380, (containerWidth - splitterWidth) / 2));
        var maxLeft = containerWidth - splitterWidth - minRight;
        if (maxLeft < minLeft) return;
        var desiredLeft = (int)(containerWidth * 0.54);
        desiredLeft = Math.Max(minLeft, Math.Min(maxLeft, desiredLeft));
        if (_mainSplit.SplitterDistance == desiredLeft) return;
        try { _mainSplit.SplitterDistance = desiredLeft; }
        catch (ArgumentException) { }
    }
}
