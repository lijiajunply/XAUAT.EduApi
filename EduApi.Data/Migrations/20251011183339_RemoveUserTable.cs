using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EduApi.Data.Migrations
{
    /// <inheritdoc />
    public partial class RemoveUserTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_scores_users_UserModelUsername",
                table: "scores");

            migrationBuilder.DropTable(
                name: "users");

            migrationBuilder.DropIndex(
                name: "IX_scores_UserModelUsername",
                table: "scores");

            migrationBuilder.DropColumn(
                name: "UserModelUsername",
                table: "scores");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "UserModelUsername",
                table: "scores",
                type: "TEXT",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "users",
                columns: table => new
                {
                    Username = table.Column<string>(type: "TEXT", maxLength: 10, nullable: false),
                    Id = table.Column<string>(type: "TEXT", maxLength: 64, nullable: true),
                    ScoreResponsesUpdateTime = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_users", x => x.Username);
                });

            migrationBuilder.CreateIndex(
                name: "IX_scores_UserModelUsername",
                table: "scores",
                column: "UserModelUsername");

            migrationBuilder.AddForeignKey(
                name: "FK_scores_users_UserModelUsername",
                table: "scores",
                column: "UserModelUsername",
                principalTable: "users",
                principalColumn: "Username");
        }
    }
}
