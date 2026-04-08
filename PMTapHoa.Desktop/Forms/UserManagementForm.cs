using PMTapHoa.Desktop.Models;

namespace PMTapHoa.Desktop.Forms;

public class UserManagementForm : Form
{
    private readonly AppServices _services;
    private readonly DataGridView _grid;
    private readonly TextBox _txtUsername;
    private readonly TextBox _txtFullName;
    private readonly ComboBox _cmbRole;
    private readonly TextBox _txtPassword;
    private readonly CheckBox _chkActive;
    private List<UserAccount> _users = [];
    private int? _selectedUserId;

    public UserManagementForm(AppServices services)
    {
        _services = services;
        if (!_services.Session.IsAdmin)
        {
            throw new InvalidOperationException("Chỉ Admin mới được truy cập màn hình này.");
        }

        Text = "Quản lý người dùng";
        Width = 1200;
        Height = 820;
        UiStyle.ApplyMainFormStyle(this, Text, new Size(1000, 700));

        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 3,
            Padding = new Padding(14)
        };
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 58f));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 160f));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 64f));
        Controls.Add(root);

        _grid = new DataGridView
        {
            Dock = DockStyle.Fill
        };
        _grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = nameof(UserAccount.UserID), HeaderText = "ID", Width = 60 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = nameof(UserAccount.Username), HeaderText = "Username", Width = 160 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = nameof(UserAccount.FullName), HeaderText = "Họ tên", Width = 250 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = nameof(UserAccount.Role), HeaderText = "Vai trò", Width = 130 });
        _grid.Columns.Add(new DataGridViewCheckBoxColumn { DataPropertyName = nameof(UserAccount.IsActive), HeaderText = "Kích hoạt", Width = 100 });
        UiStyle.StyleGrid(_grid);
        _grid.SelectionChanged += (_, _) => LoadSelectedUser();
        root.Controls.Add(_grid, 0, 0);

        var editorBox = new GroupBox
        {
            Text = "Thông tin tài khoản",
            Dock = DockStyle.Fill,
            Font = new Font("Segoe UI", 10, FontStyle.Bold)
        };
        root.Controls.Add(editorBox, 0, 1);
        var editorLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 4,
            RowCount = 2,
            Padding = new Padding(10, 10, 10, 6)
        };
        editorLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 90f));
        editorLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 40f));
        editorLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 80f));
        editorLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 60f));
        editorBox.Controls.Add(editorLayout);

        editorLayout.Controls.Add(new Label { Text = "Username:", AutoSize = true, Anchor = AnchorStyles.Left }, 0, 0);
        _txtUsername = new TextBox { Dock = DockStyle.Fill, Font = new Font("Segoe UI", 10, FontStyle.Regular) };
        editorLayout.Controls.Add(_txtUsername, 1, 0);

        editorLayout.Controls.Add(new Label { Text = "Họ tên:", AutoSize = true, Anchor = AnchorStyles.Left }, 2, 0);
        _txtFullName = new TextBox { Dock = DockStyle.Fill, Font = new Font("Segoe UI", 10, FontStyle.Regular) };
        editorLayout.Controls.Add(_txtFullName, 3, 0);

        editorLayout.Controls.Add(new Label { Text = "Vai trò:", AutoSize = true, Anchor = AnchorStyles.Left }, 0, 1);
        _cmbRole = new ComboBox { Dock = DockStyle.Left, Width = 140, DropDownStyle = ComboBoxStyle.DropDownList, Font = new Font("Segoe UI", 10, FontStyle.Regular) };
        _cmbRole.Items.AddRange(["User", "Admin"]);
        _cmbRole.SelectedIndex = 0;
        editorLayout.Controls.Add(_cmbRole, 1, 1);

        editorLayout.Controls.Add(new Label { Text = "Mật khẩu:", AutoSize = true, Anchor = AnchorStyles.Left }, 2, 1);
        _txtPassword = new TextBox { Dock = DockStyle.Fill, Font = new Font("Segoe UI", 10, FontStyle.Regular) };
        editorLayout.Controls.Add(_txtPassword, 3, 1);

        _chkActive = new CheckBox { Text = "Kích hoạt tài khoản", AutoSize = true, Checked = true, Anchor = AnchorStyles.Left };
        editorLayout.Controls.Add(_chkActive, 1, 1);
        editorLayout.SetColumnSpan(_chkActive, 1);

        var actionPanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            Padding = new Padding(4, 10, 4, 4)
        };
        root.Controls.Add(actionPanel, 0, 2);

        var btnCreate = new Button { Text = "Tạo user", Width = 120, Height = 38 };
        UiStyle.StyleButton(btnCreate, UiStyle.SuccessBright);
        btnCreate.Click += (_, _) => CreateUser();
        actionPanel.Controls.Add(btnCreate);

        var btnUpdate = new Button { Text = "Cập nhật vai trò/trạng thái", Width = 220, Height = 38 };
        UiStyle.StyleButton(btnUpdate, UiStyle.Primary);
        btnUpdate.Click += (_, _) => UpdateUser();
        actionPanel.Controls.Add(btnUpdate);

        var btnResetPassword = new Button { Text = "Reset mật khẩu", Width = 150, Height = 38 };
        UiStyle.StyleButton(btnResetPassword, UiStyle.Warning);
        btnResetPassword.Click += (_, _) => ResetPassword();
        actionPanel.Controls.Add(btnResetPassword);

        var btnClear = new Button { Text = "Làm mới", Width = 110, Height = 38 };
        UiStyle.StyleButton(btnClear, UiStyle.Neutral);
        btnClear.Click += (_, _) => ResetEditor();
        actionPanel.Controls.Add(btnClear);

        Load += (_, _) => LoadUsers();
        KeyDown += (_, e) =>
        {
            if (e.KeyCode == Keys.Escape)
            {
                Close();
            }
        };
    }

    private void LoadUsers()
    {
        _users = _services.UserService.GetAll();
        _grid.DataSource = null;
        _grid.DataSource = _users;
    }

    private void LoadSelectedUser()
    {
        if (_grid.CurrentRow?.DataBoundItem is not UserAccount user)
        {
            return;
        }

        _selectedUserId = user.UserID;
        _txtUsername.Text = user.Username;
        _txtFullName.Text = user.FullName;
        _cmbRole.SelectedItem = NormalizeRoleForUi(user.Role);
        _chkActive.Checked = user.IsActive;
    }

    private void CreateUser()
    {
        try
        {
            var userId = _services.UserService.Create(_txtUsername.Text, _txtFullName.Text, _cmbRole.Text, _txtPassword.Text);
            _services.AuditService.Log(_services.Session.CurrentUser?.Username, "CREATE_USER", $"UserID={userId}, Username={_txtUsername.Text.Trim()}");
            MessageBox.Show("Đã tạo người dùng.", "Thành công", MessageBoxButtons.OK, MessageBoxIcon.Information);
            LoadUsers();
            ResetEditor();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Tạo user thất bại: {ex.Message}", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void UpdateUser()
    {
        try
        {
            if (!_selectedUserId.HasValue)
            {
                MessageBox.Show("Vui lòng chọn user cần cập nhật.", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            _services.UserService.UpdateRoleAndStatus(_selectedUserId.Value, _cmbRole.Text, _chkActive.Checked);
            _services.AuditService.Log(_services.Session.CurrentUser?.Username, "UPDATE_USER", $"UserID={_selectedUserId.Value}, Role={_cmbRole.Text}, Active={_chkActive.Checked}");
            MessageBox.Show("Đã cập nhật user.", "Thành công", MessageBoxButtons.OK, MessageBoxIcon.Information);
            LoadUsers();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Cập nhật user thất bại: {ex.Message}", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void ResetPassword()
    {
        try
        {
            if (!_selectedUserId.HasValue)
            {
                MessageBox.Show("Vui lòng chọn user để reset mật khẩu.", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            if (string.IsNullOrWhiteSpace(_txtPassword.Text))
            {
                MessageBox.Show("Nhập mật khẩu mới vào ô Mật khẩu.", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            _services.UserService.ResetPassword(_selectedUserId.Value, _txtPassword.Text);
            _services.AuditService.Log(_services.Session.CurrentUser?.Username, "RESET_PASSWORD", $"Reset password cho UserID={_selectedUserId.Value}");
            MessageBox.Show("Đã reset mật khẩu.", "Thành công", MessageBoxButtons.OK, MessageBoxIcon.Information);
            _txtPassword.Clear();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Reset mật khẩu thất bại: {ex.Message}", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void ResetEditor()
    {
        _selectedUserId = null;
        _txtUsername.Clear();
        _txtFullName.Clear();
        _txtPassword.Clear();
        _cmbRole.SelectedIndex = 0;
        _chkActive.Checked = true;
    }

    private static string NormalizeRoleForUi(string role)
    {
        return string.Equals(role, "Admin", StringComparison.OrdinalIgnoreCase)
               || string.Equals(role, "Manager", StringComparison.OrdinalIgnoreCase)
            ? "Admin"
            : "User";
    }
}
