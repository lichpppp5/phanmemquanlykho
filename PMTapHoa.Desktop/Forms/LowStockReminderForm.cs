using PMTapHoa.Desktop.Models;

namespace PMTapHoa.Desktop.Forms;

public class LowStockReminderForm : Form
{
    private readonly CheckBox _chkDontRemindToday;

    public bool DontRemindToday => _chkDontRemindToday.Checked;

    public LowStockReminderForm(List<Product> lowStockProducts)
    {
        UiStyle.ApplyDialogStyle(this, "Cảnh báo hàng sắp hết", new Size(900, 620), sizable: true);
        MinimumSize = new Size(820, 560);

        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 4,
            Padding = new Padding(14)
        };
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 38f));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 34f));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 62f));
        Controls.Add(root);

        var title = new Label
        {
            Text = $"Có {lowStockProducts.Count} mặt hàng đang dưới mức tồn tối thiểu.",
            Dock = DockStyle.Fill,
            Font = new Font("Segoe UI", 11, FontStyle.Bold),
            TextAlign = ContentAlignment.MiddleLeft
        };
        root.Controls.Add(title, 0, 0);

        var hint = new Label
        {
            Text = "Bạn có muốn mở màn hình Mối nhập hàng để tạo yêu cầu nhập ngay?",
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft
        };
        root.Controls.Add(hint, 0, 1);

        var grid = new DataGridView
        {
            Dock = DockStyle.Fill
        };
        grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = nameof(Product.ProductName), HeaderText = "Sản phẩm", Width = 300 });
        grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = nameof(Product.Barcode), HeaderText = "Mã vạch", Width = 150 });
        grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = nameof(Product.StockQuantity), HeaderText = "Tồn", Width = 90 });
        grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = nameof(Product.MinStock), HeaderText = "Min", Width = 90 });
        UiStyle.StyleGrid(grid);
        grid.DataSource = lowStockProducts
            .OrderBy(p => p.StockQuantity - p.MinStock)
            .Take(50)
            .ToList();
        root.Controls.Add(grid, 0, 2);

        var actionPanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            Padding = new Padding(4, 10, 4, 4)
        };
        root.Controls.Add(actionPanel, 0, 3);

        _chkDontRemindToday = new CheckBox
        {
            Text = "Không nhắc lại hôm nay",
            AutoSize = true,
            Margin = new Padding(0, 8, 20, 0)
        };
        actionPanel.Controls.Add(_chkDontRemindToday);

        var btnOpen = new Button
        {
            Text = "Mở Mối nhập hàng",
            Width = 160,
            Height = 36
        };
        UiStyle.StyleButton(btnOpen, UiStyle.Primary);
        btnOpen.Click += (_, _) =>
        {
            DialogResult = DialogResult.OK;
            Close();
        };
        actionPanel.Controls.Add(btnOpen);

        var btnClose = new Button
        {
            Text = "Đóng",
            Width = 100,
            Height = 36
        };
        UiStyle.StyleButton(btnClose, UiStyle.Neutral);
        btnClose.Click += (_, _) =>
        {
            DialogResult = DialogResult.Cancel;
            Close();
        };
        actionPanel.Controls.Add(btnClose);

        KeyDown += (_, e) =>
        {
            if (e.KeyCode == Keys.Escape)
            {
                DialogResult = DialogResult.Cancel;
                Close();
            }
        };
    }
}
