using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EduApi.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddExamRecords : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "exam_records",
                columns: table => new
                {
                    Key = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    StudentId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    Time = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    ExamTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Location = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    Seat = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_exam_records", x => x.Key);
                });

            migrationBuilder.CreateIndex(
                name: "IX_exam_records_ExamTime",
                table: "exam_records",
                column: "ExamTime");

            migrationBuilder.CreateIndex(
                name: "IX_exam_records_StudentId",
                table: "exam_records",
                column: "StudentId");

            migrationBuilder.CreateIndex(
                name: "IX_exam_records_StudentId_ExamTime",
                table: "exam_records",
                columns: new[] { "StudentId", "ExamTime" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "exam_records");
        }
    }
}
