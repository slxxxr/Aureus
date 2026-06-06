using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Aureus.Postgres.Migrations
{
    /// <inheritdoc />
    public partial class AddCategorySoftDelete : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_categories_workspace_id_name_type",
                table: "categories");

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "deleted_at",
                table: "categories",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "is_deleted",
                table: "categories",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateIndex(
                name: "IX_categories_workspace_id_name_type",
                table: "categories",
                columns: new[] { "workspace_id", "name", "type" },
                unique: true,
                filter: "\"is_deleted\" = false");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_categories_workspace_id_name_type",
                table: "categories");

            migrationBuilder.DropColumn(
                name: "deleted_at",
                table: "categories");

            migrationBuilder.DropColumn(
                name: "is_deleted",
                table: "categories");

            migrationBuilder.CreateIndex(
                name: "IX_categories_workspace_id_name_type",
                table: "categories",
                columns: new[] { "workspace_id", "name", "type" },
                unique: true);
        }
    }
}
