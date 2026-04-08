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
        KeyPreview = true;
        BackColor = Color.WhiteSmoke;

        var lblSearch = new Label
        {
            Text = "Tìm nhanh (tên/mã vạch):",
            AutoSize = true,
            Location = new Point(20, 20)
        };

        _txtSearch = new TextBox
        {
            Width = 300,
            Location = new Point(180, 16),
            Font = new Font("Segoe UI", 11, FontStyle.Regular)
        };
        _txtSearch.TextChanged += (_, _) => LoadGrid();

        _grid = new DataGridView
        {
            Location = new Point(20, 55),
            Width = 1090,
            Height = 340,
            ReadOnly = true,
            AllowUserToAddRows = false,
            AutoGenerateColumns = false
        };
        _grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = nameof(Product.ProductID), HeaderText = "ID", Width = 55 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = nameof(Product.Barcode), HeaderText = "Mã vạch", Width = 140 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = nameof(Product.ProductName), HeaderText = "Tên sản phẩm", Width = 260 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = nameof(Product.CategoryName), HeaderText = "Danh mục", Width = 130 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = nameof(Product.Unit), HeaderText = "ĐVT", Width = 70 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = nameof(Product.StockQuantity), HeaderText = "Tồn", Width = 90 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = nameof(Product.MinStock), HeaderText = "Mức tối thiểu", Width = 110 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = nameof(Product.CostPrice), HeaderText = "Giá nhập", Width = 100 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = nameof(Product.SellingPrice), HeaderText = "Giá bán", Width = 100 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = nameof(Product.ExpiryDate), HeaderText = "HSD", Width = 120 });
        _grid.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 10, FontStyle.Bold);
        _grid.ColumnHeadersHeight = 38;
        _grid.DefaultCellStyle.Font = new Font("Segoe UI", 10, FontStyle.Regular);
        _grid.RowTemplate.Height = 34;
        _grid.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(248, 249, 250);
        _grid.RowPrePaint += Grid_RowPrePaint;
        _grid.SelectionChanged += (_, _) => LoadSelectedProductToEditor();

        var editorPanel = new GroupBox
        {
            Text = "CRUD sản phẩm",
            Location = new Point(20, 410),
            Width = 1090,
            Height = 190,
            Font = new Font("Segoe UI", 10, FontStyle.Bold)
        };

        var lblBarcode = new Label { Text = "Mã vạch:", AutoSize = true, Location = new Point(18, 36) };
        _txtBarcode = new TextBox { Location = new Point(85, 32), Width = 150 };
        var lblName = new Label { Text = "Tên:", AutoSize = true, Location = new Point(255, 36) };
        _txtName = new TextBox { Location = new Point(295, 32), Width = 250 };
        var lblCategory = new Label { Text = "Danh mục:", AutoSize = true, Location = new Point(565, 36) };
        _txtCategory = new TextBox { Location = new Point(635, 32), Width = 160 };
        var lblUnit = new Label { Text = "ĐVT:", AutoSize = true, Location = new Point(820, 36) };
        _txtUnit = new TextBox { Location = new Point(855, 32), Width = 80 };

        var lblCostValue = new Label { Text = "Giá nhập:", AutoSize = true, Location = new Point(18, 82) };
        _numCost = new NumericUpDown { Location = new Point(85, 78), Width = 150, DecimalPlaces = 2, Maximum = 1000000000 };
        var lblPrice = new Label { Text = "Giá bán:", AutoSize = true, Location = new Point(255, 82) };
        _numPrice = new NumericUpDown { Location = new Point(295, 78), Width = 150, DecimalPlaces = 2, Maximum = 1000000000 };
        var lblStock = new Label { Text = "Tồn kho:", AutoSize = true, Location = new Point(470, 82) };
        _numStock = new NumericUpDown { Location = new Point(525, 78), Width = 90, DecimalPlaces = 2, Maximum = 1000000 };
        var lblMin = new Label { Text = "Min:", AutoSize = true, Location = new Point(635, 82) };
        _numMinStock = new NumericUpDown { Location = new Point(670, 78), Width = 90, Maximum = 1000000, Value = 5 };
        var lblExpiry = new Label { Text = "HSD:", AutoSize = true, Location = new Point(785, 82) };
        _dtExpiry = new DateTimePicker { Location = new Point(825, 78), Width = 170, Format = DateTimePickerFormat.Short, ShowCheckBox = true };

        _btnAdd = new Button { Text = "Thêm", Width = 100, Location = new Point(295, 130), BackColor = Color.FromArgb(46, 204, 113), ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
        _btnUpdate = new Button { Text = "Sửa", Width = 100, Location = new Point(410, 130), BackColor = Color.FromArgb(52, 152, 219), ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
        _btnDelete = new Button { Text = "Xóa", Width = 100, Location = new Point(525, 130), BackColor = Color.FromArgb(192, 57, 43), ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
        var btnClear = new Button { Text = "Làm mới form", Width = 120, Location = new Point(640, 130), BackColor = Color.FromArgb(127, 140, 141), ForeColor = Color.White, FlatStyle = FlatStyle.Flat };

        _btnAdd.Click += (_, _) => CreateProduct();
        _btnUpdate.Click += (_, _) => UpdateProduct();
        _btnDelete.Click += (_, _) => DeleteProduct();
        btnClear.Click += (_, _) => ResetEditor();

        editorPanel.Controls.Add(lblBarcode);
        editorPanel.Controls.Add(_txtBarcode);
        editorPanel.Controls.Add(lblName);
        editorPanel.Controls.Add(_txtName);
        editorPanel.Controls.Add(lblCategory);
        editorPanel.Controls.Add(_txtCategory);
        editorPanel.Controls.Add(lblUnit);
        editorPanel.Controls.Add(_txtUnit);
        editorPanel.Controls.Add(lblCostValue);
        editorPanel.Controls.Add(_numCost);
        editorPanel.Controls.Add(lblPrice);
        editorPanel.Controls.Add(_numPrice);
        editorPanel.Controls.Add(lblStock);
        editorPanel.Controls.Add(_numStock);
        editorPanel.Controls.Add(lblMin);
        editorPanel.Controls.Add(_numMinStock);
        editorPanel.Controls.Add(lblExpiry);
        editorPanel.Controls.Add(_dtExpiry);
        editorPanel.Controls.Add(_btnAdd);
        editorPanel.Controls.Add(_btnUpdate);
        editorPanel.Controls.Add(_btnDelete);
        editorPanel.Controls.Add(btnClear);

        var importPanel = new GroupBox
        {
            Text = "Nhập hàng nhanh",
            Location = new Point(20, 615),
            Width = 1090,
            Height = 90,
            Font = new Font("Segoe UI", 10, FontStyle.Bold)
        };

        var lblQty = new Label
        {
            Text = "Số lượng cộng thêm:",
            AutoSize = true,
            Location = new Point(20, 38)
        };
        _numImportQty = new NumericUpDown
        {
            Location = new Point(150, 34),
            Width = 90,
            DecimalPlaces = 2,
            Maximum = 100000
        };

        var lblCost = new Label
        {
            Text = "Giá nhập mới (tuỳ chọn):",
            AutoSize = true,
            Location = new Point(270, 38)
        };
        _txtCostPrice = new TextBox
        {
            Width = 140,
            Location = new Point(430, 34)
        };

        var btnImport = new Button
        {
            Text = "Cập nhật nhập hàng",
            Width = 170,
            Location = new Point(600, 32),
            BackColor = Color.FromArgb(41, 128, 185),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat
        };
        btnImport.Click += (_, _) => QuickImportSelectedProduct();

        var btnRestock = new Button
        {
            Text = "Ghi mối nhập hàng",
            Width = 160,
            Location = new Point(785, 32),
            BackColor = Color.FromArgb(243, 156, 18),
            ForeColor = Color.Black,
            FlatStyle = FlatStyle.Flat
        };
        btnRestock.Click += (_, _) => OpenRestockForSelectedProduct();

        if (!_services.Session.IsManager)
        {
            _btnDelete.Enabled = false;
        }

        importPanel.Controls.Add(lblQty);
        importPanel.Controls.Add(_numImportQty);
        importPanel.Controls.Add(lblCost);
        importPanel.Controls.Add(_txtCostPrice);
        importPanel.Controls.Add(btnImport);
        importPanel.Controls.Add(btnRestock);

        Controls.Add(lblSearch);
        Controls.Add(_txtSearch);
        Controls.Add(_grid);
        Controls.Add(editorPanel);
        Controls.Add(importPanel);

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
        _dtExpiry.Checked = false;
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
