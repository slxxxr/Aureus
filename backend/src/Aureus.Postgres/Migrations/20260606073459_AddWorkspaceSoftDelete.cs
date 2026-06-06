using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Aureus.Postgres.Migrations
{
    /// <inheritdoc />
    public partial class AddWorkspaceSoftDelete : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_workspaces_owner_user_id",
                table: "workspaces");

            migrationBuilder.DropIndex(
                name: "IX_workspace_members_workspace_id_user_id",
                table: "workspace_members");

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "deleted_at",
                table: "workspaces",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "is_deleted",
                table: "workspaces",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "deleted_at",
                table: "workspace_members",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "is_deleted",
                table: "workspace_members",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateIndex(
                name: "IX_workspaces_owner_user_id_name",
                table: "workspaces",
                columns: new[] { "owner_user_id", "name" },
                unique: true,
                filter: "\"is_deleted\" = false");

            migrationBuilder.CreateIndex(
                name: "IX_workspace_members_workspace_id_user_id",
                table: "workspace_members",
                columns: new[] { "workspace_id", "user_id" },
                unique: true,
                filter: "\"is_deleted\" = false");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_workspaces_owner_user_id_name",
                table: "workspaces");

            migrationBuilder.DropIndex(
                name: "IX_workspace_members_workspace_id_user_id",
                table: "workspace_members");

            migrationBuilder.DropColumn(
                name: "deleted_at",
                table: "workspaces");

            migrationBuilder.DropColumn(
                name: "is_deleted",
                table: "workspaces");

            migrationBuilder.DropColumn(
                name: "deleted_at",
                table: "workspace_members");

            migrationBuilder.DropColumn(
                name: "is_deleted",
                table: "workspace_members");

            migrationBuilder.CreateIndex(
                name: "IX_workspaces_owner_user_id",
                table: "workspaces",
                column: "owner_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_workspace_members_workspace_id_user_id",
                table: "workspace_members",
                columns: new[] { "workspace_id", "user_id" },
                unique: true);
        }
    }
}
