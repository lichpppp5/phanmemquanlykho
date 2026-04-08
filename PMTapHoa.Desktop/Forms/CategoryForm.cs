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
        Width = 980;
        Height = 700;
        MinimumSize = new Size(860, 620);
        StartPosition = FormStartPosition.CenterParent;
        WindowState = FormWindowState.Maximized;
        KeyPreview = true;
        BackColor = Color.WhiteSmoke;

        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 3,
            Padding = new Padding(14)
        };
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 82f));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 56f));
        Controls.Add(root);

        _grid = new DataGridView
        {
            Dock = DockStyle.Fill,
            ReadOnly = true,
            AllowUserToAddRows = false,
            AutoGenerateColumns = false,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            MultiSelect = false
        };
        _grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = nameof(Category.CategoryID), HeaderText = "ID", Width = 80 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = nameof(Category.CategoryName), HeaderText = "Tên danh mục", AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill });
        _grid.DefaultCellStyle.Font = new Font("Segoe UI", 10, FontStyle.Regular);
        _grid.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 10, FontStyle.Bold);
        _grid.ColumnHeadersHeight = 36;
        _grid.RowTemplate.Height = 32;
        _grid.AlternatingRowsDefaultCellStyle.BackColor = UiStyle.GridAltRow;
        _grid.SelectionChanged += (_, _) => LoadSelected();
        root.Controls.Add(_grid, 0, 0);

        var editorPanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            Padding = new Padding(4, 16, 4, 4)
        };
        root.Controls.Add(editorPanel, 0, 1);
        var lbl = new Label { Text = "Tên danh mục:", AutoSize = true, Margin = new Padding(0, 9, 8, 0) };
        _txtCategoryName = new TextBox
        {
            Width = 420,
            Font = new Font("Segoe UI", 10, FontStyle.Regular),
            Margin = new Padding(0, 4, 0, 0)
        };
        editorPanel.Controls.Add(lbl);
        editorPanel.Controls.Add(_txtCategoryName);

        var btnAdd = new Button
        {
            Text = "Thêm",
            Width = 100,
            Height = 36,
            BackColor = UiStyle.SuccessBright,
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat
        };
        btnAdd.Click += (_, _) => Create();

        var btnUpdate = new Button
        {
            Text = "Sửa",
            Width = 100,
            Height = 36,
            BackColor = UiStyle.Primary,
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat
        };
        btnUpdate.Click += (_, _) => UpdateCategory();

        var btnDelete = new Button
        {
            Text = "Xóa",
            Width = 100,
            Height = 36,
            BackColor = UiStyle.Danger,
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat
        };
        btnDelete.Click += (_, _) => DeleteCategory();

        var btnClear = new Button
        {
            Text = "Làm mới",
            Width = 100,
            Height = 36,
            BackColor = UiStyle.Neutral,
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat
        };
        btnClear.Click += (_, _) => ResetEditor();

        if (!_services.Session.IsAdmin)
        {
            btnAdd.Enabled = false;
            btnUpdate.Enabled = false;
            btnDelete.Enabled = false;
            _txtCategoryName.ReadOnly = true;
        }

        var actionPanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            Padding = new Padding(4, 8, 4, 4)
        };
        actionPanel.Controls.Add(btnAdd);
        actionPanel.Controls.Add(btnUpdate);
        actionPanel.Controls.Add(btnDelete);
        actionPanel.Controls.Add(btnClear);
        root.Controls.Add(actionPanel, 0, 2);

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
