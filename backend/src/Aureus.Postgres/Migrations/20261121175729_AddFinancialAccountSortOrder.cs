using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Aureus.Postgres.Migrations
{
    /// <inheritdoc />
    public partial class AddFinancialAccountSortOrder : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<short>(
                name: "sort_order",
                table: "financial_accounts",
                type: "smallint",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "sort_order",
                table: "financial_accounts");
        }
    }
}
