using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EduApi.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddElectricitySubscription : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "electricity_subscriptions",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    ElectricityUrl = table.Column<string>(type: "TEXT", maxLength: 1024, nullable: false),
                    Email = table.Column<string>(type: "TEXT", maxLength: 256, nullable: false),
                    Threshold = table.Column<double>(type: "REAL", nullable: false, defaultValue: 0.0),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    NextCheckAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    LastCheckedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    LastKnownBalance = table.Column<double>(type: "REAL", nullable: true),
                    LastAlertedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    LastAlertedBalance = table.Column<double>(type: "REAL", nullable: true),
                    LastErrorMessage = table.Column<string>(type: "TEXT", maxLength: 512, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_electricity_subscriptions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "electricity_notification_logs",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    SubscriptionId = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    Email = table.Column<string>(type: "TEXT", maxLength: 256, nullable: false),
                    Threshold = table.Column<double>(type: "REAL", nullable: false),
                    Balance = table.Column<double>(type: "REAL", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    IsSuccess = table.Column<bool>(type: "INTEGER", nullable: false),
                    Message = table.Column<string>(type: "TEXT", maxLength: 1024, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_electricity_notification_logs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_electricity_notification_logs_electricity_subscriptions_SubscriptionId",
                        column: x => x.SubscriptionId,
                        principalTable: "electricity_subscriptions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_electricity_notification_logs_SubscriptionId_CreatedAt",
                table: "electricity_notification_logs",
                columns: new[] { "SubscriptionId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_electricity_subscriptions_Email_ElectricityUrl",
                table: "electricity_subscriptions",
                columns: new[] { "Email", "ElectricityUrl" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_electricity_subscriptions_IsActive_NextCheckAt",
                table: "electricity_subscriptions",
                columns: new[] { "IsActive", "NextCheckAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "electricity_notification_logs");

            migrationBuilder.DropTable(
                name: "electricity_subscriptions");
        }
    }
}
