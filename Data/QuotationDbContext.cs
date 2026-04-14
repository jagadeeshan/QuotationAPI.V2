using Microsoft.EntityFrameworkCore;
using QuotationAPI.V2.Models.Accounts;
using QuotationAPI.V2.Models.Auth;
using QuotationAPI.V2.Models.Quotations;
using QuotationAPI.V2.Models.Calculations;
using QuotationAPI.V2.Models.Employee;
using QuotationAPI.V2.Models.Expense;
using QuotationAPI.V2.Models.Inventory;
using QuotationAPI.V2.Models.Customer;
using QuotationAPI.V2.Models.LOV;
using QuotationAPI.V2.Models.Admin;
using QuotationAPI.V2.Models.Sales;
using QuotationAPI.V2.Models.Integrations;
using Microsoft.EntityFrameworkCore.Metadata;
using System.Linq.Expressions;
using System.Reflection;

namespace QuotationAPI.V2.Data;

public class QuotationDbContext : DbContext
{
    private static readonly MethodInfo SetSoftDeleteFilterMethod =
        typeof(QuotationDbContext).GetMethod(nameof(SetSoftDeleteFilter), BindingFlags.NonPublic | BindingFlags.Static)
        ?? throw new InvalidOperationException("Unable to locate SetSoftDeleteFilter method.");

    public QuotationDbContext(DbContextOptions<QuotationDbContext> options) : base(options)
    {
    }

    public DbSet<AppUser> Users => Set<AppUser>();
    public DbSet<AppRole> Roles => Set<AppRole>();
    public DbSet<AppUserRole> UserRoles => Set<AppUserRole>();

    public DbSet<Quotation> Quotations => Set<Quotation>();
    public DbSet<QuotationLineItem> QuotationLineItems => Set<QuotationLineItem>();

    public DbSet<BankCashBalance> BankCashBalances => Set<BankCashBalance>();
    public DbSet<CustomerOutstanding> CustomerOutstandings => Set<CustomerOutstanding>();
    public DbSet<IncomeEntry> IncomeEntries => Set<IncomeEntry>();
    public DbSet<ExpenseEntry> ExpenseEntries => Set<ExpenseEntry>();
    public DbSet<AccountTransaction> AccountTransactions => Set<AccountTransaction>();
    public DbSet<CashTransfer> CashTransfers => Set<CashTransfer>();
    public DbSet<ExpenseLedgerRow> ExpenseLedgerRows => Set<ExpenseLedgerRow>();
    public DbSet<IncomeRow> IncomeRows => Set<IncomeRow>();
    public DbSet<PurchaseSalesRow> PurchaseSalesRows => Set<PurchaseSalesRow>();
    public DbSet<TaxPaymentRow> TaxPaymentRows => Set<TaxPaymentRow>();

        // Calculations (blob storage for box quotation & invoice forms)
        public DbSet<QuotationCalcRecord> QuotationCalcRecords => Set<QuotationCalcRecord>();
        public DbSet<InvoiceCalcRecord> InvoiceCalcRecords => Set<InvoiceCalcRecord>();

        // Employee module
        public DbSet<EmpEmployee> Employees => Set<EmpEmployee>();
        public DbSet<EmpAttendanceRecord> AttendanceRecords => Set<EmpAttendanceRecord>();
        public DbSet<EmpHoliday> Holidays => Set<EmpHoliday>();
        public DbSet<EmpSalaryMaster> SalaryMasters => Set<EmpSalaryMaster>();
        public DbSet<EmpSalaryAdvance> SalaryAdvances => Set<EmpSalaryAdvance>();
        public DbSet<EmpMonthlySalaryCalc> MonthlySalaryCalcs => Set<EmpMonthlySalaryCalc>();

        // Expense records (workflow: draft/submitted/approved)
        public DbSet<ExpenseRecord> ExpenseRecords => Set<ExpenseRecord>();

        // Inventory
        public DbSet<ReelStock> ReelStocks => Set<ReelStock>();
        public DbSet<MaterialPrice> MaterialPrices => Set<MaterialPrice>();

        // Customer master
        public DbSet<CustomerMaster> CustomerMasters => Set<CustomerMaster>();

        // List of values
        public DbSet<LovItem> LovItems => Set<LovItem>();

        // Admin module
        public DbSet<AdminUser> AdminUsers => Set<AdminUser>();
        public DbSet<AdminUserGroup> AdminUserGroups => Set<AdminUserGroup>();
        public DbSet<AdminFeature> AdminFeatures => Set<AdminFeature>();
        public DbSet<AdminPermission> AdminPermissions => Set<AdminPermission>();
        public DbSet<AdminSystemSetting> AdminSystemSettings => Set<AdminSystemSetting>();
        public DbSet<AdminAuditLog> AdminAuditLogs => Set<AdminAuditLog>();
        public DbSet<AdminCompanyProfile> AdminCompanyProfiles => Set<AdminCompanyProfile>();

        // Configuration history and snapshots
        public DbSet<ConfigurationHistory> ConfigurationHistory => Set<ConfigurationHistory>();
        public DbSet<QuotationConfigSnapshot> QuotationConfigSnapshot => Set<QuotationConfigSnapshot>();
        public DbSet<InvoiceConfigSnapshot> InvoiceConfigSnapshot => Set<InvoiceConfigSnapshot>();

        // Sales module
        public DbSet<WasteSale> WasteSales => Set<WasteSale>();
        public DbSet<RollSale> RollSales => Set<RollSale>();

        // Zoho Books integration
        public DbSet<ZohoSyncState> ZohoSyncStates => Set<ZohoSyncState>();
        public DbSet<ZohoCustomerRecord> ZohoCustomerRecords => Set<ZohoCustomerRecord>();
        public DbSet<ZohoInvoiceRecord> ZohoInvoiceRecords => Set<ZohoInvoiceRecord>();
        public DbSet<ZohoOutstandingRecord> ZohoOutstandingRecords => Set<ZohoOutstandingRecord>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        foreach (var property in modelBuilder.Model
                     .GetEntityTypes()
                     .SelectMany(entity => entity.GetProperties())
                     .Where(property => property.ClrType == typeof(decimal) || property.ClrType == typeof(decimal?)))
        {
            property.SetPrecision(18);
            property.SetScale(4);
        }

        modelBuilder.Entity<AppUserRole>()
            .HasKey(x => new { x.UserId, x.RoleId });

        modelBuilder.Entity<AppUserRole>()
            .HasOne(x => x.User)
            .WithMany(x => x.Roles)
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<AppUserRole>()
            .HasOne(x => x.Role)
            .WithMany(x => x.Users)
            .HasForeignKey(x => x.RoleId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Quotation>()
            .HasMany(x => x.LineItems)
            .WithOne(x => x.Quotation)
            .HasForeignKey(x => x.QuotationId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<AppUser>()
            .HasIndex(x => x.Username)
            .IsUnique();

        modelBuilder.Entity<Quotation>()
            .HasIndex(x => x.QuoteNumber)
            .IsUnique();

        modelBuilder.Entity<CustomerMaster>()
            .Property(x => x.Code)
            .HasMaxLength(32);

        modelBuilder.Entity<CustomerMaster>()
            .HasIndex(x => x.Code)
            .IsUnique();

        modelBuilder.Entity<EmpEmployee>()
            .Property(x => x.EmployeeCode)
            .HasMaxLength(32);

        modelBuilder.Entity<EmpEmployee>()
            .HasIndex(x => x.EmployeeCode)
            .IsUnique();

        modelBuilder.Entity<ReelStock>()
            .Property(x => x.ReelNumber)
            .HasMaxLength(64);

        modelBuilder.Entity<ReelStock>()
            .HasIndex(x => x.ReelNumber)
            .IsUnique();

        modelBuilder.Entity<EmpSalaryMaster>()
            .Property(x => x.SalaryType)
            .HasMaxLength(16)
            .HasDefaultValue("monthly");

        modelBuilder.Entity<EmpMonthlySalaryCalc>()
            .Property(x => x.SalaryType)
            .HasMaxLength(16)
            .HasDefaultValue("monthly");

        modelBuilder.Entity<AdminCompanyProfile>()
            .Property(x => x.CompanyName)
            .HasMaxLength(200);

        modelBuilder.Entity<AdminCompanyProfile>()
            .Property(x => x.GstNo)
            .HasMaxLength(30);

        ApplySoftDeleteQueryFilters(modelBuilder);
    }

    public override int SaveChanges()
    {
        ConvertHardDeleteToSoftDelete();
        return base.SaveChanges();
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        ConvertHardDeleteToSoftDelete();
        return base.SaveChangesAsync(cancellationToken);
    }

    private void ConvertHardDeleteToSoftDelete()
    {
        foreach (var entry in ChangeTracker.Entries().Where(e => e.State == EntityState.Deleted))
        {
            var isDeletedProperty = entry.Metadata.FindProperty("IsDeleted");
            if (isDeletedProperty == null || isDeletedProperty.ClrType != typeof(bool))
            {
                continue;
            }

            entry.State = EntityState.Modified;
            entry.CurrentValues["IsDeleted"] = true;

            // Preserve child rows and let explicit business logic decide cascading soft-delete.
            foreach (var reference in entry.References.Where(r => r.TargetEntry != null && r.TargetEntry.State == EntityState.Deleted))
            {
                reference.TargetEntry!.State = EntityState.Unchanged;
            }
        }
    }

    private static void ApplySoftDeleteQueryFilters(ModelBuilder modelBuilder)
    {
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            var isDeletedProperty = entityType.FindProperty("IsDeleted");
            if (isDeletedProperty == null || isDeletedProperty.ClrType != typeof(bool))
            {
                continue;
            }

            var method = SetSoftDeleteFilterMethod.MakeGenericMethod(entityType.ClrType);
            method.Invoke(null, [modelBuilder]);
        }
    }

    private static void SetSoftDeleteFilter<TEntity>(ModelBuilder modelBuilder) where TEntity : class
    {
        Expression<Func<TEntity, bool>> filter = entity => !EF.Property<bool>(entity, "IsDeleted");
        modelBuilder.Entity<TEntity>().HasQueryFilter(filter);
    }
}
