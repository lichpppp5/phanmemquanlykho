using PMTapHoa.Desktop.Data;

namespace PMTapHoa.Desktop.Services;

public class BackupService
{
    private readonly DatabaseContext _databaseContext;

    public BackupService(DatabaseContext databaseContext)
    {
        _databaseContext = databaseContext;
    }

    public void BackupTo(string destinationPath)
    {
        if (string.IsNullOrWhiteSpace(destinationPath))
        {
            throw new InvalidOperationException("Đường dẫn backup không hợp lệ.");
        }

        Directory.CreateDirectory(Path.GetDirectoryName(destinationPath)!);
        File.Copy(_databaseContext.DatabaseFilePath, destinationPath, true);
    }

    public void RestoreFrom(string backupPath)
    {
        if (string.IsNullOrWhiteSpace(backupPath) || !File.Exists(backupPath))
        {
            throw new FileNotFoundException("Không tìm thấy file backup.", backupPath);
        }

        File.Copy(backupPath, _databaseContext.DatabaseFilePath, true);
    }
}
