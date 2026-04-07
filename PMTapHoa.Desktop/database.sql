PRAGMA foreign_keys = ON;

CREATE TABLE IF NOT EXISTS Categories (
    CategoryID INTEGER PRIMARY KEY AUTOINCREMENT,
    CategoryName TEXT NOT NULL
);

CREATE TABLE IF NOT EXISTS Products (
    ProductID INTEGER PRIMARY KEY AUTOINCREMENT,
    Barcode TEXT UNIQUE,
    ProductName TEXT NOT NULL,
    CategoryID INTEGER,
    Unit TEXT,
    CostPrice DECIMAL(18, 2),
    SellingPrice DECIMAL(18, 2),
    StockQuantity REAL DEFAULT 0,
    MinStock INTEGER DEFAULT 5,
    ExpiryDate DATE,
    FOREIGN KEY (CategoryID) REFERENCES Categories(CategoryID)
);

CREATE TABLE IF NOT EXISTS Sales (
    SaleID INTEGER PRIMARY KEY AUTOINCREMENT,
    SaleDate DATETIME DEFAULT CURRENT_TIMESTAMP,
    TotalAmount DECIMAL(18, 2),
    CustomerName TEXT,
    IsDebt BOOLEAN DEFAULT 0
);

CREATE TABLE IF NOT EXISTS SaleDetails (
    DetailID INTEGER PRIMARY KEY AUTOINCREMENT,
    SaleID INTEGER,
    ProductID INTEGER,
    Quantity INTEGER,
    UnitPrice DECIMAL(18, 2),
    FOREIGN KEY (SaleID) REFERENCES Sales(SaleID),
    FOREIGN KEY (ProductID) REFERENCES Products(ProductID)
);

CREATE TABLE IF NOT EXISTS DebtPayments (
    PaymentID INTEGER PRIMARY KEY AUTOINCREMENT,
    SaleID INTEGER NOT NULL,
    PaymentDate DATETIME DEFAULT CURRENT_TIMESTAMP,
    Amount DECIMAL(18, 2) NOT NULL,
    Note TEXT,
    FOREIGN KEY (SaleID) REFERENCES Sales(SaleID)
);

CREATE TABLE IF NOT EXISTS Users (
    UserID INTEGER PRIMARY KEY AUTOINCREMENT,
    Username TEXT NOT NULL UNIQUE,
    PasswordHash TEXT NOT NULL,
    FullName TEXT NOT NULL,
    Role TEXT NOT NULL, -- Manager | Staff
    IsActive BOOLEAN DEFAULT 1
);

CREATE TABLE IF NOT EXISTS AuditLogs (
    LogID INTEGER PRIMARY KEY AUTOINCREMENT,
    LogTime DATETIME DEFAULT CURRENT_TIMESTAMP,
    Username TEXT,
    Action TEXT NOT NULL,
    Detail TEXT
);

CREATE TABLE IF NOT EXISTS AppSettings (
    SettingKey TEXT PRIMARY KEY,
    SettingValue TEXT
);

CREATE TABLE IF NOT EXISTS Suppliers (
    SupplierID INTEGER PRIMARY KEY AUTOINCREMENT,
    SupplierName TEXT NOT NULL,
    ContactName TEXT,
    Phone TEXT,
    Address TEXT,
    Note TEXT
);

CREATE TABLE IF NOT EXISTS RestockRequests (
    RequestID INTEGER PRIMARY KEY AUTOINCREMENT,
    ProductID INTEGER NOT NULL,
    SupplierID INTEGER,
    RequestedQty REAL NOT NULL,
    ExpectedCostPrice DECIMAL(18, 2),
    Status TEXT NOT NULL DEFAULT 'Open', -- Open | Ordered | Received | Cancelled
    RequestDate DATETIME DEFAULT CURRENT_TIMESTAMP,
    Note TEXT,
    FOREIGN KEY (ProductID) REFERENCES Products(ProductID),
    FOREIGN KEY (SupplierID) REFERENCES Suppliers(SupplierID)
);

CREATE INDEX IF NOT EXISTS IDX_Products_Barcode ON Products(Barcode);
CREATE INDEX IF NOT EXISTS IDX_Sales_Date ON Sales(SaleDate);
CREATE INDEX IF NOT EXISTS IDX_Sales_Customer ON Sales(CustomerName);
CREATE INDEX IF NOT EXISTS IDX_DebtPayments_SaleID ON DebtPayments(SaleID);
CREATE INDEX IF NOT EXISTS IDX_AuditLogs_LogTime ON AuditLogs(LogTime);
CREATE INDEX IF NOT EXISTS IDX_RestockRequests_ProductID ON RestockRequests(ProductID);
CREATE INDEX IF NOT EXISTS IDX_RestockRequests_Status ON RestockRequests(Status);

INSERT INTO Categories (CategoryName)
SELECT 'Chưa phân loại'
WHERE NOT EXISTS (SELECT 1 FROM Categories WHERE CategoryName = 'Chưa phân loại');
