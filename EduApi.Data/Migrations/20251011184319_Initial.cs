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
                name: "scores",
                columns: table => new
                {
                    Key = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    LessonCode = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    LessonName = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    Grade = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    Gpa = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    GradeDetail = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    Credit = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    IsMinor = table.Column<bool>(type: "INTEGER", nullable: false),
                    UserId = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    Semester = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_scores", x => x.Key);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "scores");
        }
    }
}
