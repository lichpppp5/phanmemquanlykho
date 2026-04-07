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
        Width = 1100;
        Height = 700;
        StartPosition = FormStartPosition.CenterParent;
        KeyPreview = true;

        var lblSearch = new Label
        {
            Text = "Tìm khách / mã HĐ:",
            AutoSize = true,
            Location = new Point(20, 20)
        };
        _txtSearch = new TextBox
        {
            Width = 250,
            Location = new Point(140, 16)
        };
        _txtSearch.TextChanged += (_, _) => LoadDebts();

        _gridDebts = new DataGridView
        {
            Location = new Point(20, 55),
            Width = 1040,
            Height = 280,
            ReadOnly = true,
            AllowUserToAddRows = false,
            AutoGenerateColumns = false
        };
        _gridDebts.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = nameof(DebtSaleItem.SaleID), HeaderText = "Hóa đơn", Width = 90 });
        _gridDebts.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = nameof(DebtSaleItem.SaleDate), HeaderText = "Ngày bán", Width = 170 });
        _gridDebts.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = nameof(DebtSaleItem.CustomerName), HeaderText = "Khách hàng", Width = 260 });
        _gridDebts.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = nameof(DebtSaleItem.TotalAmount), HeaderText = "Tổng tiền", Width = 150 });
        _gridDebts.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = nameof(DebtSaleItem.PaidAmount), HeaderText = "Đã trả", Width = 150 });
        _gridDebts.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = nameof(DebtSaleItem.OutstandingAmount), HeaderText = "Còn nợ", Width = 150 });
        _gridDebts.SelectionChanged += (_, _) => LoadSelectedPaymentHistory();

        var paymentPanel = new GroupBox
        {
            Text = "Thu nợ từng phần",
            Location = new Point(20, 350),
            Width = 1040,
            Height = 120
        };

        var lblAmount = new Label { Text = "Số tiền thu:", AutoSize = true, Location = new Point(20, 38) };
        _numPaymentAmount = new NumericUpDown
        {
            Location = new Point(90, 34),
            Width = 160,
            DecimalPlaces = 2,
            Maximum = 1000000000
        };

        var lblNote = new Label { Text = "Ghi chú:", AutoSize = true, Location = new Point(270, 38) };
        _txtNote = new TextBox { Location = new Point(325, 34), Width = 360 };

        var btnCollect = new Button
        {
            Text = "Thu nợ",
            Width = 120,
            Location = new Point(710, 32)
        };
        btnCollect.Click += (_, _) => CollectDebt();

        if (!_services.Session.IsManager)
        {
            btnCollect.Enabled = false;
        }

        paymentPanel.Controls.Add(lblAmount);
        paymentPanel.Controls.Add(_numPaymentAmount);
        paymentPanel.Controls.Add(lblNote);
        paymentPanel.Controls.Add(_txtNote);
        paymentPanel.Controls.Add(btnCollect);

        _gridPayments = new DataGridView
        {
            Location = new Point(20, 490),
            Width = 1040,
            Height = 150,
            ReadOnly = true,
            AllowUserToAddRows = false,
            AutoGenerateColumns = false
        };
        _gridPayments.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = nameof(DebtPayment.PaymentDate), HeaderText = "Ngày thu", Width = 200 });
        _gridPayments.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = nameof(DebtPayment.Amount), HeaderText = "Số tiền", Width = 180 });
        _gridPayments.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = nameof(DebtPayment.Note), HeaderText = "Ghi chú", Width = 620 });

        Controls.Add(lblSearch);
        Controls.Add(_txtSearch);
        Controls.Add(_gridDebts);
        Controls.Add(paymentPanel);
        Controls.Add(_gridPayments);

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
