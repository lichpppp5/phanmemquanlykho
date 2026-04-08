namespace PMTapHoa.Desktop.Forms;

public class QuickNewProductPopupForm : Form
{
    private readonly TextBox _txtName;
    private readonly NumericUpDown _numQty;

    public string ProductName => _txtName.Text.Trim();
    public double Quantity => (double)_numQty.Value;

    public QuickNewProductPopupForm(string barcode)
    {
        UiStyle.ApplyDialogStyle(this, "Mã mới - Nhập nhanh", new Size(420, 230), sizable: false);

        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 3,
            Padding = new Padding(14)
        };
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 36f));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 54f));
        Controls.Add(root);

        var lblBarcode = new Label
        {
            Text = $"Mã mới: {barcode}",
            Dock = DockStyle.Fill,
            ForeColor = Color.DimGray
        };
        root.Controls.Add(lblBarcode, 0, 0);

        var formLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 2
        };
        formLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 90f));
        formLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
        root.Controls.Add(formLayout, 0, 1);

        formLayout.Controls.Add(new Label { Text = "Tên hàng:", AutoSize = true, Anchor = AnchorStyles.Left }, 0, 0);
        _txtName = new TextBox { Dock = DockStyle.Fill, Font = new Font("Segoe UI", 10, FontStyle.Regular) };
        formLayout.Controls.Add(_txtName, 1, 0);

        formLayout.Controls.Add(new Label { Text = "Số lượng:", AutoSize = true, Anchor = AnchorStyles.Left }, 0, 1);
        _numQty = new NumericUpDown
        {
            Dock = DockStyle.Left,
            Width = 120,
            DecimalPlaces = 2,
            Minimum = 1,
            Maximum = 100000,
            Value = 1
        };
        formLayout.Controls.Add(_numQty, 1, 1);

        var actionPanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            Padding = new Padding(0, 6, 0, 0)
        };
        root.Controls.Add(actionPanel, 0, 2);

        var btnSave = new Button { Text = "Lưu nhanh", Width = 110, Height = 34 };
        UiStyle.StyleButton(btnSave, UiStyle.Success);
        btnSave.Click += (_, _) => ConfirmAndClose();
        actionPanel.Controls.Add(btnSave);

        var btnCancel = new Button { Text = "Hủy", Width = 90, Height = 34 };
        UiStyle.StyleButton(btnCancel, UiStyle.Neutral);
        btnCancel.Click += (_, _) =>
        {
            DialogResult = DialogResult.Cancel;
            Close();
        };
        actionPanel.Controls.Add(btnCancel);

        Shown += (_, _) => _txtName.Focus();
        KeyDown += (_, e) =>
        {
            if (e.KeyCode == Keys.Enter)
            {
                ConfirmAndClose();
                e.Handled = true;
            }
            else if (e.KeyCode == Keys.Escape)
            {
                DialogResult = DialogResult.Cancel;
                Close();
            }
        };
    }

    private void ConfirmAndClose()
    {
        if (string.IsNullOrWhiteSpace(_txtName.Text))
        {
            MessageBox.Show("Vui lòng nhập tên hàng.", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
            _txtName.Focus();
            return;
        }

        if (_numQty.Value <= 0)
        {
            MessageBox.Show("Số lượng phải lớn hơn 0.", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        DialogResult = DialogResult.OK;
        Close();
    }
}
