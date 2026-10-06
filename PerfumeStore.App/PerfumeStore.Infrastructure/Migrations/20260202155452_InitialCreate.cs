using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PerfumeStore.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Release the purchase FK before replacing the referenced supplier
            // primary key. DebtsTable restores it against the renamed Persons.
            migrationBuilder.DropForeignKey(
                name: "FK_PurchaseInvoices_Suppliers1",
                table: "PurchaseInvoices");

            migrationBuilder.DropForeignKey(
                name: "FK_PaymentVouchers_Suppliers1",
                table: "PaymentVouchers");

            migrationBuilder.DropForeignKey(
                name: "FK_ReceiptVouchers_Customers1",
                table: "ReceiptVouchers");

            migrationBuilder.DropForeignKey(
                name: "FK_SalesInvoices_Customers1",
                table: "SalesInvoices");

            migrationBuilder.DropTable(
                name: "Customers");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Suppliers",
                table: "Suppliers");

            migrationBuilder.DropColumn(
                name: "UnitPrice",
                table: "SalesInvoiceItems");

            migrationBuilder.DropColumn(
                name: "UnitCost",
                table: "PurchaseInvoiceItems");

            migrationBuilder.RenameTable(
                name: "Suppliers",
                newName: "Persons");

            migrationBuilder.RenameColumn(
                name: "SupplierName",
                table: "Persons",
                newName: "Name");

            migrationBuilder.RenameColumn(
                name: "SupplierPhone",
                table: "Persons",
                newName: "Phone");

            migrationBuilder.AddColumn<int>(
                name: "AmountPaid",
                table: "SalesInvoices",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "AmountPaid",
                table: "PurchaseInvoices",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedAt",
                table: "Persons",
                type: "datetime2(0)",
                precision: 0,
                nullable: false,
                defaultValueSql: "sysdatetime()",
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldDefaultValueSql: "(sysdatetime())")
                .OldAnnotation("Relational:DefaultConstraintName", "DF_Suppliers_CreatedAt");

            migrationBuilder.AddColumn<string>(
                name: "CreatedBy",
                table: "Persons",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Discriminator",
                table: "Persons",
                type: "nvarchar(8)",
                maxLength: 8,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "Persons",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "UpdatedBy",
                table: "Persons",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Persons",
                table: "Persons",
                column: "Phone");

            migrationBuilder.CreateTable(
                name: "Debt",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Amount = table.Column<int>(type: "int", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SalesInvoiceId = table.Column<long>(type: "bigint", nullable: true),
                    PurchaseInvoiceId = table.Column<long>(type: "bigint", nullable: true),
                    PersonPhone = table.Column<string>(type: "nvarchar(30)", nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    UpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Debt", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Debt_Persons_PersonPhone",
                        column: x => x.PersonPhone,
                        principalTable: "Persons",
                        principalColumn: "Phone");
                    table.ForeignKey(
                        name: "FK_Debt_PurchaseInvoices_PurchaseInvoiceId",
                        column: x => x.PurchaseInvoiceId,
                        principalTable: "PurchaseInvoices",
                        principalColumn: "ID");
                    table.ForeignKey(
                        name: "FK_Debt_SalesInvoices_SalesInvoiceId",
                        column: x => x.SalesInvoiceId,
                        principalTable: "SalesInvoices",
                        principalColumn: "ID");
                });

            migrationBuilder.CreateIndex(
                name: "IX_Debt_PersonPhone",
                table: "Debt",
                column: "PersonPhone",
                unique: true,
                filter: "[PersonPhone] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Debt_PurchaseInvoiceId",
                table: "Debt",
                column: "PurchaseInvoiceId",
                unique: true,
                filter: "[PurchaseInvoiceId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Debt_SalesInvoiceId",
                table: "Debt",
                column: "SalesInvoiceId",
                unique: true,
                filter: "[SalesInvoiceId] IS NOT NULL");

            migrationBuilder.AddForeignKey(
                name: "FK_PaymentVouchers_Persons_SupplierPhone",
                table: "PaymentVouchers",
                column: "SupplierPhone",
                principalTable: "Persons",
                principalColumn: "Phone");

            migrationBuilder.AddForeignKey(
                name: "FK_ReceiptVouchers_Customers1",
                table: "ReceiptVouchers",
                column: "CustomerPhone",
                principalTable: "Persons",
                principalColumn: "Phone");

            migrationBuilder.AddForeignKey(
                name: "FK_SalesInvoices_Customers1",
                table: "SalesInvoices",
                column: "CustomerPhone",
                principalTable: "Persons",
                principalColumn: "Phone");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PaymentVouchers_Persons_SupplierPhone",
                table: "PaymentVouchers");

            migrationBuilder.DropForeignKey(
                name: "FK_ReceiptVouchers_Customers1",
                table: "ReceiptVouchers");

            migrationBuilder.DropForeignKey(
                name: "FK_SalesInvoices_Customers1",
                table: "SalesInvoices");

            migrationBuilder.DropTable(
                name: "Debt");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Persons",
                table: "Persons");

            migrationBuilder.DropColumn(
                name: "AmountPaid",
                table: "SalesInvoices");

            migrationBuilder.DropColumn(
                name: "AmountPaid",
                table: "PurchaseInvoices");

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                table: "Persons");

            migrationBuilder.DropColumn(
                name: "Discriminator",
                table: "Persons");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "Persons");

            migrationBuilder.DropColumn(
                name: "UpdatedBy",
                table: "Persons");

            migrationBuilder.RenameTable(
                name: "Persons",
                newName: "Suppliers");

            migrationBuilder.RenameColumn(
                name: "Name",
                table: "Suppliers",
                newName: "SupplierName");

            migrationBuilder.RenameColumn(
                name: "Phone",
                table: "Suppliers",
                newName: "SupplierPhone");

            migrationBuilder.AddColumn<decimal>(
                name: "UnitPrice",
                table: "SalesInvoiceItems",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "UnitCost",
                table: "PurchaseInvoiceItems",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedAt",
                table: "Suppliers",
                type: "datetime2",
                nullable: false,
                defaultValueSql: "(sysdatetime())",
                oldClrType: typeof(DateTime),
                oldType: "datetime2(0)",
                oldPrecision: 0,
                oldDefaultValueSql: "sysdatetime()")
                .Annotation("Relational:DefaultConstraintName", "DF_Suppliers_CreatedAt");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Suppliers",
                table: "Suppliers",
                column: "SupplierPhone");

            migrationBuilder.CreateTable(
                name: "Customers",
                columns: table => new
                {
                    CustomerPhone = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(0)", precision: 0, nullable: false, defaultValueSql: "(sysdatetime())")
                        .Annotation("Relational:DefaultConstraintName", "DF_Customers_CreatedAt"),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CustomerName = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Customers", x => x.CustomerPhone);
                });

            migrationBuilder.AddForeignKey(
                name: "FK_PaymentVouchers_Suppliers1",
                table: "PaymentVouchers",
                column: "SupplierPhone",
                principalTable: "Suppliers",
                principalColumn: "SupplierPhone");

            migrationBuilder.AddForeignKey(
                name: "FK_PurchaseInvoices_Suppliers1",
                table: "PurchaseInvoices",
                column: "SupplierPhone",
                principalTable: "Suppliers",
                principalColumn: "SupplierPhone");

            migrationBuilder.AddForeignKey(
                name: "FK_ReceiptVouchers_Customers1",
                table: "ReceiptVouchers",
                column: "CustomerPhone",
                principalTable: "Customers",
                principalColumn: "CustomerPhone");

            migrationBuilder.AddForeignKey(
                name: "FK_SalesInvoices_Customers1",
                table: "SalesInvoices",
                column: "CustomerPhone",
                principalTable: "Customers",
                principalColumn: "CustomerPhone");
        }
    }
}
