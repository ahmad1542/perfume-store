using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PerfumeStore.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddInvoicePricesAndFifoStock : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "CostTotal",
                table: "SalesInvoiceItems",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsCostEstimated",
                table: "SalesInvoiceItems",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsPriceEstimated",
                table: "SalesInvoiceItems",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<decimal>(
                name: "UnitPrice",
                table: "SalesInvoiceItems",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "UsesFifoCost",
                table: "SalesInvoiceItems",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsPriceEstimated",
                table: "PurchaseInvoiceItems",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<decimal>(
                name: "UnitCost",
                table: "PurchaseInvoiceItems",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "InventoryBatches",
                columns: table => new
                {
                    ID = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProductID = table.Column<int>(type: "int", nullable: false),
                    PurchaseInvoiceID = table.Column<long>(type: "bigint", nullable: true),
                    ReceivedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    OriginalQuantity = table.Column<int>(type: "int", nullable: false),
                    RemainingQuantity = table.Column<int>(type: "int", nullable: false),
                    UnitCost = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    IsCostEstimated = table.Column<bool>(type: "bit", nullable: false),
                    Source = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InventoryBatches", x => x.ID);
                    table.ForeignKey(
                        name: "FK_InventoryBatches_Products_ProductID",
                        column: x => x.ProductID,
                        principalTable: "Products",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_InventoryBatches_PurchaseInvoices_PurchaseInvoiceID",
                        column: x => x.PurchaseInvoiceID,
                        principalTable: "PurchaseInvoices",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SalesCostAllocations",
                columns: table => new
                {
                    ID = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SalesInvoiceItemID = table.Column<long>(type: "bigint", nullable: false),
                    InventoryBatchID = table.Column<long>(type: "bigint", nullable: false),
                    Quantity = table.Column<int>(type: "int", nullable: false),
                    UnitCost = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    IsCostEstimated = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SalesCostAllocations", x => x.ID);
                    table.ForeignKey(
                        name: "FK_SalesCostAllocations_InventoryBatches_InventoryBatchID",
                        column: x => x.InventoryBatchID,
                        principalTable: "InventoryBatches",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SalesCostAllocations_SalesInvoiceItems_SalesInvoiceItemID",
                        column: x => x.SalesInvoiceItemID,
                        principalTable: "SalesInvoiceItems",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_InventoryBatches_ProductID_ReceivedAt",
                table: "InventoryBatches",
                columns: new[] { "ProductID", "ReceivedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_InventoryBatches_PurchaseInvoiceID_ProductID",
                table: "InventoryBatches",
                columns: new[] { "PurchaseInvoiceID", "ProductID" },
                unique: true,
                filter: "[PurchaseInvoiceID] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_SalesCostAllocations_InventoryBatchID",
                table: "SalesCostAllocations",
                column: "InventoryBatchID");

            migrationBuilder.CreateIndex(
                name: "IX_SalesCostAllocations_SalesInvoiceItemID",
                table: "SalesCostAllocations",
                column: "SalesInvoiceItemID");

            // Historical line prices were not retained by the old schema.
            // Preserve today's approximation explicitly, rather than pretending
            // that current catalog prices are exact historical values.
            migrationBuilder.Sql("""
                UPDATE items SET UnitCost = products.CostPrice, IsPriceEstimated = 1
                FROM PurchaseInvoiceItems items JOIN Products products ON items.ProductID = products.ID;
                UPDATE items SET UnitPrice = products.SalePrice, CostTotal = products.CostPrice * items.Quantity,
                    IsPriceEstimated = 1, IsCostEstimated = 1
                FROM SalesInvoiceItems items JOIN Products products ON items.ProductID = products.ID;
                INSERT INTO InventoryBatches
                    (ProductID, ReceivedAt, OriginalQuantity, RemainingQuantity, UnitCost, IsCostEstimated, Source)
                SELECT stock.ProductID, CAST('0001-01-01' AS datetime2), stock.Quantity, stock.Quantity,
                    products.CostPrice, 1, 'Opening stock'
                FROM Inventory stock JOIN Products products ON stock.ProductID = products.ID
                WHERE stock.Quantity > 0;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SalesCostAllocations");

            migrationBuilder.DropTable(
                name: "InventoryBatches");

            migrationBuilder.DropColumn(
                name: "CostTotal",
                table: "SalesInvoiceItems");

            migrationBuilder.DropColumn(
                name: "IsCostEstimated",
                table: "SalesInvoiceItems");

            migrationBuilder.DropColumn(
                name: "IsPriceEstimated",
                table: "SalesInvoiceItems");

            migrationBuilder.DropColumn(
                name: "UnitPrice",
                table: "SalesInvoiceItems");

            migrationBuilder.DropColumn(
                name: "UsesFifoCost",
                table: "SalesInvoiceItems");

            migrationBuilder.DropColumn(
                name: "IsPriceEstimated",
                table: "PurchaseInvoiceItems");

            migrationBuilder.DropColumn(
                name: "UnitCost",
                table: "PurchaseInvoiceItems");
        }
    }
}
