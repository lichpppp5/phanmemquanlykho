namespace PMTapHoa.Desktop.Forms;

public class LoginForm : Form
{
    private readonly AppServices _services;
    private readonly TextBox _txtUsername;
    private readonly TextBox _txtPassword;

    public LoginForm(AppServices services)
    {
        _services = services;

        Text = "Đăng nhập hệ thống";
        Width = 420;
        Height = 260;
        StartPosition = FormStartPosition.CenterScreen;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        KeyPreview = true;

        var lblUsername = new Label
        {
            Text = "Tài khoản:",
            AutoSize = true,
            Location = new Point(35, 40)
        };
        _txtUsername = new TextBox
        {
            Width = 220,
            Location = new Point(120, 36)
        };

        var lblPassword = new Label
        {
            Text = "Mật khẩu:",
            AutoSize = true,
            Location = new Point(35, 85)
        };
        _txtPassword = new TextBox
        {
            Width = 220,
            Location = new Point(120, 81),
            PasswordChar = '*'
        };

        var btnLogin = new Button
        {
            Text = "Đăng nhập",
            Width = 110,
            Height = 36,
            Location = new Point(120, 135)
        };
        btnLogin.Click += (_, _) => HandleLogin();

        var lblHint = new Label
        {
            Text = "Mặc định: admin/admin123 hoặc staff/staff123",
            AutoSize = true,
            ForeColor = Color.DimGray,
            Location = new Point(35, 182)
        };

        Controls.Add(lblUsername);
        Controls.Add(_txtUsername);
        Controls.Add(lblPassword);
        Controls.Add(_txtPassword);
        Controls.Add(btnLogin);
        Controls.Add(lblHint);

        Shown += (_, _) => _txtUsername.Focus();
        KeyDown += LoginForm_KeyDown;
    }

    private void LoginForm_KeyDown(object? sender, KeyEventArgs e)
    {
        if (e.KeyCode == Keys.Enter)
        {
            HandleLogin();
            e.Handled = true;
        }
        else if (e.KeyCode == Keys.Escape)
        {
            DialogResult = DialogResult.Cancel;
            Close();
        }
    }

    private void HandleLogin()
    {
        try
        {
            var user = _services.AuthService.Authenticate(_txtUsername.Text, _txtPassword.Text);
            if (user == null)
            {
                _services.AuditService.Log(_txtUsername.Text.Trim(), "LOGIN_FAILED", "Sai thông tin đăng nhập.");
                MessageBox.Show("Sai tài khoản hoặc mật khẩu.", "Đăng nhập thất bại", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                _txtPassword.SelectAll();
                _txtPassword.Focus();
                return;
            }

            _services.Session.SetUser(user);
            _services.AuditService.Log(user.Username, "LOGIN_SUCCESS", "Đăng nhập thành công.");
            DialogResult = DialogResult.OK;
            Close();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Lỗi đăng nhập: {ex.Message}", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }
}
