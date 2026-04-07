using PMTapHoa.Desktop.Models;

namespace PMTapHoa.Desktop.Forms;

public class CategoryForm : Form
{
    private readonly AppServices _services;
    private readonly DataGridView _grid;
    private readonly TextBox _txtCategoryName;
    private int? _selectedCategoryId;
    private List<Category> _categories = [];

    public CategoryForm(AppServices services)
    {
        _services = services;

        Text = "Quản lý danh mục";
        Width = 650;
        Height = 520;
        StartPosition = FormStartPosition.CenterParent;
        KeyPreview = true;

        _grid = new DataGridView
        {
            Location = new Point(20, 20),
            Width = 590,
            Height = 300,
            ReadOnly = true,
            AllowUserToAddRows = false,
            AutoGenerateColumns = false
        };
        _grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = nameof(Category.CategoryID), HeaderText = "ID", Width = 80 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = nameof(Category.CategoryName), HeaderText = "Tên danh mục", Width = 460 });
        _grid.SelectionChanged += (_, _) => LoadSelected();

        var lbl = new Label
        {
            Text = "Tên danh mục:",
            AutoSize = true,
            Location = new Point(20, 350)
        };
        _txtCategoryName = new TextBox
        {
            Width = 320,
            Location = new Point(115, 346)
        };

        var btnAdd = new Button
        {
            Text = "Thêm",
            Width = 100,
            Location = new Point(20, 395)
        };
        btnAdd.Click += (_, _) => Create();

        var btnUpdate = new Button
        {
            Text = "Sửa",
            Width = 100,
            Location = new Point(130, 395)
        };
        btnUpdate.Click += (_, _) => UpdateCategory();

        var btnDelete = new Button
        {
            Text = "Xóa",
            Width = 100,
            Location = new Point(240, 395)
        };
        btnDelete.Click += (_, _) => DeleteCategory();

        var btnClear = new Button
        {
            Text = "Làm mới",
            Width = 100,
            Location = new Point(350, 395)
        };
        btnClear.Click += (_, _) => ResetEditor();

        if (!_services.Session.IsManager)
        {
            btnAdd.Enabled = false;
            btnUpdate.Enabled = false;
            btnDelete.Enabled = false;
            _txtCategoryName.ReadOnly = true;
        }

        Controls.Add(_grid);
        Controls.Add(lbl);
        Controls.Add(_txtCategoryName);
        Controls.Add(btnAdd);
        Controls.Add(btnUpdate);
        Controls.Add(btnDelete);
        Controls.Add(btnClear);

        Load += (_, _) => LoadGrid();
        KeyDown += (_, e) =>
        {
            if (e.KeyCode == Keys.Escape)
            {
                Close();
            }
        };
    }

    private void LoadGrid()
    {
        _categories = _services.CategoryService.GetAll();
        _grid.DataSource = null;
        _grid.DataSource = _categories;
    }

    private void LoadSelected()
    {
        if (_grid.CurrentRow?.DataBoundItem is not Category selected)
        {
            return;
        }

        _selectedCategoryId = selected.CategoryID;
        _txtCategoryName.Text = selected.CategoryName;
    }

    private void Create()
    {
        try
        {
            var id = _services.CategoryService.Create(_txtCategoryName.Text);
            _services.AuditService.Log(_services.Session.CurrentUser?.Username, "CREATE_CATEGORY", $"CategoryID={id}, Name={_txtCategoryName.Text.Trim()}");
            MessageBox.Show($"Đã thêm danh mục ID={id}.", "Thành công", MessageBoxButtons.OK, MessageBoxIcon.Information);
            LoadGrid();
            ResetEditor();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Lỗi thêm danh mục: {ex.Message}", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void UpdateCategory()
    {
        try
        {
            if (!_selectedCategoryId.HasValue)
            {
                MessageBox.Show("Vui lòng chọn danh mục cần sửa.", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            _services.CategoryService.Update(_selectedCategoryId.Value, _txtCategoryName.Text);
            _services.AuditService.Log(_services.Session.CurrentUser?.Username, "UPDATE_CATEGORY", $"CategoryID={_selectedCategoryId.Value}, Name={_txtCategoryName.Text.Trim()}");
            MessageBox.Show("Đã cập nhật danh mục.", "Thành công", MessageBoxButtons.OK, MessageBoxIcon.Information);
            LoadGrid();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Lỗi cập nhật danh mục: {ex.Message}", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void DeleteCategory()
    {
        try
        {
            if (!_selectedCategoryId.HasValue)
            {
                MessageBox.Show("Vui lòng chọn danh mục cần xóa.", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var confirm = MessageBox.Show("Bạn chắc chắn muốn xóa danh mục này?", "Xác nhận", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (confirm != DialogResult.Yes)
            {
                return;
            }

            _services.CategoryService.Delete(_selectedCategoryId.Value);
            _services.AuditService.Log(_services.Session.CurrentUser?.Username, "DELETE_CATEGORY", $"CategoryID={_selectedCategoryId.Value}");
            MessageBox.Show("Đã xóa danh mục.", "Thành công", MessageBoxButtons.OK, MessageBoxIcon.Information);
            LoadGrid();
            ResetEditor();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Lỗi xóa danh mục: {ex.Message}", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void ResetEditor()
    {
        _selectedCategoryId = null;
        _txtCategoryName.Clear();
    }
}
