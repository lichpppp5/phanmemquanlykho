using PMTapHoa.Desktop.Data;
using PMTapHoa.Desktop.Services;

namespace PMTapHoa.Desktop;

public class AppServices
{
    public DatabaseContext DatabaseContext { get; }
    public ProductService ProductService { get; }
    public SalesService SalesService { get; }
    public InventoryService InventoryService { get; }
    public ReceiptService ReceiptService { get; }
    public DebtService DebtService { get; }
    public CategoryService CategoryService { get; }
    public ExportService ExportService { get; }
    public AuthService AuthService { get; }
    public AppSession Session { get; }
    public UserService UserService { get; }
    public AuditService AuditService { get; }
    public AppConfigService AppConfigService { get; }
    public BackupService BackupService { get; }
    public HealthCheckService HealthCheckService { get; }
    public RestockService RestockService { get; }
    public QrPaymentService QrPaymentService { get; }
    public CustomerService CustomerService { get; }
    public DiscountService DiscountService { get; }
    public NotificationService NotificationService { get; }

    public AppServices(string appDataDirectory, string sqlScriptPath)
    {
        Directory.CreateDirectory(appDataDirectory);

        var dbPath = Path.Combine(appDataDirectory, "taphoa.db");
        DatabaseContext = new DatabaseContext(dbPath);
        DatabaseContext.InitializeDatabase(sqlScriptPath);

        ProductService = new ProductService(DatabaseContext);
        SalesService = new SalesService(DatabaseContext);
        InventoryService = new InventoryService();
        ReceiptService = new ReceiptService();
        DebtService = new DebtService(DatabaseContext);
        CategoryService = new CategoryService(DatabaseContext);
        ExportService = new ExportService();
        AuthService = new AuthService(DatabaseContext);
        Session = new AppSession();
        UserService = new UserService(DatabaseContext);
        AuditService = new AuditService(DatabaseContext);
        AppConfigService = new AppConfigService(DatabaseContext);
        BackupService = new BackupService(DatabaseContext);
        HealthCheckService = new HealthCheckService(DatabaseContext, AppConfigService);
        RestockService = new RestockService(DatabaseContext);
        QrPaymentService = new QrPaymentService(AppConfigService);
        CustomerService = new CustomerService(DatabaseContext);
        DiscountService = new DiscountService();
        NotificationService = new NotificationService(InventoryService, ProductService, DebtService);

        AuthService.EnsureDefaultUsers();
    }
}
