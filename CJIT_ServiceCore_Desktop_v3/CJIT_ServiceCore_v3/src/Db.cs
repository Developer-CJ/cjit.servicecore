using System;
using System.Data;
using System.Globalization;
using System.IO;
using Microsoft.Data.Sqlite;

namespace CJIT.ServiceCore;

internal static class Db
{
    private static readonly string DataDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "CJIT", "ServiceCore");
    private static readonly string DbPath = Path.Combine(DataDir, "servicecore_v3.db");
    private static string ConnectionString => new SqliteConnectionStringBuilder { DataSource = DbPath, ForeignKeys = true }.ToString();

    public static SqliteParameter P(string name, object? value) => new(name, value ?? DBNull.Value);

    public static void EnsureDatabase()
    {
        Directory.CreateDirectory(DataDir);
        Execute("PRAGMA foreign_keys = ON;");
        Execute(SchemaSql);
        Seed();
    }

    public static DataTable Query(string sql, params SqliteParameter[] parameters)
    {
        using var conn = new SqliteConnection(ConnectionString);
        conn.Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = sql;
        cmd.Parameters.AddRange(parameters);
        using var reader = cmd.ExecuteReader();
        var table = new DataTable();
        table.Load(reader);
        return table;
    }

    public static object? Scalar(string sql, params SqliteParameter[] parameters)
    {
        using var conn = new SqliteConnection(ConnectionString);
        conn.Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = sql;
        cmd.Parameters.AddRange(parameters);
        return cmd.ExecuteScalar();
    }

    public static int Execute(string sql, params SqliteParameter[] parameters)
    {
        using var conn = new SqliteConnection(ConnectionString);
        conn.Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = sql;
        cmd.Parameters.AddRange(parameters);
        return cmd.ExecuteNonQuery();
    }

    public static long Insert(string sql, params SqliteParameter[] parameters)
    {
        using var conn = new SqliteConnection(ConnectionString);
        conn.Open();
        using var tx = conn.BeginTransaction();
        using var cmd = conn.CreateCommand();
        cmd.Transaction = tx;
        cmd.CommandText = sql;
        cmd.Parameters.AddRange(parameters);
        cmd.ExecuteNonQuery();
        cmd.Parameters.Clear();
        cmd.CommandText = "SELECT last_insert_rowid();";
        var id = Convert.ToInt64(cmd.ExecuteScalar(), CultureInfo.InvariantCulture);
        tx.Commit();
        return id;
    }

    public static string Setting(string key, string fallback = "")
    {
        var value = Scalar("SELECT Value FROM Settings WHERE Key=@key", P("@key", key));
        return value == null || value == DBNull.Value ? fallback : Convert.ToString(value, CultureInfo.InvariantCulture) ?? fallback;
    }

    public static void SetSetting(string key, string value)
    {
        Execute("""
            INSERT INTO Settings(Key, Value, UpdatedAt) VALUES(@key, @value, @updated)
            ON CONFLICT(Key) DO UPDATE SET Value=excluded.Value, UpdatedAt=excluded.UpdatedAt
            """, P("@key", key), P("@value", value), P("@updated", Now()));
    }

    public static void Audit(string actor, string action, string details)
    {
        Execute("INSERT INTO AuditLogs(Actor, Action, Details, CreatedAt) VALUES(@actor,@action,@details,@created)",
            P("@actor", actor), P("@action", action), P("@details", details), P("@created", Now()));
    }

    public static string Now() => DateTime.Now.ToString("s", CultureInfo.InvariantCulture);

    public static string NextNumber(string prefix)
    {
        return prefix + "-" + DateTime.Now.ToString("yyyyMMdd-HHmmss", CultureInfo.InvariantCulture);
    }

    private static void Seed()
    {
        if (Convert.ToInt32(Scalar("SELECT COUNT(*) FROM Settings") ?? 0, CultureInfo.InvariantCulture) == 0)
        {
            SetSetting("StoreName", "CJIT ServiceCore Demo Store");
            SetSetting("TerminalName", "COUNTER-01");
            SetSetting("TaxRate", "0.06");
            SetSetting("DiagnosticFee", "35.00");
        }

        if (Convert.ToInt32(Scalar("SELECT COUNT(*) FROM Users") ?? 0, CultureInfo.InvariantCulture) == 0)
        {
            Execute("INSERT INTO Users(Username, Password, DisplayName, Role, IsActive, CreatedAt) VALUES('chris','1228','CJ','Administrator',1,@created)", P("@created", Now()));
        }

        if (Convert.ToInt32(Scalar("SELECT COUNT(*) FROM InventoryItems") ?? 0, CultureInfo.InvariantCulture) == 0)
        {
            Execute("""
                INSERT INTO InventoryItems(Sku, Name, Category, Cost, Price, Quantity, ReorderLevel, BinLocation, Notes, IsActive, CreatedAt, UpdatedAt) VALUES
                ('SVC-DIAG','Diagnostic Fee','Service',0,35.00,999,0,'SERVICE','Standard diagnostic intake fee',1,@now,@now),
                ('SVC-WININSTALL','Windows Reinstall / Setup','Service',0,89.99,999,0,'SERVICE','OS reinstall and basic setup',1,@now,@now),
                ('SVC-DATAXFER','Data Transfer','Service',0,59.99,999,0,'SERVICE','Customer data transfer service',1,@now,@now),
                ('SVC-VIRUS','Virus / Malware Removal','Service',0,79.99,999,0,'SERVICE','Malware cleanup service',1,@now,@now),
                ('PART-IP12SCREEN','iPhone 12 Screen Assembly','Phone Part',31.50,119.99,5,2,'A-03','Compatible with iPhone 12',1,@now,@now),
                ('ACC-USBC20','USB-C 20W Charger','Accessory',7.50,19.99,18,5,'B-01','Retail charger',1,@now,@now),
                ('ACC-LIGHTNING','Lightning Cable 6ft','Accessory',3.20,12.99,26,8,'B-02','Retail cable',1,@now,@now),
                ('NET-ROUTER-BASIC','Basic Wi-Fi Router','Network Gear',29.00,69.99,4,2,'C-01','Home router',1,@now,@now),
                ('USED-LAPTOP-DEMO','Refurbished Laptop Demo','Used Device',125.00,249.99,2,1,'SHOWCASE','Example refurbished laptop',1,@now,@now)
                """, P("@now", Now()));
        }

        if (Convert.ToInt32(Scalar("SELECT COUNT(*) FROM Customers") ?? 0, CultureInfo.InvariantCulture) == 0)
        {
            var c1 = Insert("""
                INSERT INTO Customers(FirstName, LastName, Phone, Email, Address, BusinessName, Notes, CreatedAt, UpdatedAt)
                VALUES('Amanda','Reese','810-555-0188','amanda@example.com','', '', 'Demo returning customer.', @now, @now)
                """, P("@now", Now()));
            var d1 = Insert("""
                INSERT INTO Devices(CustomerId, DeviceType, Brand, Model, SerialNumber, Imei, PasscodeNotes, ConditionNotes, Accessories, CreatedAt)
                VALUES(@customer,'Laptop','HP','Pavilion 15','HP-DEMO-1001','','Customer provided PIN verbally.','Powers on slowly. Minor case scratches.','Charger included',@now)
                """, P("@customer", c1), P("@now", Now()));
            Insert("""
                INSERT INTO Tickets(TicketNumber, CustomerId, DeviceId, ServiceCategory, Priority, Status, Issue, InternalNote, AssignedTo,
                    DiagnosticFee, LaborEstimate, PartsEstimate, EstimateTotal, DepositPaid, BalanceDue, CreatedAt, UpdatedAt)
                VALUES(@num,@customer,@device,'Computer Repair','Normal','Waiting Diagnosis','Laptop running very slow and browser popups.','Demo ticket seeded by ServiceCore.', 'CJ',
                    35, 80, 0, 115, 35, 80, @now, @now)
                """, P("@num", NextNumber("SC")), P("@customer", c1), P("@device", d1), P("@now", Now()));
        }
    }

    private const string SchemaSql = """
        CREATE TABLE IF NOT EXISTS Settings(
            Key TEXT PRIMARY KEY,
            Value TEXT NOT NULL,
            UpdatedAt TEXT NOT NULL
        );

        CREATE TABLE IF NOT EXISTS Users(
            Id INTEGER PRIMARY KEY AUTOINCREMENT,
            Username TEXT NOT NULL UNIQUE,
            Password TEXT NOT NULL,
            DisplayName TEXT NOT NULL,
            Role TEXT NOT NULL,
            IsActive INTEGER NOT NULL DEFAULT 1,
            CreatedAt TEXT NOT NULL
        );

        CREATE TABLE IF NOT EXISTS Customers(
            Id INTEGER PRIMARY KEY AUTOINCREMENT,
            FirstName TEXT NOT NULL,
            LastName TEXT NOT NULL,
            Phone TEXT,
            Email TEXT,
            Address TEXT,
            BusinessName TEXT,
            Notes TEXT,
            CreatedAt TEXT NOT NULL,
            UpdatedAt TEXT NOT NULL
        );

        CREATE TABLE IF NOT EXISTS Devices(
            Id INTEGER PRIMARY KEY AUTOINCREMENT,
            CustomerId INTEGER NOT NULL,
            DeviceType TEXT NOT NULL,
            Brand TEXT,
            Model TEXT,
            SerialNumber TEXT,
            Imei TEXT,
            PasscodeNotes TEXT,
            ConditionNotes TEXT,
            Accessories TEXT,
            CreatedAt TEXT NOT NULL,
            FOREIGN KEY(CustomerId) REFERENCES Customers(Id)
        );

        CREATE TABLE IF NOT EXISTS Tickets(
            Id INTEGER PRIMARY KEY AUTOINCREMENT,
            TicketNumber TEXT NOT NULL UNIQUE,
            CustomerId INTEGER NOT NULL,
            DeviceId INTEGER NOT NULL,
            ServiceCategory TEXT NOT NULL,
            Priority TEXT NOT NULL,
            Status TEXT NOT NULL,
            Issue TEXT NOT NULL,
            InternalNote TEXT,
            AssignedTo TEXT,
            DiagnosticFee REAL NOT NULL DEFAULT 0,
            LaborEstimate REAL NOT NULL DEFAULT 0,
            PartsEstimate REAL NOT NULL DEFAULT 0,
            EstimateTotal REAL NOT NULL DEFAULT 0,
            DepositPaid REAL NOT NULL DEFAULT 0,
            BalanceDue REAL NOT NULL DEFAULT 0,
            CreatedAt TEXT NOT NULL,
            UpdatedAt TEXT NOT NULL,
            FOREIGN KEY(CustomerId) REFERENCES Customers(Id),
            FOREIGN KEY(DeviceId) REFERENCES Devices(Id)
        );

        CREATE TABLE IF NOT EXISTS TicketNotes(
            Id INTEGER PRIMARY KEY AUTOINCREMENT,
            TicketId INTEGER NOT NULL,
            Author TEXT NOT NULL,
            NoteType TEXT NOT NULL,
            NoteText TEXT NOT NULL,
            CreatedAt TEXT NOT NULL,
            FOREIGN KEY(TicketId) REFERENCES Tickets(Id)
        );

        CREATE TABLE IF NOT EXISTS InventoryItems(
            Id INTEGER PRIMARY KEY AUTOINCREMENT,
            Sku TEXT NOT NULL UNIQUE,
            Name TEXT NOT NULL,
            Category TEXT NOT NULL,
            Cost REAL NOT NULL DEFAULT 0,
            Price REAL NOT NULL DEFAULT 0,
            Quantity INTEGER NOT NULL DEFAULT 0,
            ReorderLevel INTEGER NOT NULL DEFAULT 0,
            BinLocation TEXT,
            Notes TEXT,
            IsActive INTEGER NOT NULL DEFAULT 1,
            CreatedAt TEXT NOT NULL,
            UpdatedAt TEXT NOT NULL
        );

        CREATE TABLE IF NOT EXISTS Sales(
            Id INTEGER PRIMARY KEY AUTOINCREMENT,
            SaleNumber TEXT NOT NULL UNIQUE,
            CustomerId INTEGER,
            TicketId INTEGER,
            Subtotal REAL NOT NULL,
            Tax REAL NOT NULL,
            Total REAL NOT NULL,
            PaymentMethod TEXT NOT NULL,
            AmountPaid REAL NOT NULL,
            CreatedAt TEXT NOT NULL,
            FOREIGN KEY(CustomerId) REFERENCES Customers(Id),
            FOREIGN KEY(TicketId) REFERENCES Tickets(Id)
        );

        CREATE TABLE IF NOT EXISTS SaleItems(
            Id INTEGER PRIMARY KEY AUTOINCREMENT,
            SaleId INTEGER NOT NULL,
            InventoryItemId INTEGER,
            Description TEXT NOT NULL,
            Quantity INTEGER NOT NULL,
            UnitPrice REAL NOT NULL,
            LineTotal REAL NOT NULL,
            FOREIGN KEY(SaleId) REFERENCES Sales(Id),
            FOREIGN KEY(InventoryItemId) REFERENCES InventoryItems(Id)
        );

        CREATE TABLE IF NOT EXISTS Payments(
            Id INTEGER PRIMARY KEY AUTOINCREMENT,
            CustomerId INTEGER,
            TicketId INTEGER,
            SaleId INTEGER,
            PaymentType TEXT NOT NULL,
            Method TEXT NOT NULL,
            Amount REAL NOT NULL,
            CreatedAt TEXT NOT NULL,
            Note TEXT,
            FOREIGN KEY(CustomerId) REFERENCES Customers(Id),
            FOREIGN KEY(TicketId) REFERENCES Tickets(Id),
            FOREIGN KEY(SaleId) REFERENCES Sales(Id)
        );

        CREATE TABLE IF NOT EXISTS DailyCloseReports(
            Id INTEGER PRIMARY KEY AUTOINCREMENT,
            BusinessDate TEXT NOT NULL,
            CashExpected REAL NOT NULL,
            CashCounted REAL NOT NULL,
            OverShort REAL NOT NULL,
            CardTotal REAL NOT NULL,
            DepositTotal REAL NOT NULL,
            SalesTotal REAL NOT NULL,
            Notes TEXT,
            ClosedBy TEXT NOT NULL,
            CreatedAt TEXT NOT NULL
        );

        CREATE TABLE IF NOT EXISTS AuditLogs(
            Id INTEGER PRIMARY KEY AUTOINCREMENT,
            Actor TEXT NOT NULL,
            Action TEXT NOT NULL,
            Details TEXT NOT NULL,
            CreatedAt TEXT NOT NULL
        );
        """;
}
