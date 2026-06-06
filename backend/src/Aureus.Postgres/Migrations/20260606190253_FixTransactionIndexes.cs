using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Aureus.Postgres.Migrations
{
    /// <inheritdoc />
    public partial class FixTransactionIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_transactions_financial_account_id",
                table: "transactions");

            migrationBuilder.DropIndex(
                name: "IX_transactions_workspace_id_is_deleted",
                table: "transactions");

            migrationBuilder.CreateIndex(
                name: "IX_transactions_financial_account_id_is_deleted",
                table: "transactions",
                columns: new[] { "financial_account_id", "is_deleted" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_transactions_financial_account_id_is_deleted",
                table: "transactions");

            migrationBuilder.CreateIndex(
                name: "IX_transactions_financial_account_id",
                table: "transactions",
                column: "financial_account_id");

            migrationBuilder.CreateIndex(
                name: "IX_transactions_workspace_id_is_deleted",
                table: "transactions",
                columns: new[] { "workspace_id", "is_deleted" });
        }
    }
}
