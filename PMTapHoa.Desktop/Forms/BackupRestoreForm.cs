namespace PMTapHoa.Desktop.Forms;

public class BackupRestoreForm : Form
{
    private readonly AppServices _services;

    public BackupRestoreForm(AppServices services)
    {
        _services = services;
        if (!_services.Session.IsAdmin)
        {
            throw new InvalidOperationException("Chỉ Admin mới được sao lưu/khôi phục dữ liệu.");
        }

        UiStyle.ApplyDialogStyle(this, "Sao lưu / Khôi phục dữ liệu", new Size(620, 320), sizable: false);

        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 3,
            Padding = new Padding(16)
        };
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 70f));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 58f));
        Controls.Add(root);

        var lblHint = new Label
        {
            Text = "Thực hiện sao lưu định kỳ để đảm bảo an toàn dữ liệu. Khôi phục sẽ ghi đè dữ liệu hiện tại.",
            Dock = DockStyle.Fill,
            Font = new Font("Segoe UI", 10, FontStyle.Regular)
        };
        root.Controls.Add(lblHint, 0, 0);

        var buttonPanel = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 1
        };
        buttonPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50f));
        buttonPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50f));
        root.Controls.Add(buttonPanel, 0, 1);

        var btnBackup = new Button
        {
            Text = "Sao lưu database",
            Dock = DockStyle.Fill,
            Height = 52,
            Margin = new Padding(0, 0, 10, 0)
        };
        UiStyle.StyleButton(btnBackup, UiStyle.Success);
        btnBackup.Click += (_, _) => DoBackup();

        var btnRestore = new Button
        {
            Text = "Khôi phục từ file backup",
            Dock = DockStyle.Fill,
            Height = 52,
            Margin = new Padding(10, 0, 0, 0)
        };
        UiStyle.StyleButton(btnRestore, UiStyle.Danger);
        btnRestore.Click += (_, _) => DoRestore();
        buttonPanel.Controls.Add(btnBackup, 0, 0);
        buttonPanel.Controls.Add(btnRestore, 1, 0);

        KeyDown += (_, e) =>
        {
            if (e.KeyCode == Keys.Escape)
            {
                Close();
            }
        };
    }

    private void DoBackup()
    {
        try
        {
            using var saveDialog = new SaveFileDialog
            {
                Filter = "SQLite Backup (*.db)|*.db",
                FileName = $"taphoa_backup_{DateTime.Now:yyyyMMdd_HHmmss}.db"
            };

            if (saveDialog.ShowDialog(this) != DialogResult.OK)
            {
                return;
            }

            _services.BackupService.BackupTo(saveDialog.FileName);
            _services.AuditService.Log(_services.Session.CurrentUser?.Username, "BACKUP_DATABASE", saveDialog.FileName);
            MessageBox.Show("Sao lưu thành công.", "Thành công", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Sao lưu thất bại: {ex.Message}", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void DoRestore()
    {
        try
        {
            var confirm = MessageBox.Show(
                "Khôi phục sẽ ghi đè dữ liệu hiện tại. Tiếp tục?",
                "Xác nhận khôi phục",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning);
            if (confirm != DialogResult.Yes)
            {
                return;
            }

            using var openDialog = new OpenFileDialog
            {
                Filter = "SQLite Backup (*.db)|*.db|All files (*.*)|*.*"
            };

            if (openDialog.ShowDialog(this) != DialogResult.OK)
            {
                return;
            }

            _services.BackupService.RestoreFrom(openDialog.FileName);
            _services.AuditService.Log(_services.Session.CurrentUser?.Username, "RESTORE_DATABASE", openDialog.FileName);
            MessageBox.Show("Khôi phục thành công. Ứng dụng sẽ khởi động lại.", "Thành công", MessageBoxButtons.OK, MessageBoxIcon.Information);
            Application.Restart();
            Environment.Exit(0);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Khôi phục thất bại: {ex.Message}", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }
}
