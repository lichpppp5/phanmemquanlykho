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
        if (!_services.Session.IsManager)
        {
            throw new InvalidOperationException("Chỉ quản lý mới được truy cập màn hình này.");
        }

        Text = "Quản lý người dùng";
        Width = 900;
        Height = 620;
        StartPosition = FormStartPosition.CenterParent;
        KeyPreview = true;

        _grid = new DataGridView
        {
            Location = new Point(20, 20),
            Width = 840,
            Height = 300,
            ReadOnly = true,
            AllowUserToAddRows = false,
            AutoGenerateColumns = false
        };
        _grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = nameof(UserAccount.UserID), HeaderText = "ID", Width = 60 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = nameof(UserAccount.Username), HeaderText = "Username", Width = 160 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = nameof(UserAccount.FullName), HeaderText = "Họ tên", Width = 250 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = nameof(UserAccount.Role), HeaderText = "Vai trò", Width = 130 });
        _grid.Columns.Add(new DataGridViewCheckBoxColumn { DataPropertyName = nameof(UserAccount.IsActive), HeaderText = "Kích hoạt", Width = 100 });
        _grid.SelectionChanged += (_, _) => LoadSelectedUser();
        Controls.Add(_grid);

        Controls.Add(new Label { Text = "Username:", AutoSize = true, Location = new Point(20, 350) });
        _txtUsername = new TextBox { Location = new Point(90, 346), Width = 170 };
        Controls.Add(_txtUsername);

        Controls.Add(new Label { Text = "Họ tên:", AutoSize = true, Location = new Point(280, 350) });
        _txtFullName = new TextBox { Location = new Point(330, 346), Width = 240 };
        Controls.Add(_txtFullName);

        Controls.Add(new Label { Text = "Vai trò:", AutoSize = true, Location = new Point(590, 350) });
        _cmbRole = new ComboBox { Location = new Point(640, 346), Width = 120, DropDownStyle = ComboBoxStyle.DropDownList };
        _cmbRole.Items.AddRange(["Staff", "Manager"]);
        _cmbRole.SelectedIndex = 0;
        Controls.Add(_cmbRole);

        Controls.Add(new Label { Text = "Mật khẩu:", AutoSize = true, Location = new Point(20, 395) });
        _txtPassword = new TextBox { Location = new Point(90, 391), Width = 170 };
        Controls.Add(_txtPassword);

        _chkActive = new CheckBox { Text = "Kích hoạt tài khoản", AutoSize = true, Location = new Point(280, 393), Checked = true };
        Controls.Add(_chkActive);

        var btnCreate = new Button { Text = "Tạo user", Width = 110, Location = new Point(20, 450) };
        btnCreate.Click += (_, _) => CreateUser();
        Controls.Add(btnCreate);

        var btnUpdate = new Button { Text = "Cập nhật vai trò/trạng thái", Width = 200, Location = new Point(145, 450) };
        btnUpdate.Click += (_, _) => UpdateUser();
        Controls.Add(btnUpdate);

        var btnResetPassword = new Button { Text = "Reset mật khẩu", Width = 140, Location = new Point(360, 450) };
        btnResetPassword.Click += (_, _) => ResetPassword();
        Controls.Add(btnResetPassword);

        var btnClear = new Button { Text = "Làm mới", Width = 100, Location = new Point(515, 450) };
        btnClear.Click += (_, _) => ResetEditor();
        Controls.Add(btnClear);

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
        _cmbRole.SelectedItem = user.Role;
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
}
