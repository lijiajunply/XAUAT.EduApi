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
                name: "users",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    Username = table.Column<string>(type: "TEXT", maxLength: 10, nullable: false),
                    Password = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    Semesters = table.Column<string>(type: "TEXT", nullable: false),
                    SemesterUpdateTime = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    ScoreResponsesUpdateTime = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_users", x => x.Id);
                });

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
                    UserModelId = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_scores", x => x.Key);
                    table.ForeignKey(
                        name: "FK_scores_users_UserModelId",
                        column: x => x.UserModelId,
                        principalTable: "users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_scores_UserModelId",
                table: "scores",
                column: "UserModelId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "scores");

            migrationBuilder.DropTable(
                name: "users");
        }
    }
}
