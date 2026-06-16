using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Aureus.Postgres.Migrations
{
    /// <inheritdoc />
    public partial class RemoveWorkspaceOwnerUserId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_workspaces_users_owner_user_id",
                table: "workspaces");

            migrationBuilder.DropIndex(
                name: "IX_workspaces_owner_user_id_name",
                table: "workspaces");

            migrationBuilder.DropColumn(
                name: "owner_user_id",
                table: "workspaces");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "owner_user_id",
                table: "workspaces",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateIndex(
                name: "IX_workspaces_owner_user_id_name",
                table: "workspaces",
                columns: new[] { "owner_user_id", "name" },
                unique: true,
                filter: "\"is_deleted\" = false");

            migrationBuilder.AddForeignKey(
                name: "FK_workspaces_users_owner_user_id",
                table: "workspaces",
                column: "owner_user_id",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
