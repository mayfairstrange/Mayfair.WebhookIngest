using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Mayfair.WebhookIngest.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class CheckModelChange : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "LastAttemptAt",
                table: "IncomingEvents",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LockId",
                table: "IncomingEvents",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "LockedUntil",
                table: "IncomingEvents",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "NextAttemptAt",
                table: "IncomingEvents",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "ProcessedAt",
                table: "IncomingEvents",
                type: "timestamp with time zone",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LastAttemptAt",
                table: "IncomingEvents");

            migrationBuilder.DropColumn(
                name: "LockId",
                table: "IncomingEvents");

            migrationBuilder.DropColumn(
                name: "LockedUntil",
                table: "IncomingEvents");

            migrationBuilder.DropColumn(
                name: "NextAttemptAt",
                table: "IncomingEvents");

            migrationBuilder.DropColumn(
                name: "ProcessedAt",
                table: "IncomingEvents");
        }
    }
}
