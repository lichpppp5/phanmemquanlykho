namespace PMTapHoa.Desktop.Forms;

public class ChangePasswordForm : Form
{
    private readonly AppServices _services;
    private readonly TextBox _txtCurrent;
    private readonly TextBox _txtNew;
    private readonly TextBox _txtConfirm;

    public ChangePasswordForm(AppServices services)
    {
        _services = services;

        UiStyle.ApplyDialogStyle(this, "Đổi mật khẩu", new Size(760, 460), sizable: true);
        MinimumSize = new Size(700, 420);

        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 2,
            Padding = new Padding(20, 18, 20, 14)
        };
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 68f));
        Controls.Add(root);

        var formLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 3,
            Padding = new Padding(10, 14, 10, 8)
        };
        formLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 230f));
        formLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
        formLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 64f));
        formLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 64f));
        formLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 64f));
        root.Controls.Add(formLayout, 0, 0);

        formLayout.Controls.Add(new Label { Text = "Mật khẩu hiện tại:", AutoSize = true, Anchor = AnchorStyles.Left, Font = new Font("Segoe UI", 11, FontStyle.Bold) }, 0, 0);
        _txtCurrent = new TextBox { Dock = DockStyle.Fill, PasswordChar = '*', Font = new Font("Segoe UI", 11, FontStyle.Regular) };
        formLayout.Controls.Add(_txtCurrent, 1, 0);

        formLayout.Controls.Add(new Label { Text = "Mật khẩu mới:", AutoSize = true, Anchor = AnchorStyles.Left, Font = new Font("Segoe UI", 11, FontStyle.Bold) }, 0, 1);
        _txtNew = new TextBox { Dock = DockStyle.Fill, PasswordChar = '*', Font = new Font("Segoe UI", 11, FontStyle.Regular) };
        formLayout.Controls.Add(_txtNew, 1, 1);

        formLayout.Controls.Add(new Label { Text = "Xác nhận mật khẩu:", AutoSize = true, Anchor = AnchorStyles.Left, Font = new Font("Segoe UI", 11, FontStyle.Bold) }, 0, 2);
        _txtConfirm = new TextBox { Dock = DockStyle.Fill, PasswordChar = '*', Font = new Font("Segoe UI", 11, FontStyle.Regular) };
        formLayout.Controls.Add(_txtConfirm, 1, 2);

        var actionPanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = true,
            Padding = new Padding(10, 10, 10, 4)
        };
        root.Controls.Add(actionPanel, 0, 1);

        var btnSave = new Button { Text = "Lưu", Width = 140, Height = 42, Font = new Font("Segoe UI", 10, FontStyle.Bold) };
        UiStyle.StyleButton(btnSave, UiStyle.Success);
        btnSave.Click += (_, _) => SaveChange();
        actionPanel.Controls.Add(btnSave);

        KeyDown += (_, e) =>
        {
            if (e.KeyCode == Keys.Enter)
            {
                SaveChange();
            }
            else if (e.KeyCode == Keys.Escape)
            {
                Close();
            }
        };
    }

    private void SaveChange()
    {
        try
        {
            var user = _services.Session.CurrentUser;
            if (user == null)
            {
                MessageBox.Show("Phiên đăng nhập không hợp lệ.", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            if (_txtNew.Text != _txtConfirm.Text)
            {
                MessageBox.Show("Mật khẩu xác nhận không khớp.", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var success = _services.AuthService.ChangePassword(user.UserID, _txtCurrent.Text, _txtNew.Text);
            if (!success)
            {
                MessageBox.Show("Mật khẩu hiện tại không đúng.", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            _services.AuditService.Log(user.Username, "CHANGE_PASSWORD", "Người dùng tự đổi mật khẩu.");
            MessageBox.Show("Đổi mật khẩu thành công.", "Thành công", MessageBoxButtons.OK, MessageBoxIcon.Information);
            Close();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Đổi mật khẩu thất bại: {ex.Message}", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }
}
