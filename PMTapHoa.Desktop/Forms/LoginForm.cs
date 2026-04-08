namespace PMTapHoa.Desktop.Forms;

public class LoginForm : Form
{
    private readonly AppServices _services;
    private readonly TextBox _txtUsername;
    private readonly TextBox _txtPassword;

    public LoginForm(AppServices services)
    {
        _services = services;

        UiStyle.ApplyDialogStyle(this, "Đăng nhập hệ thống", new Size(760, 500), sizable: true);
        StartPosition = FormStartPosition.CenterScreen;
        MinimumSize = new Size(700, 460);

        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 4,
            Padding = new Padding(22, 18, 22, 16)
        };
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        Controls.Add(root);

        var lblTitle = new Label
        {
            Text = "ĐĂNG NHẬP HỆ THỐNG",
            AutoSize = true,
            Font = new Font("Segoe UI", 22, FontStyle.Bold),
            Margin = new Padding(0, 4, 0, 10)
        };
        root.Controls.Add(lblTitle, 0, 0);

        var formLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 2,
            Padding = new Padding(8, 8, 8, 0)
        };
        formLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 140f));
        formLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
        formLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 62f));
        formLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 62f));
        root.Controls.Add(formLayout, 0, 1);

        var lblUsername = new Label
        {
            Text = "Tài khoản:",
            AutoSize = true,
            Anchor = AnchorStyles.Left
        };
        _txtUsername = new TextBox
        {
            Dock = DockStyle.Fill,
            Font = new Font("Segoe UI", 12, FontStyle.Regular)
        };

        var lblPassword = new Label
        {
            Text = "Mật khẩu:",
            AutoSize = true,
            Anchor = AnchorStyles.Left
        };
        _txtPassword = new TextBox
        {
            Dock = DockStyle.Fill,
            Font = new Font("Segoe UI", 12, FontStyle.Regular),
            PasswordChar = '*'
        };

        formLayout.Controls.Add(lblUsername, 0, 0);
        formLayout.Controls.Add(_txtUsername, 1, 0);
        formLayout.Controls.Add(lblPassword, 0, 1);
        formLayout.Controls.Add(_txtPassword, 1, 1);

        var btnLogin = new Button
        {
            Text = "Đăng nhập",
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            MinimumSize = new Size(160, 42),
            Padding = new Padding(14, 0, 14, 0)
        };
        UiStyle.StyleButton(btnLogin, UiStyle.Success);
        btnLogin.Click += (_, _) => HandleLogin();
        var actionPanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = true,
            Padding = new Padding(8, 2, 8, 4),
            Margin = new Padding(0, 6, 0, 0)
        };
        actionPanel.Controls.Add(btnLogin);
        root.Controls.Add(actionPanel, 0, 2);

        var lblHint = new Label
        {
            Text = "Mặc định: admin/admin123 hoặc staff/staff123",
            AutoSize = true,
            MaximumSize = new Size(680, 0),
            ForeColor = Color.DimGray,
            Font = new Font("Segoe UI", 10, FontStyle.Regular),
            Margin = new Padding(8, 0, 8, 0)
        };
        root.Controls.Add(lblHint, 0, 3);

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
