using PMTapHoa.Desktop.Models;

namespace PMTapHoa.Desktop.Forms;

public class InventoryForm : Form
{
    private readonly AppServices _services;
    private readonly DataGridView _grid;
    private readonly TextBox _txtSearch;
    private readonly NumericUpDown _numImportQty;
    private readonly TextBox _txtCostPrice;
    private readonly TextBox _txtBarcode;
    private readonly TextBox _txtName;
    private readonly TextBox _txtCategory;
    private readonly TextBox _txtUnit;
    private readonly NumericUpDown _numCost;
    private readonly NumericUpDown _numPrice;
    private readonly NumericUpDown _numStock;
    private readonly NumericUpDown _numMinStock;
    private readonly NumericUpDown _numScanQty;
    private readonly Label _lblScanStatus;
    private readonly DateTimePicker _dtExpiry;
    private readonly Button _btnAdd;
    private readonly Button _btnUpdate;
    private readonly Button _btnDelete;
    private int? _selectedProductId;
    private List<Product> _products = [];

    public InventoryForm(AppServices services)
    {
        _services = services;

        Text = "Quản lý kho hàng";
        Width = 1300;
        Height = 860;
        MinimumSize = new Size(1220, 780);
        StartPosition = FormStartPosition.CenterParent;
        WindowState = FormWindowState.Normal;
        AutoScroll = false;
        AutoScaleMode = AutoScaleMode.Font;
        KeyPreview = true;
        BackColor = Color.WhiteSmoke;
        UiStyle.ApplyWindowMode(this);

        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 4,
            Padding = new Padding(14)
        };
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 54f));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 54f));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 30f));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 16f));
        Controls.Add(root);

        var searchPanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            Padding = new Padding(4, 10, 4, 4)
        };
        searchPanel.Controls.Add(new Label { Text = "Tìm nhanh (tên/mã vạch):", AutoSize = true, Margin = new Padding(0, 8, 10, 0) });
        _txtSearch = new TextBox
        {
            Width = 360,
            Font = new Font("Segoe UI", 11, FontStyle.Regular),
            Margin = new Padding(0, 4, 0, 0)
        };
        _txtSearch.TextChanged += (_, _) => LoadGrid();
        searchPanel.Controls.Add(_txtSearch);
        root.Controls.Add(searchPanel, 0, 0);

        _grid = new DataGridView
        {
            Dock = DockStyle.Fill,
            ReadOnly = true,
            AllowUserToAddRows = false,
            AutoGenerateColumns = false
        };
        _grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = nameof(Product.ProductID), HeaderText = "ID", Width = 55 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = nameof(Product.Barcode), HeaderText = "Mã vạch", Width = 140 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = nameof(Product.ProductName), HeaderText = "Tên sản phẩm", Width = 280 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = nameof(Product.CategoryName), HeaderText = "Danh mục", Width = 130 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = nameof(Product.Unit), HeaderText = "ĐVT", Width = 70 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = nameof(Product.StockQuantity), HeaderText = "Tồn", Width = 90 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = nameof(Product.MinStock), HeaderText = "Mức tối thiểu", Width = 110 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = nameof(Product.CostPrice), HeaderText = "Giá nhập", Width = 100 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = nameof(Product.SellingPrice), HeaderText = "Giá bán", Width = 100 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = nameof(Product.ExpiryDate), HeaderText = "HSD", Width = 120 });
        _grid.Columns[_grid.Columns.Count - 1].AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
        _grid.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 11, FontStyle.Bold);
        _grid.ColumnHeadersHeight = 40;
        _grid.DefaultCellStyle.Font = new Font("Segoe UI", 11, FontStyle.Regular);
        _grid.RowTemplate.Height = 36;
        _grid.AlternatingRowsDefaultCellStyle.BackColor = UiStyle.GridAltRow;
        _grid.RowPrePaint += Grid_RowPrePaint;
        _grid.SelectionChanged += (_, _) => LoadSelectedProductToEditor();
        root.Controls.Add(_grid, 0, 1);

        var editorPanel = new GroupBox
        {
            Text = "CRUD sản phẩm",
            Dock = DockStyle.Fill,
            Font = new Font("Segoe UI", 11, FontStyle.Bold)
        };
        root.Controls.Add(editorPanel, 0, 2);

        var editorLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 10,
            RowCount = 4,
            Padding = new Padding(10, 12, 10, 8)
        };
        editorLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 72f));
        editorLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 24f));
        editorLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 48f));
        editorLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 24f));
        editorLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 72f));
        editorLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 20f));
        editorLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 48f));
        editorLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 12f));
        editorLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 44f));
        editorLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 20f));
        editorLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 44f));
        editorLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 44f));
        editorLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 44f));
        editorLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
        editorPanel.Controls.Add(editorLayout);

        var lblBarcode = new Label { Text = "Mã vạch:", AutoSize = true, Anchor = AnchorStyles.Left };
        _txtBarcode = new TextBox { Dock = DockStyle.Fill, Font = new Font("Segoe UI", 11, FontStyle.Regular) };
        _txtBarcode.KeyDown += TxtBarcode_KeyDown;
        var lblName = new Label { Text = "Tên:", AutoSize = true, Anchor = AnchorStyles.Left };
        _txtName = new TextBox { Dock = DockStyle.Fill, Font = new Font("Segoe UI", 11, FontStyle.Regular) };
        _txtName.KeyDown += TxtName_KeyDown;
        var lblCategory = new Label { Text = "Danh mục:", AutoSize = true, Anchor = AnchorStyles.Left };
        _txtCategory = new TextBox { Dock = DockStyle.Fill, Font = new Font("Segoe UI", 11, FontStyle.Regular) };
        var lblUnit = new Label { Text = "ĐVT:", AutoSize = true, Anchor = AnchorStyles.Left };
        _txtUnit = new TextBox { Dock = DockStyle.Fill, Font = new Font("Segoe UI", 11, FontStyle.Regular) };

        var lblCostValue = new Label { Text = "Giá nhập:", AutoSize = true, Anchor = AnchorStyles.Left };
        _numCost = new NumericUpDown { Dock = DockStyle.Fill, DecimalPlaces = 2, Maximum = 1000000000, Font = new Font("Segoe UI", 11, FontStyle.Regular) };
        var lblPrice = new Label { Text = "Giá bán:", AutoSize = true, Anchor = AnchorStyles.Left };
        _numPrice = new NumericUpDown { Dock = DockStyle.Fill, DecimalPlaces = 2, Maximum = 1000000000, Font = new Font("Segoe UI", 11, FontStyle.Regular) };
        var lblStock = new Label { Text = "Tồn kho:", AutoSize = true, Anchor = AnchorStyles.Left };
        _numStock = new NumericUpDown { Dock = DockStyle.Fill, DecimalPlaces = 2, Maximum = 1000000, Font = new Font("Segoe UI", 11, FontStyle.Regular) };
        var lblMin = new Label { Text = "Min:", AutoSize = true, Anchor = AnchorStyles.Left };
        _numMinStock = new NumericUpDown { Dock = DockStyle.Fill, Maximum = 1000000, Value = 5, Font = new Font("Segoe UI", 11, FontStyle.Regular) };
        var lblExpiry = new Label { Text = "HSD:", AutoSize = true, Anchor = AnchorStyles.Left };
        _dtExpiry = new DateTimePicker { Dock = DockStyle.Fill, Format = DateTimePickerFormat.Short, ShowCheckBox = true, Font = new Font("Segoe UI", 11, FontStyle.Regular) };

        var lblScanQty = new Label { Text = "SL quét:", AutoSize = true, Anchor = AnchorStyles.Left };
        _numScanQty = new NumericUpDown
        {
            Dock = DockStyle.Left,
            Width = 120,
            DecimalPlaces = 2,
            Maximum = 100000,
            Minimum = 1,
            Value = 1,
            Font = new Font("Segoe UI", 11, FontStyle.Regular)
        };
        _numScanQty.KeyDown += NumScanQty_KeyDown;

        _lblScanStatus = new Label
        {
            Text = "Quét mã -> Enter, nhập tên (nếu mới), nhập SL -> Enter để lưu nhanh.",
            AutoSize = true,
            ForeColor = Color.DimGray,
            Anchor = AnchorStyles.Left
        };

        _btnAdd = new Button { Text = "Thêm", Width = 120, Height = 38, BackColor = UiStyle.SuccessBright, ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI", 10, FontStyle.Bold) };
        _btnUpdate = new Button { Text = "Sửa", Width = 120, Height = 38, BackColor = UiStyle.Primary, ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI", 10, FontStyle.Bold) };
        _btnDelete = new Button { Text = "Xóa", Width = 120, Height = 38, BackColor = UiStyle.Danger, ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI", 10, FontStyle.Bold) };
        var btnClear = new Button { Text = "Làm mới form", Width = 145, Height = 38, BackColor = UiStyle.Neutral, ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI", 10, FontStyle.Bold) };

        _btnAdd.Click += (_, _) => CreateProduct();
        _btnUpdate.Click += (_, _) => UpdateProduct();
        _btnDelete.Click += (_, _) => DeleteProduct();
        btnClear.Click += (_, _) => ResetEditor();

        editorLayout.Controls.Add(lblBarcode, 0, 0);
        editorLayout.Controls.Add(_txtBarcode, 1, 0);
        editorLayout.Controls.Add(lblName, 2, 0);
        editorLayout.Controls.Add(_txtName, 3, 0);
        editorLayout.SetColumnSpan(_txtName, 3);
        editorLayout.Controls.Add(lblCategory, 6, 0);
        editorLayout.Controls.Add(_txtCategory, 7, 0);
        editorLayout.SetColumnSpan(_txtCategory, 3);

        editorLayout.Controls.Add(lblCostValue, 0, 1);
        editorLayout.Controls.Add(_numCost, 1, 1);
        editorLayout.Controls.Add(lblPrice, 2, 1);
        editorLayout.Controls.Add(_numPrice, 3, 1);
        editorLayout.Controls.Add(lblStock, 4, 1);
        editorLayout.Controls.Add(_numStock, 5, 1);
        editorLayout.Controls.Add(lblMin, 6, 1);
        editorLayout.Controls.Add(_numMinStock, 7, 1);
        editorLayout.Controls.Add(lblExpiry, 8, 1);
        editorLayout.Controls.Add(_dtExpiry, 9, 1);

        editorLayout.Controls.Add(lblUnit, 0, 2);
        editorLayout.Controls.Add(_txtUnit, 1, 2);
        editorLayout.Controls.Add(lblScanQty, 2, 2);
        editorLayout.Controls.Add(_numScanQty, 3, 2);
        editorLayout.Controls.Add(_lblScanStatus, 4, 2);
        editorLayout.SetColumnSpan(_lblScanStatus, 6);

        var editorButtonPanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = true,
            Margin = new Padding(0, 0, 0, 0)
        };
        editorButtonPanel.Controls.Add(_btnAdd);
        editorButtonPanel.Controls.Add(_btnUpdate);
        editorButtonPanel.Controls.Add(_btnDelete);
        editorButtonPanel.Controls.Add(btnClear);
        editorLayout.Controls.Add(editorButtonPanel, 0, 3);
        editorLayout.SetColumnSpan(editorButtonPanel, 10);

        var importPanel = new GroupBox
        {
            Text = "Nhập hàng nhanh",
            Dock = DockStyle.Fill,
            Font = new Font("Segoe UI", 11, FontStyle.Bold)
        };
        root.Controls.Add(importPanel, 0, 3);

        var importLayout = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = true,
            Padding = new Padding(10, 10, 10, 8)
        };
        importPanel.Controls.Add(importLayout);

        var lblQty = new Label
        {
            Text = "Số lượng cộng thêm:",
            AutoSize = true,
            Margin = new Padding(0, 10, 8, 0)
        };
        _numImportQty = new NumericUpDown
        {
            Width = 110,
            DecimalPlaces = 2,
            Maximum = 100000,
            Font = new Font("Segoe UI", 11, FontStyle.Regular),
            Margin = new Padding(0, 4, 12, 0)
        };

        var lblCost = new Label
        {
            Text = "Giá nhập mới (tuỳ chọn):",
            AutoSize = true,
            Margin = new Padding(0, 10, 8, 0)
        };
        _txtCostPrice = new TextBox
        {
            Width = 160,
            Font = new Font("Segoe UI", 11, FontStyle.Regular),
            Margin = new Padding(0, 4, 12, 0)
        };

        var btnImport = new Button
        {
            Text = "Cập nhật nhập hàng",
            Width = 170,
            BackColor = UiStyle.AccentBlueDark,
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Height = 38,
            Font = new Font("Segoe UI", 10, FontStyle.Bold),
            Margin = new Padding(0, 4, 10, 0)
        };
        btnImport.Click += (_, _) => QuickImportSelectedProduct();

        var btnRestock = new Button
        {
            Text = "Ghi mối nhập hàng",
            Width = 160,
            BackColor = UiStyle.Warning,
            ForeColor = Color.Black,
            FlatStyle = FlatStyle.Flat,
            Height = 38,
            Font = new Font("Segoe UI", 10, FontStyle.Bold),
            Margin = new Padding(0, 4, 0, 0)
        };
        btnRestock.Click += (_, _) => OpenRestockForSelectedProduct();

        if (!_services.Session.IsAdmin)
        {
            _btnUpdate.Enabled = false;
            _btnDelete.Enabled = false;
            btnRestock.Enabled = false;
        }

        importLayout.Controls.Add(lblQty);
        importLayout.Controls.Add(_numImportQty);
        importLayout.Controls.Add(lblCost);
        importLayout.Controls.Add(_txtCostPrice);
        importLayout.Controls.Add(btnImport);
        importLayout.Controls.Add(btnRestock);

        Load += InventoryForm_Load;
        KeyDown += InventoryForm_KeyDown;
    }

    private void InventoryForm_Load(object? sender, EventArgs e)
    {
        LoadGrid();
        ShowLowStockReminderIfNeeded();
    }

    private void LoadGrid()
    {
        var keyword = _txtSearch.Text.Trim();
        _products = string.IsNullOrWhiteSpace(keyword)
            ? _services.ProductService.GetAll()
            : _services.ProductService.Search(keyword);

        _grid.DataSource = null;
        _grid.DataSource = _products;
    }

    private void LoadSelectedProductToEditor()
    {
        if (_grid.CurrentRow?.DataBoundItem is not Product selected)
        {
            return;
        }

        _selectedProductId = selected.ProductID;
        _txtBarcode.Text = selected.Barcode ?? string.Empty;
        _txtName.Text = selected.ProductName;
        _txtCategory.Text = selected.CategoryName ?? string.Empty;
        _txtUnit.Text = selected.Unit ?? string.Empty;
        _numCost.Value = SafeDecimalToUpDown(selected.CostPrice, _numCost.Maximum);
        _numPrice.Value = SafeDecimalToUpDown(selected.SellingPrice, _numPrice.Maximum);
        _numStock.Value = SafeDecimalToUpDown((decimal)selected.StockQuantity, _numStock.Maximum);
        _numMinStock.Value = SafeDecimalToUpDown(selected.MinStock <= 0 ? 0 : selected.MinStock, _numMinStock.Maximum);
        if (selected.ExpiryDate.HasValue)
        {
            _dtExpiry.Checked = true;
            _dtExpiry.Value = selected.ExpiryDate.Value;
        }
        else
        {
            _dtExpiry.Checked = false;
        }
    }

    private void Grid_RowPrePaint(object? sender, DataGridViewRowPrePaintEventArgs e)
    {
        if (e.RowIndex < 0 || e.RowIndex >= _grid.Rows.Count)
        {
            return;
        }

        var row = _grid.Rows[e.RowIndex];
        if (row.DataBoundItem is Product product && product.StockQuantity < product.MinStock)
        {
            row.DefaultCellStyle.BackColor = Color.MistyRose;
            row.DefaultCellStyle.ForeColor = Color.DarkRed;
        }
        else
        {
            row.DefaultCellStyle.BackColor = Color.White;
            row.DefaultCellStyle.ForeColor = Color.Black;
        }
    }

    private void QuickImportSelectedProduct()
    {
        try
        {
            if (_grid.CurrentRow?.DataBoundItem is not Product selected)
            {
                MessageBox.Show("Vui lòng chọn sản phẩm cần nhập hàng.", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var qtyToAdd = (double)_numImportQty.Value;
            if (qtyToAdd <= 0)
            {
                MessageBox.Show("Số lượng phải lớn hơn 0.", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            decimal? newCost = null;
            if (!string.IsNullOrWhiteSpace(_txtCostPrice.Text))
            {
                if (!decimal.TryParse(_txtCostPrice.Text.Trim(), out var parsed))
                {
                    MessageBox.Show("Giá nhập mới không hợp lệ.", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                newCost = parsed;
            }

            _services.ProductService.QuickImportStock(selected.ProductID, qtyToAdd, newCost);
            _services.AuditService.Log(
                _services.Session.CurrentUser?.Username,
                "QUICK_IMPORT_STOCK",
                $"ProductID={selected.ProductID}, Qty={qtyToAdd}, NewCost={(newCost.HasValue ? newCost.Value.ToString() : "null")}");
            MessageBox.Show("Đã cập nhật nhập hàng.", "Thành công", MessageBoxButtons.OK, MessageBoxIcon.Information);
            LoadGrid();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Lỗi nhập hàng: {ex.Message}", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void CreateProduct()
    {
        try
        {
            var product = BuildProductModelFromEditor();
            var id = _services.ProductService.CreateProduct(product, _txtCategory.Text);
            _services.AuditService.Log(_services.Session.CurrentUser?.Username, "CREATE_PRODUCT", $"ProductID={id}, Name={product.ProductName}");
            MessageBox.Show($"Đã thêm sản phẩm ID={id}.", "Thành công", MessageBoxButtons.OK, MessageBoxIcon.Information);
            LoadGrid();
            ResetEditor();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Lỗi thêm sản phẩm: {ex.Message}", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void UpdateProduct()
    {
        try
        {
            if (!_selectedProductId.HasValue)
            {
                MessageBox.Show("Vui lòng chọn sản phẩm để sửa.", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var product = BuildProductModelFromEditor();
            product.ProductID = _selectedProductId.Value;
            _services.ProductService.UpdateProduct(product, _txtCategory.Text);
            _services.AuditService.Log(_services.Session.CurrentUser?.Username, "UPDATE_PRODUCT", $"ProductID={product.ProductID}, Name={product.ProductName}");
            MessageBox.Show("Đã cập nhật sản phẩm.", "Thành công", MessageBoxButtons.OK, MessageBoxIcon.Information);
            LoadGrid();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Lỗi cập nhật sản phẩm: {ex.Message}", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void DeleteProduct()
    {
        try
        {
            if (!_selectedProductId.HasValue)
            {
                MessageBox.Show("Vui lòng chọn sản phẩm để xóa.", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var confirm = MessageBox.Show("Bạn chắc chắn muốn xóa sản phẩm này?", "Xác nhận", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (confirm != DialogResult.Yes)
            {
                return;
            }

            _services.ProductService.DeleteProduct(_selectedProductId.Value);
            _services.AuditService.Log(_services.Session.CurrentUser?.Username, "DELETE_PRODUCT", $"ProductID={_selectedProductId.Value}");
            MessageBox.Show("Đã xóa sản phẩm.", "Thành công", MessageBoxButtons.OK, MessageBoxIcon.Information);
            LoadGrid();
            ResetEditor();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Không thể xóa sản phẩm: {ex.Message}", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private Product BuildProductModelFromEditor()
    {
        return new Product
        {
            Barcode = _txtBarcode.Text.Trim(),
            ProductName = _txtName.Text.Trim(),
            Unit = _txtUnit.Text.Trim(),
            CostPrice = _numCost.Value,
            SellingPrice = _numPrice.Value,
            StockQuantity = (double)_numStock.Value,
            MinStock = (int)_numMinStock.Value,
            ExpiryDate = _dtExpiry.Checked ? _dtExpiry.Value.Date : null
        };
    }

    private void ResetEditor()
    {
        _selectedProductId = null;
        _txtBarcode.Clear();
        _txtName.Clear();
        _txtCategory.Clear();
        _txtUnit.Clear();
        _numCost.Value = 0;
        _numPrice.Value = 0;
        _numStock.Value = 0;
        _numMinStock.Value = 5;
        _numScanQty.Value = 1;
        _dtExpiry.Checked = false;
        _lblScanStatus.Text = "Quét mã -> Enter, nhập tên (nếu mới), nhập SL -> Enter để lưu nhanh.";
    }

    private static decimal SafeDecimalToUpDown(decimal value, decimal max)
    {
        if (value < 0)
        {
            return 0;
        }

        if (value > max)
        {
            return max;
        }

        return value;
    }

    private void InventoryForm_KeyDown(object? sender, KeyEventArgs e)
    {
        if (e.KeyCode == Keys.Escape)
        {
            Close();
        }
    }

    private void TxtBarcode_KeyDown(object? sender, KeyEventArgs e)
    {
        if (e.KeyCode != Keys.Enter)
        {
            return;
        }

        e.Handled = true;
        e.SuppressKeyPress = true;
        PrepareScanFlowByBarcode();
    }

    private void TxtName_KeyDown(object? sender, KeyEventArgs e)
    {
        if (e.KeyCode != Keys.Enter)
        {
            return;
        }

        e.Handled = true;
        e.SuppressKeyPress = true;
        _numScanQty.Focus();
        _numScanQty.Select(0, _numScanQty.Text.Length);
    }

    private void NumScanQty_KeyDown(object? sender, KeyEventArgs e)
    {
        if (e.KeyCode != Keys.Enter)
        {
            return;
        }

        e.Handled = true;
        e.SuppressKeyPress = true;
        SaveByScanFlow();
    }

    private void PrepareScanFlowByBarcode()
    {
        try
        {
            var barcode = _txtBarcode.Text.Trim();
            if (string.IsNullOrWhiteSpace(barcode))
            {
                return;
            }

            var existing = _services.ProductService.GetByBarcode(barcode);
            if (existing != null)
            {
                _selectedProductId = existing.ProductID;
                _txtName.Text = existing.ProductName;
                _txtCategory.Text = existing.CategoryName ?? string.Empty;
                _txtUnit.Text = existing.Unit ?? string.Empty;
                _lblScanStatus.Text = $"Mã đã tồn tại: {existing.ProductName}. Nhập SL rồi Enter để cộng tồn.";
            }
            else
            {
                using var popup = new QuickNewProductPopupForm(barcode);
                if (popup.ShowDialog(this) != DialogResult.OK)
                {
                    _lblScanStatus.Text = "Đã hủy thêm mã mới. Quét mã khác để tiếp tục.";
                    _txtBarcode.SelectAll();
                    _txtBarcode.Focus();
                    return;
                }

                var product = new Product
                {
                    Barcode = barcode,
                    ProductName = popup.ProductName,
                    Unit = "Cái",
                    CostPrice = 0,
                    SellingPrice = 0,
                    StockQuantity = popup.Quantity,
                    MinStock = (int)_numMinStock.Value,
                    ExpiryDate = null
                };

                var newId = _services.ProductService.CreateProduct(product, _txtCategory.Text);
                _services.AuditService.Log(
                    _services.Session.CurrentUser?.Username,
                    "SCAN_CREATE_PRODUCT",
                    $"ProductID={newId}, Barcode={barcode}, Qty={popup.Quantity}");

                _lblScanStatus.Text = $"Đã tạo mới {popup.ProductName} (SL {popup.Quantity}). Sẵn sàng quét mã tiếp theo.";
                LoadGrid();
                ResetAfterScanSaved();
                return;
            }

            _numScanQty.Focus();
            _numScanQty.Select(0, _numScanQty.Text.Length);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Lỗi khi đọc mã quét: {ex.Message}", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void SaveByScanFlow()
    {
        try
        {
            var barcode = _txtBarcode.Text.Trim();
            if (string.IsNullOrWhiteSpace(barcode))
            {
                MessageBox.Show("Vui lòng quét/nhập mã vạch trước.", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
                _txtBarcode.Focus();
                return;
            }

            var qty = (double)_numScanQty.Value;
            if (qty <= 0)
            {
                MessageBox.Show("Số lượng phải lớn hơn 0.", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var existing = _services.ProductService.GetByBarcode(barcode);
            if (existing != null)
            {
                _services.ProductService.QuickImportStock(existing.ProductID, qty, null);
                _services.AuditService.Log(
                    _services.Session.CurrentUser?.Username,
                    "SCAN_IMPORT_EXISTING",
                    $"ProductID={existing.ProductID}, Barcode={barcode}, Qty={qty}");
                _lblScanStatus.Text = $"Đã cộng tồn {qty} cho {existing.ProductName}. Sẵn sàng quét mã tiếp theo.";
            }
            else
            {
                if (string.IsNullOrWhiteSpace(_txtName.Text))
                {
                    MessageBox.Show("Vui lòng nhập tên hàng cho mã mới.", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    _txtName.Focus();
                    return;
                }

                var product = new Product
                {
                    Barcode = barcode,
                    ProductName = _txtName.Text.Trim(),
                    Unit = string.IsNullOrWhiteSpace(_txtUnit.Text) ? "Cái" : _txtUnit.Text.Trim(),
                    CostPrice = _numCost.Value,
                    SellingPrice = _numPrice.Value,
                    StockQuantity = qty,
                    MinStock = (int)_numMinStock.Value,
                    ExpiryDate = _dtExpiry.Checked ? _dtExpiry.Value.Date : null
                };

                var newId = _services.ProductService.CreateProduct(product, _txtCategory.Text);
                _services.AuditService.Log(
                    _services.Session.CurrentUser?.Username,
                    "SCAN_CREATE_PRODUCT",
                    $"ProductID={newId}, Barcode={barcode}, Qty={qty}");
                _lblScanStatus.Text = $"Đã tạo mới {product.ProductName} (SL {qty}). Sẵn sàng quét mã tiếp theo.";
            }

            LoadGrid();
            ResetAfterScanSaved();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Lưu theo mã quét thất bại: {ex.Message}", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void ResetAfterScanSaved()
    {
        _selectedProductId = null;
        _txtBarcode.Clear();
        _txtName.Clear();
        _numScanQty.Value = 1;
        _txtBarcode.Focus();
    }

    private void OpenRestockForSelectedProduct()
    {
        if (_grid.CurrentRow?.DataBoundItem is not Product selected)
        {
            MessageBox.Show("Vui lòng chọn sản phẩm cần ghi mối nhập hàng.", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        new RestockRequestForm(_services, selected.ProductID).ShowDialog(this);
    }

    private void ShowLowStockReminderIfNeeded()
    {
        try
        {
            if (!_services.AppConfigService.LowStockReminderEnabled)
            {
                return;
            }

            var todayKey = DateTime.Today.ToString("yyyy-MM-dd");
            var suppressedDate = _services.AppConfigService.Get("low_stock_reminder_suppressed_date");
            if (string.Equals(suppressedDate, todayKey, StringComparison.Ordinal))
            {
                return;
            }

            var source = _products.Count > 0 ? _products : _services.ProductService.GetAll();
            var lowStock = _services.InventoryService.GetLowStockProducts(source);
            if (lowStock.Count == 0)
            {
                return;
            }

            using var reminder = new LowStockReminderForm(lowStock);
            var result = reminder.ShowDialog(this);
            if (reminder.DontRemindToday)
            {
                _services.AppConfigService.Set("low_stock_reminder_suppressed_date", todayKey);
                _services.AuditService.Log(_services.Session.CurrentUser?.Username, "SUPPRESS_LOW_STOCK_REMINDER", $"Date={todayKey}");
            }

            if (result == DialogResult.OK)
            {
                _services.AuditService.Log(_services.Session.CurrentUser?.Username, "OPEN_RESTOCK_FROM_REMINDER", $"LowStockCount={lowStock.Count}");
                new RestockRequestForm(_services).ShowDialog(this);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Không thể hiển thị cảnh báo tồn kho: {ex.Message}", "Cảnh báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }
}
