using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PerfumeStore.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RepairReceiptVoucherPersonColumn : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Earlier snapshots included this property without creating its database column.
            // Some existing databases already have it, so repair only what is missing.
            migrationBuilder.Sql("""
                IF COL_LENGTH(N'dbo.ReceiptVouchers', N'PersonId') IS NULL
                    ALTER TABLE [dbo].[ReceiptVouchers] ADD [PersonId] uniqueidentifier NULL;

                IF NOT EXISTS (SELECT 1 FROM sys.indexes
                    WHERE object_id = OBJECT_ID(N'dbo.ReceiptVouchers')
                    AND name = N'IX_ReceiptVouchers_PersonId')
                    CREATE INDEX [IX_ReceiptVouchers_PersonId] ON [dbo].[ReceiptVouchers] ([PersonId]);
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Keep the column and any voucher associations when rolling back this repair.
        }
    }
}
