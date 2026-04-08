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
        if (!_services.Session.IsAdmin)
        {
            throw new InvalidOperationException("Chỉ Admin mới được truy cập màn hình mối nhập hàng.");
        }

        Text = "Mối nhập hàng / Yêu cầu nhập hàng";
        Width = 1260;
        Height = 840;
        MinimumSize = new Size(1180, 760);
        StartPosition = FormStartPosition.CenterParent;
        WindowState = FormWindowState.Normal;
        AutoScroll = false;
        AutoScaleMode = AutoScaleMode.Font;
        Font = new Font("Segoe UI", 11, FontStyle.Regular);
        KeyPreview = true;
        BackColor = Color.WhiteSmoke;
        UiStyle.ApplyWindowMode(this);

        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 4,
            Padding = new Padding(20, 16, 20, 16)
        };
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 250f));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 260f));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 56f));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
        Controls.Add(root);

        var panel = new GroupBox
        {
            Text = "Tạo yêu cầu nhập hàng",
            Dock = DockStyle.Fill,
            Font = new Font("Segoe UI", 12, FontStyle.Bold)
        };
        var requestLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 6,
            RowCount = 3,
            Padding = new Padding(12, 10, 12, 10)
        };
        requestLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 100f));
        requestLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 34f));
        requestLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 100f));
        requestLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33f));
        requestLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 110f));
        requestLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33f));
        requestLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 56f));
        requestLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 56f));
        requestLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 56f));
        panel.Controls.Add(requestLayout);

        requestLayout.Controls.Add(new Label { Text = "Sản phẩm:", AutoSize = true, Anchor = AnchorStyles.Left, Font = new Font("Segoe UI", 11, FontStyle.Bold) }, 0, 0);
        _cmbProducts = new ComboBox
        {
            Dock = DockStyle.Fill,
            DropDownStyle = ComboBoxStyle.DropDownList,
            Font = new Font("Segoe UI", 11, FontStyle.Regular)
        };
        requestLayout.Controls.Add(_cmbProducts, 1, 0);

        requestLayout.Controls.Add(new Label { Text = "Mối nhập:", AutoSize = true, Anchor = AnchorStyles.Left, Font = new Font("Segoe UI", 11, FontStyle.Bold) }, 2, 0);
        _txtSupplierName = new TextBox { Dock = DockStyle.Fill, Font = new Font("Segoe UI", 11, FontStyle.Regular) };
        requestLayout.Controls.Add(_txtSupplierName, 3, 0);

        requestLayout.Controls.Add(new Label { Text = "Người liên hệ:", AutoSize = true, Anchor = AnchorStyles.Left, Font = new Font("Segoe UI", 11, FontStyle.Bold) }, 4, 0);
        _txtContactName = new TextBox { Dock = DockStyle.Fill, Font = new Font("Segoe UI", 11, FontStyle.Regular) };
        requestLayout.Controls.Add(_txtContactName, 5, 0);

        requestLayout.Controls.Add(new Label { Text = "SĐT:", AutoSize = true, Anchor = AnchorStyles.Left, Font = new Font("Segoe UI", 11, FontStyle.Bold) }, 0, 1);
        _txtPhone = new TextBox { Dock = DockStyle.Fill, Font = new Font("Segoe UI", 11, FontStyle.Regular) };
        requestLayout.Controls.Add(_txtPhone, 1, 1);

        requestLayout.Controls.Add(new Label { Text = "Địa chỉ:", AutoSize = true, Anchor = AnchorStyles.Left, Font = new Font("Segoe UI", 11, FontStyle.Bold) }, 2, 1);
        _txtAddress = new TextBox { Dock = DockStyle.Fill, Font = new Font("Segoe UI", 11, FontStyle.Regular) };
        requestLayout.Controls.Add(_txtAddress, 3, 1);

        requestLayout.Controls.Add(new Label { Text = "SL cần nhập:", AutoSize = true, Anchor = AnchorStyles.Left, Font = new Font("Segoe UI", 11, FontStyle.Bold) }, 4, 1);
        _numQty = new NumericUpDown
        {
            Dock = DockStyle.Left,
            Width = 120,
            DecimalPlaces = 2,
            Maximum = 1000000,
            Value = 1,
            Font = new Font("Segoe UI", 11, FontStyle.Regular)
        };
        requestLayout.Controls.Add(_numQty, 5, 1);

        requestLayout.Controls.Add(new Label { Text = "Giá nhập dự kiến:", AutoSize = true, Anchor = AnchorStyles.Left, Font = new Font("Segoe UI", 11, FontStyle.Bold) }, 0, 2);
        _numExpectedCost = new NumericUpDown
        {
            Dock = DockStyle.Left,
            Width = 150,
            DecimalPlaces = 2,
            Maximum = 1000000000,
            Font = new Font("Segoe UI", 11, FontStyle.Regular)
        };
        requestLayout.Controls.Add(_numExpectedCost, 1, 2);

        requestLayout.Controls.Add(new Label { Text = "Ghi chú:", AutoSize = true, Anchor = AnchorStyles.Left, Font = new Font("Segoe UI", 11, FontStyle.Bold) }, 2, 2);
        _txtNote = new TextBox { Dock = DockStyle.Fill, Font = new Font("Segoe UI", 11, FontStyle.Regular) };
        requestLayout.Controls.Add(_txtNote, 3, 2);

        var btnSave = new Button
        {
            Text = "Lưu yêu cầu",
            Width = 140,
            Height = 42,
            Anchor = AnchorStyles.Left,
            BackColor = UiStyle.Success,
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI", 11, FontStyle.Bold)
        };
        btnSave.Click += (_, _) => SaveRequest();
        requestLayout.Controls.Add(btnSave, 5, 2);

        root.Controls.Add(panel, 0, 0);

        var suggestBox = new GroupBox
        {
            Text = "Gợi ý tự động cho hàng sắp hết",
            Dock = DockStyle.Fill,
            Font = new Font("Segoe UI", 12, FontStyle.Bold)
        };

        var suggestLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 2,
            Padding = new Padding(10, 10, 10, 10)
        };
        suggestLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 48f));
        suggestLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
        suggestBox.Controls.Add(suggestLayout);

        var suggestTop = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = true
        };
        suggestLayout.Controls.Add(suggestTop, 0, 0);

        var btnLoadSuggestions = new Button
        {
            Text = "Tải gợi ý",
            Width = 120,
            Height = 38,
            BackColor = UiStyle.Primary,
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI", 10, FontStyle.Bold)
        };
        btnLoadSuggestions.Click += (_, _) => LoadSuggestions();
        suggestTop.Controls.Add(btnLoadSuggestions);

        var btnCreateSuggested = new Button
        {
            Text = "Tạo yêu cầu đã chọn",
            Width = 190,
            Height = 38,
            BackColor = UiStyle.AccentOrange,
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI", 10, FontStyle.Bold)
        };
        btnCreateSuggested.Click += (_, _) => CreateRequestsFromSuggestions();
        suggestTop.Controls.Add(btnCreateSuggested);

        var hint = new Label
        {
            Text = "Mặc định sẽ tự chọn dòng chưa có yêu cầu Open/Ordered.",
            AutoSize = true,
            ForeColor = Color.DimGray,
            Margin = new Padding(10, 8, 0, 0)
        };
        suggestTop.Controls.Add(hint);

        _gridSuggestions = new DataGridView
        {
            Dock = DockStyle.Fill,
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
        _gridSuggestions.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 11, FontStyle.Bold);
        _gridSuggestions.DefaultCellStyle.Font = new Font("Segoe UI", 11, FontStyle.Regular);
        _gridSuggestions.ColumnHeadersHeight = 38;
        _gridSuggestions.RowTemplate.Height = 34;
        _gridSuggestions.RowPrePaint += GridSuggestions_RowPrePaint;
        suggestLayout.Controls.Add(_gridSuggestions, 0, 1);

        root.Controls.Add(suggestBox, 0, 1);

        var filterPanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = true
        };
        root.Controls.Add(filterPanel, 0, 2);
        filterPanel.Controls.Add(new Label { Text = "Lọc trạng thái:", AutoSize = true, Margin = new Padding(0, 10, 8, 0), Font = new Font("Segoe UI", 11, FontStyle.Bold) });
        _cmbStatusFilter = new ComboBox
        {
            Width = 170,
            DropDownStyle = ComboBoxStyle.DropDownList,
            Font = new Font("Segoe UI", 11, FontStyle.Regular),
            Margin = new Padding(0, 4, 12, 0)
        };
        _cmbStatusFilter.Items.AddRange(["Tất cả", "Open", "Ordered", "Received", "Cancelled"]);
        _cmbStatusFilter.SelectedIndex = 0;
        _cmbStatusFilter.SelectedIndexChanged += (_, _) => LoadRequests();
        filterPanel.Controls.Add(_cmbStatusFilter);

        var btnRefresh = new Button
        {
            Text = "Làm mới",
            Width = 120,
            Height = 38,
            BackColor = UiStyle.Neutral,
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI", 10, FontStyle.Bold),
            Margin = new Padding(0, 4, 0, 0)
        };
        btnRefresh.Click += (_, _) => LoadRequests();
        filterPanel.Controls.Add(btnRefresh);

        _grid = new DataGridView
        {
            Dock = DockStyle.Fill,
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
        _grid.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 11, FontStyle.Bold);
        _grid.DefaultCellStyle.Font = new Font("Segoe UI", 11, FontStyle.Regular);
        _grid.ColumnHeadersHeight = 38;
        _grid.RowTemplate.Height = 34;
        _grid.AlternatingRowsDefaultCellStyle.BackColor = UiStyle.GridAltRow;
        _grid.CellDoubleClick += Grid_CellDoubleClick;
        root.Controls.Add(_grid, 0, 3);

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
        if (!_services.Session.IsAdmin)
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
