using PMTapHoa.Core.Data;
using PMTapHoa.Core.Services;

namespace PMTapHoa.Core;

public class CoreServices
{
    public DatabaseContext DatabaseContext { get; }
    public ProductService ProductService { get; }
    public SalesService SalesService { get; }
    public InventoryService InventoryService { get; }
    public DebtService DebtService { get; }
    public CategoryService CategoryService { get; }
    public AuthService AuthService { get; }
    public UserService UserService { get; }
    public AuditService AuditService { get; }
    public AppConfigService AppConfigService { get; }
    public BackupService BackupService { get; }
    public RestockService RestockService { get; }
    public CustomerService CustomerService { get; }
    public DiscountService DiscountService { get; }
    public NotificationService NotificationService { get; }
    public InvoiceFileService InvoiceFileService { get; }
    public PaymentAutomationService PaymentAutomationService { get; }
    public Esp32CustomerDisplayService Esp32CustomerDisplayService { get; }

    public CoreServices(string appDataDirectory, string? sqlScriptPath = null)
    {
        Directory.CreateDirectory(appDataDirectory);

        var dbPath = Path.Combine(appDataDirectory, "taphoa.db");
        DatabaseContext = new DatabaseContext(dbPath);

        if (!string.IsNullOrWhiteSpace(sqlScriptPath) && File.Exists(sqlScriptPath))
        {
            DatabaseContext.InitializeDatabase(sqlScriptPath);
        }

        ProductService = new ProductService(DatabaseContext);
        SalesService = new SalesService(DatabaseContext);
        InventoryService = new InventoryService();
        DebtService = new DebtService(DatabaseContext);
        CategoryService = new CategoryService(DatabaseContext);
        AuthService = new AuthService(DatabaseContext);
        UserService = new UserService(DatabaseContext);
        AuditService = new AuditService(DatabaseContext);
        AppConfigService = new AppConfigService(DatabaseContext);
        BackupService = new BackupService(DatabaseContext);
        RestockService = new RestockService(DatabaseContext);
        CustomerService = new CustomerService(DatabaseContext);
        DiscountService = new DiscountService();
        NotificationService = new NotificationService(InventoryService, ProductService, DebtService);
        InvoiceFileService = new InvoiceFileService(AppConfigService);
        PaymentAutomationService = new PaymentAutomationService(AppConfigService);
        Esp32CustomerDisplayService = new Esp32CustomerDisplayService(AppConfigService);

        AuthService.EnsureDefaultUsers();
    }
}
