namespace PMTapHoa.Desktop.Forms;

public class BackupRestoreForm : Form
{
    private readonly AppServices _services;

    public BackupRestoreForm(AppServices services)
    {
        _services = services;
        if (!_services.Session.IsManager)
        {
            throw new InvalidOperationException("Chỉ quản lý mới được sao lưu/khôi phục dữ liệu.");
        }

        Text = "Sao lưu / Khôi phục dữ liệu";
        Width = 520;
        Height = 250;
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        KeyPreview = true;

        var btnBackup = new Button
        {
            Text = "Sao lưu database",
            Width = 180,
            Height = 50,
            Location = new Point(40, 70)
        };
        btnBackup.Click += (_, _) => DoBackup();

        var btnRestore = new Button
        {
            Text = "Khôi phục từ file backup",
            Width = 220,
            Height = 50,
            Location = new Point(250, 70)
        };
        btnRestore.Click += (_, _) => DoRestore();

        Controls.Add(btnBackup);
        Controls.Add(btnRestore);

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
