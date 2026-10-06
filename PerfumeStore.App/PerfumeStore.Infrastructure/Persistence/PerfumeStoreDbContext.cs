using Microsoft.EntityFrameworkCore;
using PerfumeStore.Domain.Entities;

namespace PerfumeStore.Infrastructure.Persistence;

public partial class PerfumeStoreDbContext : DbContext {
    public PerfumeStoreDbContext() {
    }

    public PerfumeStoreDbContext(DbContextOptions<PerfumeStoreDbContext> options) : base(options) {
    }

    public virtual DbSet<Brand> Brands { get; set; }
    public virtual DbSet<Customer> Customers { get; set; }
    public virtual DbSet<Expense> Expenses { get; set; }
    public virtual DbSet<ExpenseType> ExpenseTypes { get; set; }
    public virtual DbSet<Inventory> Inventory { get; set; }
    public virtual DbSet<InventoryBatch> InventoryBatches { get; set; }
    public virtual DbSet<SalesCostAllocation> SalesCostAllocations { get; set; }
    public virtual DbSet<MoneyAccount> MoneyAccounts { get; set; }
    public virtual DbSet<MoneyTransaction> MoneyTransactions { get; set; }
    public virtual DbSet<PaymentVoucher> PaymentVouchers { get; set; }
    public virtual DbSet<PaymentVoucherPurchaseInvoice> PaymentVoucherPurchaseInvoices { get; set; }
    public virtual DbSet<ProductCategory> ProductCategories { get; set; }
    public virtual DbSet<Product> Products { get; set; }
    public virtual DbSet<PurchaseInvoiceItem> PurchaseInvoiceItems { get; set; }
    public virtual DbSet<PurchaseInvoice> PurchaseInvoices { get; set; }
    public virtual DbSet<ReceiptVoucher> ReceiptVouchers { get; set; }
    public virtual DbSet<ReceiptVoucherSalesInvoice> ReceiptVoucherSalesInvoices { get; set; }
    public virtual DbSet<SalesInvoiceItem> SalesInvoiceItems { get; set; }
    public virtual DbSet<SalesInvoice> SalesInvoices { get; set; }
    public virtual DbSet<Supplier> Suppliers { get; set; }
    public virtual DbSet<Debt> Debts { get; set; }
    public virtual DbSet<Person> Persons { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder) {
        modelBuilder.Entity<InventoryBatch>(entity => {
            entity.Property(e => e.UnitCost).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.Source).HasMaxLength(30);
            entity.HasOne(e => e.Product).WithMany(p => p.InventoryBatches)
                .HasForeignKey(e => e.ProductID).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.PurchaseInvoice).WithMany()
                .HasForeignKey(e => e.PurchaseInvoiceID).OnDelete(DeleteBehavior.Restrict);
            entity.HasIndex(e => new { e.ProductID, e.ReceivedAt });
            entity.HasIndex(e => new { e.PurchaseInvoiceID, e.ProductID }).IsUnique()
                .HasFilter("[PurchaseInvoiceID] IS NOT NULL");
        });
        modelBuilder.Entity<SalesCostAllocation>(entity => {
            entity.Property(e => e.UnitCost).HasColumnType("decimal(18, 2)");
            entity.HasOne(e => e.SalesInvoiceItem).WithMany().HasForeignKey(e => e.SalesInvoiceItemID)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.InventoryBatch).WithMany().HasForeignKey(e => e.InventoryBatchID)
                .OnDelete(DeleteBehavior.Restrict);
        });
        modelBuilder.Entity<Brand>(entity => {
            entity.HasKey(e => e.ID);
            entity.HasIndex(e => e.Name, "IX_Brands").IsUnique();
            entity.Property(e => e.BrandDescription)
                .HasColumnType("nvarchar(max)")
                .IsFixedLength();
            entity.Property(e => e.Name).HasMaxLength(150);
        });

        modelBuilder.Entity<Expense>(entity => {
            entity.HasKey(e => e.ID);
            entity.Property(e => e.Amount).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.Date)
                .HasPrecision(0)
                .HasDefaultValueSql("(sysdatetime())", "DF_Expenses_ExpenseDate");
            entity.Property(e => e.Notes).HasMaxLength(250);

            entity.HasOne(d => d.MoneyAccount)
                .WithMany(p => p.Expenses)
                .HasForeignKey(d => d.MoneyAccountID)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Expenses_MoneyAccounts1");

            entity.HasOne(d => d.ExpenseType)
                .WithMany(p => p.Expenses)
                .HasForeignKey(d => d.ExpenseTypeId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Expenses_ExpenseTypes");
        });

        modelBuilder.Entity<Inventory>(entity => {
            entity.HasKey(e => e.ProductID);
            entity.Property(e => e.ProductID).ValueGeneratedNever();
            entity.HasOne(d => d.Product).WithOne(p => p.Inventory)
                .HasForeignKey<Inventory>(d => d.ProductID)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Inventory_Products1");
        });

        modelBuilder.Entity<MoneyAccount>(entity => {
            entity.HasKey(e => e.ID);
            entity.Property(e => e.AccountName).HasMaxLength(50);
            entity.Property(e => e.CurrentBalance).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.Notes).HasMaxLength(250);
        });

        modelBuilder.Entity<MoneyTransaction>(entity => {
            entity.HasKey(e => e.ID);
            entity.Property(e => e.Notes).HasMaxLength(250);
            entity.Property(e => e.TransferAmount).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.Date)
                .HasPrecision(0)
                .HasDefaultValueSql("(sysdatetime())", "DF_MoneyTransactions_TransferDate");

            entity.HasOne(d => d.FromMoneyAccount).WithMany(p => p.MoneyTransfersFromMoneyAccount)
                .HasForeignKey(d => d.FromMoneyAccountID)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_MoneyTransactions_MoneyAccounts1");

            entity.HasOne(d => d.ToMoneyAccount).WithMany(p => p.MoneyTransfersToMoneyAccount)
                .HasForeignKey(d => d.ToMoneyAccountID)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_MoneyTransactions_MoneyAccounts");
        });

        modelBuilder.Entity<PaymentVoucher>(entity => {
            entity.HasKey(e => e.ID);
            entity.Property(e => e.Amount).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.Notes).HasMaxLength(250);
            entity.Property(e => e.Date)
                .HasPrecision(0)
                .HasDefaultValueSql("(sysdatetime())", "DF_PaymentVouchers_PaymentVoucherDate");

            entity.HasOne(d => d.MoneyAccount).WithMany(p => p.PaymentVouchers)
                .HasForeignKey(d => d.MoneyAccountID)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_PaymentVouchers_MoneyAccounts1");

            entity.Property(x => x.ID)
                .ValueGeneratedOnAdd();

            entity.HasOne(d => d.Supplier).WithMany(p => p.PaymentVouchers)
                .HasForeignKey(d => d.SupplierId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_PaymentVouchers_Suppliers1");
        });

        modelBuilder.Entity<PaymentVoucherPurchaseInvoice>(entity => {
            entity.HasKey(e => e.ID);
            entity.Property(e => e.AppliedAmount).HasColumnType("decimal(18, 2)");

            entity.HasOne(d => d.PaymentVoucher).WithMany(p => p.AppliedPurchaseInvoices)
                .HasForeignKey(d => d.PaymentVoucherId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("FK_PaymentVoucherPurchaseInvoices_PaymentVouchers");

            entity.HasOne(d => d.PurchaseInvoice).WithMany(p => p.PaymentVoucherPurchaseInvoices)
                .HasForeignKey(d => d.PurchaseInvoiceId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_PaymentVoucherPurchaseInvoices_PurchaseInvoices");

            entity.HasIndex(e => new { e.PaymentVoucherId, e.PurchaseInvoiceId }).IsUnique();
        });

        modelBuilder.Entity<ProductCategory>(entity => {
            entity.HasKey(e => e.ID);
            entity.HasIndex(e => e.Name).IsUnique();
            entity.Property(e => e.Name).HasMaxLength(100);
            entity.Property(e => e.Description).HasMaxLength(250);
        });

        modelBuilder.Entity<Product>(entity => {
            entity.HasKey(e => e.ID);
            entity.Property(e => e.CostPrice).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.CreatedAt).HasPrecision(0);
            entity.Property(e => e.Name).HasMaxLength(150);
            entity.Property(e => e.SalePrice).HasColumnType("decimal(18, 2)");

            entity.HasOne(d => d.Brand).WithMany(p => p.Products)
                .HasForeignKey(d => d.BrandID)
                .HasConstraintName("FK_Products_Brands1");

            entity.HasOne(d => d.ProductCategory).WithMany(p => p.Products)
                .HasForeignKey(d => d.ProductCategoryID)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Products_ProductCategories1");
        });

        modelBuilder.Entity<PurchaseInvoiceItem>(entity => {
            entity.HasKey(e => e.ID);
            entity.Property(e => e.UnitCost).HasColumnType("decimal(18, 2)");

            entity.HasOne(d => d.Product).WithMany(p => p.PurchaseInvoiceItems)
                .HasForeignKey(d => d.ProductID)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_PurchaseInvoiceItems_Products1");

            entity.HasOne(d => d.PurchaseInvoice).WithMany(p => p.PurchaseInvoiceItems)
                .HasForeignKey(d => d.PurchaseInvoiceID)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_PurchaseInvoiceItems_PurchaseInvoices1");
        });

        modelBuilder.Entity<PurchaseInvoice>(entity => {
            entity.HasKey(e => e.ID);

            entity.HasOne(x => x.MoneyAccount)
                .WithMany()
                .HasForeignKey(x => x.MoneyAccountId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.Property(e => e.Date)
                .HasPrecision(0)
                .HasDefaultValueSql("(sysdatetime())", "DF_PurchaseInvoices_InvoiceDate");
            entity.Property(e => e.Notes).HasMaxLength(250);

            entity.HasOne(d => d.Supplier).WithMany(p => p.PurchaseInvoices)
                .HasForeignKey(d => d.SupplierId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_PurchaseInvoices_Suppliers1");
        });

        modelBuilder.Entity<ReceiptVoucher>(entity => {
            entity.HasKey(e => e.ID);
            entity.Property(e => e.Amount).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.Notes).HasMaxLength(250);
            entity.Property(e => e.Date)
                .HasPrecision(0)
                .HasDefaultValueSql("(sysdatetime())", "DF_ReceiptVouchers_ReceiptVoucherDate");

            entity.HasOne(d => d.Customer).WithMany(p => p.ReceiptVouchers)
                .HasForeignKey(d => d.CustomerId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ReceiptVouchers_Customers1");

            entity.HasOne(d => d.Person).WithMany()
                .HasForeignKey(d => d.PersonId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ReceiptVouchers_Persons1");

            entity.HasOne(d => d.MoneyAccount).WithMany(p => p.ReceiptVouchers)
                .HasForeignKey(d => d.MoneyAccountID)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ReceiptVouchers_MoneyAccounts1");
        });

        modelBuilder.Entity<ReceiptVoucherSalesInvoice>(entity => {
            entity.HasKey(e => e.ID);
            entity.Property(e => e.AppliedAmount).HasColumnType("decimal(18, 2)");

            entity.HasOne(d => d.ReceiptVoucher).WithMany(p => p.AppliedSalesInvoices)
                .HasForeignKey(d => d.ReceiptVoucherId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("FK_ReceiptVoucherSalesInvoices_ReceiptVouchers");

            entity.HasOne(d => d.SalesInvoice).WithMany(p => p.ReceiptVoucherSalesInvoices)
                .HasForeignKey(d => d.SalesInvoiceId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_ReceiptVoucherSalesInvoices_SalesInvoices");

            entity.HasIndex(e => new { e.ReceiptVoucherId, e.SalesInvoiceId }).IsUnique();
        });

        modelBuilder.Entity<SalesInvoiceItem>(entity => {
            entity.HasKey(e => e.ID);
            entity.Property(e => e.UnitPrice).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.CostTotal).HasColumnType("decimal(18, 2)");

            entity.HasOne(d => d.Product).WithMany(p => p.SalesInvoiceItems)
                .HasForeignKey(d => d.ProductID)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_SalesInvoiceItems_Products1");

            entity.HasOne(d => d.SalesInvoice).WithMany(p => p.SalesInvoiceItems)
                .HasForeignKey(d => d.SalesInvoiceID)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_SalesInvoiceItems_SalesInvoices1");
        });

        modelBuilder.Entity<SalesInvoice>(entity => {
            entity.HasKey(e => e.ID);

            entity.HasOne(x => x.MoneyAccount)
                .WithMany()
                .HasForeignKey(x => x.MoneyAccountId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.Property(e => e.Date)
                .HasPrecision(0)
                .HasDefaultValueSql("(sysdatetime())", "DF_SalesInvoices_InvoiceDate");
            entity.Property(e => e.Notes).HasMaxLength(250);

            entity.HasOne(d => d.Customer).WithMany(p => p.SalesInvoices)
                .HasForeignKey(d => d.CustomerId)
                .HasConstraintName("FK_SalesInvoices_Customers1");
        });

        modelBuilder.Entity<Person>(entity => {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.Property(e => e.Phone).HasMaxLength(30).IsRequired();
            entity.HasIndex(e => e.Phone).IsUnique();
            entity.Property(e => e.Name).HasMaxLength(150).IsRequired();
            entity.Property(e => e.CreatedAt)
                .HasPrecision(0)
                .HasDefaultValueSql("sysdatetime()");
        });

        modelBuilder.Entity<ExpenseType>(entity => {
            entity.ToTable("ExpenseType");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).HasMaxLength(150);
            entity.HasIndex(e => e.Name).IsUnique();
        });

        modelBuilder.Entity<Debt>(entity => {
            entity.HasKey(e => e.Id);
            entity.HasOne(d => d.MoneyAccount)
                .WithMany()
                .HasForeignKey(d => d.MoneyAccountId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Person>()
            .HasMany(p => p.Debts)
            .WithOne(d => d.Person)
            .HasForeignKey(d => d.PersonId);

        modelBuilder.Entity<Person>().UseTpcMappingStrategy();

        modelBuilder.Entity<Customer>().ToTable("Customers");
        modelBuilder.Entity<Supplier>().ToTable("Suppliers");

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
