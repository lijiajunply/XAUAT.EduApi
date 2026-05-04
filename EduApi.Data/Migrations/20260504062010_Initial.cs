using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EduApi.Data.Migrations
{
    /// <inheritdoc />
    public partial class Initial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "electricity_subscriptions",
                columns: table => new
                {
                    Id = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    ElectricityUrl = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: false),
                    Email = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    Threshold = table.Column<double>(type: "double precision", nullable: false, defaultValue: 0.0),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    NextCheckAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LastCheckedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastKnownBalance = table.Column<double>(type: "double precision", nullable: true),
                    LastAlertedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastAlertedBalance = table.Column<double>(type: "double precision", nullable: true),
                    LastErrorMessage = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_electricity_subscriptions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "scores",
                columns: table => new
                {
                    Key = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Name = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    LessonCode = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    LessonName = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Grade = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Gpa = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    GradeDetail = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Credit = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    IsMinor = table.Column<bool>(type: "boolean", nullable: false),
                    UserId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Semester = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_scores", x => x.Key);
                });

            migrationBuilder.CreateTable(
                name: "electricity_notification_logs",
                columns: table => new
                {
                    Id = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    SubscriptionId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Email = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    Threshold = table.Column<double>(type: "double precision", nullable: false),
                    Balance = table.Column<double>(type: "double precision", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsSuccess = table.Column<bool>(type: "boolean", nullable: false),
                    Message = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_electricity_notification_logs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_electricity_notification_logs_electricity_subscriptions_Sub~",
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

            migrationBuilder.CreateIndex(
                name: "IX_scores_Semester",
                table: "scores",
                column: "Semester");

            migrationBuilder.CreateIndex(
                name: "IX_scores_UserId",
                table: "scores",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_scores_UserId_Semester",
                table: "scores",
                columns: new[] { "UserId", "Semester" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "electricity_notification_logs");

            migrationBuilder.DropTable(
                name: "scores");

            migrationBuilder.DropTable(
                name: "electricity_subscriptions");
        }
    }
}
