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

        Text = "Đổi mật khẩu";
        Width = 430;
        Height = 290;
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        KeyPreview = true;

        Controls.Add(new Label { Text = "Mật khẩu hiện tại:", AutoSize = true, Location = new Point(30, 40) });
        _txtCurrent = new TextBox { Location = new Point(165, 36), Width = 210, PasswordChar = '*' };
        Controls.Add(_txtCurrent);

        Controls.Add(new Label { Text = "Mật khẩu mới:", AutoSize = true, Location = new Point(30, 85) });
        _txtNew = new TextBox { Location = new Point(165, 81), Width = 210, PasswordChar = '*' };
        Controls.Add(_txtNew);

        Controls.Add(new Label { Text = "Xác nhận mật khẩu:", AutoSize = true, Location = new Point(30, 130) });
        _txtConfirm = new TextBox { Location = new Point(165, 126), Width = 210, PasswordChar = '*' };
        Controls.Add(_txtConfirm);

        var btnSave = new Button { Text = "Lưu", Width = 100, Height = 36, Location = new Point(165, 180) };
        btnSave.Click += (_, _) => SaveChange();
        Controls.Add(btnSave);

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
