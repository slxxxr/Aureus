using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Aureus.Postgres.Migrations
{
    /// <inheritdoc />
    public partial class TransactionOccurredAtToDate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                "ALTER TABLE transactions ALTER COLUMN occurred_at TYPE date " +
                "USING (occurred_at AT TIME ZONE 'UTC')::date;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                "ALTER TABLE transactions ALTER COLUMN occurred_at TYPE timestamp with time zone " +
                "USING (occurred_at::timestamp AT TIME ZONE 'UTC');");
        }
    }
}
