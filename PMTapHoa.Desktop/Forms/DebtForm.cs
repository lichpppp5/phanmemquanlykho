using PMTapHoa.Desktop.Models;

namespace PMTapHoa.Desktop.Forms;

public class DebtForm : Form
{
    private readonly AppServices _services;
    private readonly TextBox _txtSearch;
    private readonly DataGridView _gridDebts;
    private readonly DataGridView _gridPayments;
    private readonly NumericUpDown _numPaymentAmount;
    private readonly TextBox _txtNote;
    private List<DebtSaleItem> _debtItems = [];

    public DebtForm(AppServices services)
    {
        _services = services;

        Text = "Quản lý công nợ khách hàng";
        Width = 1280;
        Height = 820;
        MinimumSize = new Size(1160, 720);
        StartPosition = FormStartPosition.CenterParent;
        WindowState = FormWindowState.Normal;
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
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 56f));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 54f));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 130f));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 46f));
        Controls.Add(root);

        var searchPanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = true,
            Padding = new Padding(4, 10, 4, 4)
        };
        root.Controls.Add(searchPanel, 0, 0);

        var lblSearch = new Label
        {
            Text = "Tìm khách / mã HĐ:",
            AutoSize = true,
            Margin = new Padding(0, 8, 8, 0)
        };
        _txtSearch = new TextBox
        {
            Width = 280,
            Margin = new Padding(0, 4, 0, 0),
            Font = new Font("Segoe UI", 10, FontStyle.Regular)
        };
        _txtSearch.TextChanged += (_, _) => LoadDebts();
        searchPanel.Controls.Add(lblSearch);
        searchPanel.Controls.Add(_txtSearch);

        _gridDebts = new DataGridView
        {
            Dock = DockStyle.Fill,
            ReadOnly = true,
            AllowUserToAddRows = false,
            AutoGenerateColumns = false,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            MultiSelect = false
        };
        _gridDebts.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = nameof(DebtSaleItem.SaleID), HeaderText = "Hóa đơn", Width = 90 });
        _gridDebts.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = nameof(DebtSaleItem.SaleDate), HeaderText = "Ngày bán", Width = 170 });
        _gridDebts.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = nameof(DebtSaleItem.CustomerName), HeaderText = "Khách hàng", Width = 260 });
        _gridDebts.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = nameof(DebtSaleItem.TotalAmount), HeaderText = "Tổng tiền", Width = 150 });
        _gridDebts.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = nameof(DebtSaleItem.PaidAmount), HeaderText = "Đã trả", Width = 150 });
        _gridDebts.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = nameof(DebtSaleItem.OutstandingAmount), HeaderText = "Còn nợ", Width = 150 });
        _gridDebts.DefaultCellStyle.Font = new Font("Segoe UI", 10, FontStyle.Regular);
        _gridDebts.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 10, FontStyle.Bold);
        _gridDebts.ColumnHeadersHeight = 36;
        _gridDebts.RowTemplate.Height = 32;
        _gridDebts.AlternatingRowsDefaultCellStyle.BackColor = UiStyle.GridAltRow;
        _gridDebts.SelectionChanged += (_, _) => LoadSelectedPaymentHistory();
        root.Controls.Add(_gridDebts, 0, 1);

        var paymentPanel = new GroupBox
        {
            Text = "Thu nợ từng phần",
            Dock = DockStyle.Fill,
            Font = new Font("Segoe UI", 10, FontStyle.Bold)
        };
        root.Controls.Add(paymentPanel, 0, 2);

        var paymentLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 6,
            RowCount = 1,
            Padding = new Padding(12, 14, 12, 12)
        };
        paymentLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 11f));
        paymentLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 16f));
        paymentLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 9f));
        paymentLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 44f));
        paymentLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 18f));
        paymentLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 2f));
        paymentPanel.Controls.Add(paymentLayout);

        var lblAmount = new Label { Text = "Số tiền thu:", AutoSize = true, Anchor = AnchorStyles.Left };
        _numPaymentAmount = new NumericUpDown
        {
            Dock = DockStyle.Fill,
            DecimalPlaces = 2,
            Maximum = 1000000000
        };

        var lblNote = new Label { Text = "Ghi chú:", AutoSize = true, Anchor = AnchorStyles.Left };
        _txtNote = new TextBox { Dock = DockStyle.Fill, Font = new Font("Segoe UI", 10, FontStyle.Regular) };

        var btnCollect = new Button
        {
            Text = "Thu nợ",
            Dock = DockStyle.Fill,
            Height = 38,
            BackColor = UiStyle.SuccessBright,
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat
        };
        btnCollect.Click += (_, _) => CollectDebt();

        if (!_services.Session.IsAdmin)
        {
            btnCollect.Enabled = false;
        }

        paymentLayout.Controls.Add(lblAmount, 0, 0);
        paymentLayout.Controls.Add(_numPaymentAmount, 1, 0);
        paymentLayout.Controls.Add(lblNote, 2, 0);
        paymentLayout.Controls.Add(_txtNote, 3, 0);
        paymentLayout.Controls.Add(btnCollect, 4, 0);

        _gridPayments = new DataGridView
        {
            Dock = DockStyle.Fill,
            ReadOnly = true,
            AllowUserToAddRows = false,
            AutoGenerateColumns = false
        };
        _gridPayments.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = nameof(DebtPayment.PaymentDate), HeaderText = "Ngày thu", Width = 200 });
        _gridPayments.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = nameof(DebtPayment.Amount), HeaderText = "Số tiền", Width = 180 });
        _gridPayments.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = nameof(DebtPayment.Note), HeaderText = "Ghi chú", Width = 620 });
        _gridPayments.DefaultCellStyle.Font = new Font("Segoe UI", 10, FontStyle.Regular);
        _gridPayments.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 10, FontStyle.Bold);
        _gridPayments.ColumnHeadersHeight = 36;
        _gridPayments.RowTemplate.Height = 32;
        _gridPayments.AlternatingRowsDefaultCellStyle.BackColor = UiStyle.GridAltRow;
        root.Controls.Add(_gridPayments, 0, 3);

        Load += (_, _) => LoadDebts();
        KeyDown += (_, e) =>
        {
            if (e.KeyCode == Keys.Escape)
            {
                Close();
            }
        };
    }

    private void LoadDebts()
    {
        try
        {
            _debtItems = _services.DebtService.GetOutstandingDebts(_txtSearch.Text);
            _gridDebts.DataSource = null;
            _gridDebts.DataSource = _debtItems;
            LoadSelectedPaymentHistory();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Lỗi tải danh sách nợ: {ex.Message}", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void LoadSelectedPaymentHistory()
    {
        if (_gridDebts.CurrentRow?.DataBoundItem is not DebtSaleItem selectedDebt)
        {
            _gridPayments.DataSource = null;
            return;
        }

        var payments = _services.DebtService.GetPaymentsBySale(selectedDebt.SaleID);
        _gridPayments.DataSource = null;
        _gridPayments.DataSource = payments;
    }

    private void CollectDebt()
    {
        try
        {
            if (_gridDebts.CurrentRow?.DataBoundItem is not DebtSaleItem selectedDebt)
            {
                MessageBox.Show("Vui lòng chọn hóa đơn công nợ.", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var amount = _numPaymentAmount.Value;
            if (amount <= 0)
            {
                MessageBox.Show("Số tiền thu phải lớn hơn 0.", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            _services.DebtService.AddPayment(selectedDebt.SaleID, amount, _txtNote.Text.Trim());
            _services.AuditService.Log(_services.Session.CurrentUser?.Username, "COLLECT_DEBT", $"SaleID={selectedDebt.SaleID}, Amount={amount}");
            MessageBox.Show("Đã thu nợ thành công.", "Thành công", MessageBoxButtons.OK, MessageBoxIcon.Information);
            _numPaymentAmount.Value = 0;
            _txtNote.Clear();
            LoadDebts();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Thu nợ thất bại: {ex.Message}", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }
}
