using PMTapHoa.Desktop.Models;

namespace PMTapHoa.Desktop.Forms;

public class LowStockReminderForm : Form
{
    private readonly CheckBox _chkDontRemindToday;

    public bool DontRemindToday => _chkDontRemindToday.Checked;

    public LowStockReminderForm(List<Product> lowStockProducts)
    {
        Text = "Cảnh báo hàng sắp hết";
        Width = 760;
        Height = 500;
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        KeyPreview = true;

        var title = new Label
        {
            Text = $"Có {lowStockProducts.Count} mặt hàng đang dưới mức tồn tối thiểu.",
            AutoSize = true,
            Font = new Font("Segoe UI", 11, FontStyle.Bold),
            Location = new Point(20, 18)
        };
        Controls.Add(title);

        var hint = new Label
        {
            Text = "Bạn có muốn mở màn hình Mối nhập hàng để tạo yêu cầu nhập ngay?",
            AutoSize = true,
            Location = new Point(20, 46)
        };
        Controls.Add(hint);

        var grid = new DataGridView
        {
            Location = new Point(20, 75),
            Width = 700,
            Height = 310,
            ReadOnly = true,
            AllowUserToAddRows = false,
            AutoGenerateColumns = false
        };
        grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = nameof(Product.ProductName), HeaderText = "Sản phẩm", Width = 300 });
        grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = nameof(Product.Barcode), HeaderText = "Mã vạch", Width = 150 });
        grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = nameof(Product.StockQuantity), HeaderText = "Tồn", Width = 90 });
        grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = nameof(Product.MinStock), HeaderText = "Min", Width = 90 });
        grid.DataSource = lowStockProducts
            .OrderBy(p => p.StockQuantity - p.MinStock)
            .Take(50)
            .ToList();
        Controls.Add(grid);

        _chkDontRemindToday = new CheckBox
        {
            Text = "Không nhắc lại hôm nay",
            AutoSize = true,
            Location = new Point(20, 398)
        };
        Controls.Add(_chkDontRemindToday);

        var btnOpen = new Button
        {
            Text = "Mở Mối nhập hàng",
            Width = 160,
            Height = 36,
            Location = new Point(390, 420)
        };
        btnOpen.Click += (_, _) =>
        {
            DialogResult = DialogResult.OK;
            Close();
        };
        Controls.Add(btnOpen);

        var btnClose = new Button
        {
            Text = "Đóng",
            Width = 100,
            Height = 36,
            Location = new Point(565, 420)
        };
        btnClose.Click += (_, _) =>
        {
            DialogResult = DialogResult.Cancel;
            Close();
        };
        Controls.Add(btnClose);

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
