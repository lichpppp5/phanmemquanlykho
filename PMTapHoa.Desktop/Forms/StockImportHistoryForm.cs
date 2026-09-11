using PMTapHoa.Desktop.Models;

namespace PMTapHoa.Desktop.Forms;

public class StockImportHistoryForm : Form
{
    private readonly AppServices _services;
    private readonly DataGridView _grid;
    private readonly TextBox _txtSearch;
    private List<StockImportHistoryItem> _allData = [];

    public StockImportHistoryForm(AppServices services)
    {
        _services = services;
        Text = "Lịch sử nhập hàng";
        Width = 1100;
        Height = 680;
        MinimumSize = new Size(900, 560);
        StartPosition = FormStartPosition.CenterParent;
        AutoScaleMode = AutoScaleMode.Font;
        BackColor = UiStyle.Background;
        KeyPreview = true;
        UiStyle.ApplyWindowMode(this);

        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 3,
            Padding = new Padding(16, 14, 16, 16)
        };
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 52f));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 44f));
        Controls.Add(root);

        // Header
        var hdr = new Label
        {
            Text = "🕒  LỊCH SỬ NHẬP HÀNG",
            Font = new Font("Segoe UI", 16, FontStyle.Bold),
            Dock = DockStyle.Fill,
            ForeColor = UiStyle.HeaderDark
        };
        root.Controls.Add(hdr, 0, 0);

        // Grid
        _grid = new DataGridView
        {
            Dock = DockStyle.Fill,
            ReadOnly = true,
            AllowUserToAddRows = false,
            AutoGenerateColumns = false,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            RowHeadersVisible = false
        };
        _grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = nameof(StockImportHistoryItem.ImportDate), HeaderText = "Ngày nhập", Width = 150, DefaultCellStyle = { Format = "dd/MM/yyyy HH:mm" } });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = nameof(StockImportHistoryItem.ProductName), HeaderText = "Tên sản phẩm", AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill, MinimumWidth = 220 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = nameof(StockImportHistoryItem.SupplierName), HeaderText = "Nhà cung cấp", Width = 170 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = nameof(StockImportHistoryItem.Quantity), HeaderText = "Số lượng", Width = 90, DefaultCellStyle = { Alignment = DataGridViewContentAlignment.MiddleRight } });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = nameof(StockImportHistoryItem.CostPrice), HeaderText = "Giá nhập", Width = 110, DefaultCellStyle = { Alignment = DataGridViewContentAlignment.MiddleRight, Format = "N0" } });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = nameof(StockImportHistoryItem.ImportedBy), HeaderText = "Người nhập", Width = 120 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = nameof(StockImportHistoryItem.Note), HeaderText = "Ghi chú", Width = 140 });
        UiStyle.StyleGrid(_grid);
        root.Controls.Add(_grid, 0, 1);

        // Footer
        var footer = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            Padding = new Padding(0, 4, 0, 0)
        };
        root.Controls.Add(footer, 0, 2);

        var lblSearch = new Label { Text = "Tìm sản phẩm:", AutoSize = true, Margin = new Padding(0, 8, 8, 0) };
        _txtSearch = new TextBox { Width = 260, Font = new Font("Segoe UI", 11), Margin = new Padding(0, 4, 12, 0) };
        _txtSearch.TextChanged += (_, _) => FilterGrid();
        var btnRefresh = new Button
        {
            Text = "🔄 Tải lại",
            AutoSize = true,
            MinimumSize = new Size(110, 36),
            BackColor = UiStyle.Primary,
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat
        };
        btnRefresh.FlatAppearance.BorderSize = 0;
        btnRefresh.Click += (_, _) => LoadData();
        footer.Controls.AddRange(new Control[] { lblSearch, _txtSearch, btnRefresh });

        Load += (_, _) => LoadData();
        KeyDown += (_, e) => { if (e.KeyCode == Keys.Escape) Close(); };
    }

    private void LoadData()
    {
        try
        {
            _allData = _services.ProductService.GetImportHistory(null, 500);
            FilterGrid();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Lỗi tải dữ liệu: {ex.Message}", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void FilterGrid()
    {
        var kw = _txtSearch.Text.Trim();
        var filtered = string.IsNullOrWhiteSpace(kw)
            ? _allData
            : _allData.Where(x => x.ProductName.Contains(kw, StringComparison.CurrentCultureIgnoreCase)
                                || (x.SupplierName ?? "").Contains(kw, StringComparison.CurrentCultureIgnoreCase)).ToList();
        _grid.DataSource = filtered;
    }
}
