using PMTapHoa.Desktop.Models;
using System.ComponentModel;

namespace PMTapHoa.Desktop.Forms;

public class RestockRequestForm : Form
{
    private readonly AppServices _services;
    private readonly int? _defaultProductId;
    private readonly ComboBox _cmbProducts;
    private readonly TextBox _txtSupplierName;
    private readonly TextBox _txtContactName;
    private readonly TextBox _txtPhone;
    private readonly TextBox _txtAddress;
    private readonly NumericUpDown _numQty;
    private readonly NumericUpDown _numExpectedCost;
    private readonly TextBox _txtNote;
    private readonly ComboBox _cmbStatusFilter;
    private readonly DataGridView _grid;
    private readonly DataGridView _gridSuggestions;
    private readonly BindingList<LowStockSuggestionItem> _suggestions = [];

    public RestockRequestForm(AppServices services, int? defaultProductId = null)
    {
        _services = services;
        _defaultProductId = defaultProductId;

        Text = "Mối nhập hàng / Yêu cầu nhập hàng";
        Width = 1260;
        Height = 840;
        MinimumSize = new Size(1180, 760);
        StartPosition = FormStartPosition.CenterParent;
        KeyPreview = true;
        BackColor = Color.WhiteSmoke;

        var panel = new GroupBox
        {
            Text = "Tạo yêu cầu nhập hàng",
            Location = new Point(20, 20),
            Width = 1060,
            Height = 190,
            Font = new Font("Segoe UI", 10, FontStyle.Bold)
        };

        panel.Controls.Add(new Label { Text = "Sản phẩm:", AutoSize = true, Location = new Point(20, 35) });
        _cmbProducts = new ComboBox
        {
            Location = new Point(85, 31),
            Width = 280,
            DropDownStyle = ComboBoxStyle.DropDownList,
            Font = new Font("Segoe UI", 10, FontStyle.Regular)
        };
        panel.Controls.Add(_cmbProducts);

        panel.Controls.Add(new Label { Text = "Mối nhập:", AutoSize = true, Location = new Point(390, 35) });
        _txtSupplierName = new TextBox { Location = new Point(455, 31), Width = 220 };
        panel.Controls.Add(_txtSupplierName);

        panel.Controls.Add(new Label { Text = "Người liên hệ:", AutoSize = true, Location = new Point(700, 35) });
        _txtContactName = new TextBox { Location = new Point(790, 31), Width = 240 };
        panel.Controls.Add(_txtContactName);

        panel.Controls.Add(new Label { Text = "SĐT:", AutoSize = true, Location = new Point(20, 78) });
        _txtPhone = new TextBox { Location = new Point(85, 74), Width = 180 };
        panel.Controls.Add(_txtPhone);

        panel.Controls.Add(new Label { Text = "Địa chỉ:", AutoSize = true, Location = new Point(290, 78) });
        _txtAddress = new TextBox { Location = new Point(340, 74), Width = 335 };
        panel.Controls.Add(_txtAddress);

        panel.Controls.Add(new Label { Text = "SL cần nhập:", AutoSize = true, Location = new Point(700, 78) });
        _numQty = new NumericUpDown
        {
            Location = new Point(790, 74),
            Width = 100,
            DecimalPlaces = 2,
            Maximum = 1000000,
            Value = 1
        };
        panel.Controls.Add(_numQty);

        panel.Controls.Add(new Label { Text = "Giá nhập dự kiến:", AutoSize = true, Location = new Point(20, 122) });
        _numExpectedCost = new NumericUpDown
        {
            Location = new Point(120, 118),
            Width = 145,
            DecimalPlaces = 2,
            Maximum = 1000000000
        };
        panel.Controls.Add(_numExpectedCost);

        panel.Controls.Add(new Label { Text = "Ghi chú:", AutoSize = true, Location = new Point(290, 122) });
        _txtNote = new TextBox { Location = new Point(340, 118), Width = 500 };
        panel.Controls.Add(_txtNote);

        var btnSave = new Button
        {
            Text = "Lưu yêu cầu",
            Width = 130,
            Height = 34,
            Location = new Point(860, 115),
            BackColor = Color.FromArgb(39, 174, 96),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat
        };
        btnSave.Click += (_, _) => SaveRequest();
        panel.Controls.Add(btnSave);

        Controls.Add(panel);

        var suggestBox = new GroupBox
        {
            Text = "Gợi ý tự động cho hàng sắp hết",
            Location = new Point(20, 220),
            Width = 1060,
            Height = 180,
            Font = new Font("Segoe UI", 10, FontStyle.Bold)
        };

        var btnLoadSuggestions = new Button
        {
            Text = "Tải gợi ý",
            Width = 100,
            Location = new Point(20, 30),
            BackColor = Color.FromArgb(52, 152, 219),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat
        };
        btnLoadSuggestions.Click += (_, _) => LoadSuggestions();
        suggestBox.Controls.Add(btnLoadSuggestions);

        var btnCreateSuggested = new Button
        {
            Text = "Tạo yêu cầu đã chọn",
            Width = 160,
            Location = new Point(130, 30),
            BackColor = Color.FromArgb(230, 126, 34),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat
        };
        btnCreateSuggested.Click += (_, _) => CreateRequestsFromSuggestions();
        suggestBox.Controls.Add(btnCreateSuggested);

        var hint = new Label
        {
            Text = "Mặc định sẽ tự chọn dòng chưa có yêu cầu Open/Ordered.",
            AutoSize = true,
            ForeColor = Color.DimGray,
            Location = new Point(310, 35)
        };
        suggestBox.Controls.Add(hint);

        _gridSuggestions = new DataGridView
        {
            Location = new Point(20, 65),
            Width = 1020,
            Height = 100,
            AllowUserToAddRows = false,
            AutoGenerateColumns = false
        };
        _gridSuggestions.Columns.Add(new DataGridViewCheckBoxColumn { DataPropertyName = nameof(LowStockSuggestionItem.IsSelected), HeaderText = "Chọn", Width = 60 });
        _gridSuggestions.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = nameof(LowStockSuggestionItem.ProductName), HeaderText = "Sản phẩm", Width = 280, ReadOnly = true });
        _gridSuggestions.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = nameof(LowStockSuggestionItem.Barcode), HeaderText = "Mã vạch", Width = 140, ReadOnly = true });
        _gridSuggestions.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = nameof(LowStockSuggestionItem.StockQuantity), HeaderText = "Tồn", Width = 90, ReadOnly = true });
        _gridSuggestions.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = nameof(LowStockSuggestionItem.MinStock), HeaderText = "Min", Width = 80, ReadOnly = true });
        _gridSuggestions.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = nameof(LowStockSuggestionItem.SuggestedQty), HeaderText = "SL gợi ý", Width = 100 });
        _gridSuggestions.Columns.Add(new DataGridViewCheckBoxColumn { DataPropertyName = nameof(LowStockSuggestionItem.HasOpenRequest), HeaderText = "Đang có YC mở", Width = 120, ReadOnly = true });
        _gridSuggestions.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 10, FontStyle.Bold);
        _gridSuggestions.DefaultCellStyle.Font = new Font("Segoe UI", 10, FontStyle.Regular);
        _gridSuggestions.ColumnHeadersHeight = 34;
        _gridSuggestions.RowTemplate.Height = 32;
        _gridSuggestions.RowPrePaint += GridSuggestions_RowPrePaint;
        suggestBox.Controls.Add(_gridSuggestions);

        Controls.Add(suggestBox);

        Controls.Add(new Label { Text = "Lọc trạng thái:", AutoSize = true, Location = new Point(20, 415) });
        _cmbStatusFilter = new ComboBox
        {
            Location = new Point(105, 411),
            Width = 140,
            DropDownStyle = ComboBoxStyle.DropDownList,
            Font = new Font("Segoe UI", 10, FontStyle.Regular)
        };
        _cmbStatusFilter.Items.AddRange(["Tất cả", "Open", "Ordered", "Received", "Cancelled"]);
        _cmbStatusFilter.SelectedIndex = 0;
        _cmbStatusFilter.SelectedIndexChanged += (_, _) => LoadRequests();
        Controls.Add(_cmbStatusFilter);

        var btnRefresh = new Button
        {
            Text = "Làm mới",
            Width = 100,
            Location = new Point(265, 410),
            BackColor = Color.FromArgb(127, 140, 141),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat
        };
        btnRefresh.Click += (_, _) => LoadRequests();
        Controls.Add(btnRefresh);

        _grid = new DataGridView
        {
            Location = new Point(20, 450),
            Width = 1060,
            Height = 185,
            ReadOnly = true,
            AllowUserToAddRows = false,
            AutoGenerateColumns = false
        };
        _grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = nameof(RestockRequestItem.RequestID), HeaderText = "ID", Width = 60 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = nameof(RestockRequestItem.RequestDate), HeaderText = "Ngày tạo", Width = 150 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = nameof(RestockRequestItem.ProductName), HeaderText = "Sản phẩm", Width = 230 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = nameof(RestockRequestItem.SupplierName), HeaderText = "Mối nhập", Width = 180 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = nameof(RestockRequestItem.RequestedQty), HeaderText = "SL", Width = 80 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = nameof(RestockRequestItem.ExpectedCostPrice), HeaderText = "Giá dự kiến", Width = 120 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = nameof(RestockRequestItem.Status), HeaderText = "Trạng thái", Width = 100 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = nameof(RestockRequestItem.Note), HeaderText = "Ghi chú", Width = 130 });
        _grid.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 10, FontStyle.Bold);
        _grid.DefaultCellStyle.Font = new Font("Segoe UI", 10, FontStyle.Regular);
        _grid.ColumnHeadersHeight = 34;
        _grid.RowTemplate.Height = 32;
        _grid.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(248, 249, 250);
        _grid.CellDoubleClick += Grid_CellDoubleClick;
        Controls.Add(_grid);

        Load += (_, _) =>
        {
            LoadProductCombo();
            LoadSuggestions();
            LoadRequests();
        };
        KeyDown += (_, e) =>
        {
            if (e.KeyCode == Keys.Escape)
            {
                Close();
            }
        };
    }

    private void LoadProductCombo()
    {
        var products = _services.ProductService.GetAll();
        _cmbProducts.DataSource = products;
        _cmbProducts.DisplayMember = nameof(Product.ProductName);
        _cmbProducts.ValueMember = nameof(Product.ProductID);

        if (_defaultProductId.HasValue)
        {
            _cmbProducts.SelectedValue = _defaultProductId.Value;
        }
    }

    private void SaveRequest()
    {
        try
        {
            if (_cmbProducts.SelectedValue is not int productId)
            {
                MessageBox.Show("Vui lòng chọn sản phẩm.", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var expected = _numExpectedCost.Value > 0 ? _numExpectedCost.Value : (decimal?)null;
            var requestId = _services.RestockService.CreateRequest(
                productId,
                _txtSupplierName.Text,
                _txtContactName.Text,
                _txtPhone.Text,
                _txtAddress.Text,
                (double)_numQty.Value,
                expected,
                _txtNote.Text);

            _services.AuditService.Log(
                _services.Session.CurrentUser?.Username,
                "CREATE_RESTOCK_REQUEST",
                $"RequestID={requestId}, ProductID={productId}, Supplier={_txtSupplierName.Text.Trim()}");

            MessageBox.Show("Đã lưu yêu cầu nhập hàng.", "Thành công", MessageBoxButtons.OK, MessageBoxIcon.Information);
            _numQty.Value = 1;
            _numExpectedCost.Value = 0;
            _txtNote.Clear();
            LoadRequests();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Lưu yêu cầu thất bại: {ex.Message}", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void LoadRequests()
    {
        var status = _cmbStatusFilter.SelectedIndex <= 0 ? null : _cmbStatusFilter.SelectedItem?.ToString();
        var rows = _services.RestockService.GetRequests(status);
        _grid.DataSource = null;
        _grid.DataSource = rows;
    }

    private void LoadSuggestions()
    {
        var rows = _services.RestockService.GetLowStockSuggestions();
        _suggestions.Clear();
        foreach (var row in rows)
        {
            _suggestions.Add(row);
        }

        _gridSuggestions.DataSource = null;
        _gridSuggestions.DataSource = _suggestions;
    }

    private void CreateRequestsFromSuggestions()
    {
        try
        {
            var selected = _suggestions
                .Where(x => x.IsSelected && !x.HasOpenRequest && x.SuggestedQty > 0)
                .ToList();

            if (selected.Count == 0)
            {
                MessageBox.Show("Không có dòng gợi ý hợp lệ để tạo yêu cầu.", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            if (string.IsNullOrWhiteSpace(_txtSupplierName.Text))
            {
                MessageBox.Show("Nhập tên mối nhập trước khi tạo yêu cầu tự động.", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
                _txtSupplierName.Focus();
                return;
            }

            var expected = _numExpectedCost.Value > 0 ? _numExpectedCost.Value : (decimal?)null;
            var createdCount = 0;
            foreach (var item in selected)
            {
                _services.RestockService.CreateRequest(
                    item.ProductID,
                    _txtSupplierName.Text,
                    _txtContactName.Text,
                    _txtPhone.Text,
                    _txtAddress.Text,
                    item.SuggestedQty,
                    expected,
                    $"Tự động gợi ý: Tồn={item.StockQuantity}, Min={item.MinStock}. {_txtNote.Text}".Trim());
                createdCount++;
            }

            _services.AuditService.Log(
                _services.Session.CurrentUser?.Username,
                "AUTO_CREATE_RESTOCK_REQUESTS",
                $"Created={createdCount}, Supplier={_txtSupplierName.Text.Trim()}");

            MessageBox.Show($"Đã tạo {createdCount} yêu cầu nhập từ gợi ý.", "Thành công", MessageBoxButtons.OK, MessageBoxIcon.Information);
            LoadSuggestions();
            LoadRequests();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Tạo yêu cầu tự động thất bại: {ex.Message}", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void Grid_CellDoubleClick(object? sender, DataGridViewCellEventArgs e)
    {
        if (!_services.Session.IsManager)
        {
            return;
        }

        if (e.RowIndex < 0 || _grid.Rows[e.RowIndex].DataBoundItem is not RestockRequestItem row)
        {
            return;
        }

        var next = row.Status switch
        {
            "Open" => "Ordered",
            "Ordered" => "Received",
            _ => "Open"
        };

        try
        {
            _services.RestockService.UpdateStatus(row.RequestID, next);
            _services.AuditService.Log(_services.Session.CurrentUser?.Username, "UPDATE_RESTOCK_STATUS", $"RequestID={row.RequestID}, Status={next}");
            LoadRequests();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Không cập nhật được trạng thái: {ex.Message}", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void GridSuggestions_RowPrePaint(object? sender, DataGridViewRowPrePaintEventArgs e)
    {
        if (e.RowIndex < 0 || e.RowIndex >= _gridSuggestions.Rows.Count)
        {
            return;
        }

        if (_gridSuggestions.Rows[e.RowIndex].DataBoundItem is not LowStockSuggestionItem row)
        {
            return;
        }

        if (row.HasOpenRequest)
        {
            _gridSuggestions.Rows[e.RowIndex].DefaultCellStyle.BackColor = Color.LemonChiffon;
            _gridSuggestions.Rows[e.RowIndex].DefaultCellStyle.ForeColor = Color.DarkGoldenrod;
        }
        else
        {
            _gridSuggestions.Rows[e.RowIndex].DefaultCellStyle.BackColor = Color.Honeydew;
            _gridSuggestions.Rows[e.RowIndex].DefaultCellStyle.ForeColor = Color.DarkGreen;
        }
    }
}
