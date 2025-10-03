using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EduApi.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddUserId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_scores_users_UserModelId",
                table: "scores");

            migrationBuilder.RenameColumn(
                name: "UserModelId",
                table: "scores",
                newName: "UserId");

            migrationBuilder.RenameIndex(
                name: "IX_scores_UserModelId",
                table: "scores",
                newName: "IX_scores_UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_scores_users_UserId",
                table: "scores",
                column: "UserId",
                principalTable: "users",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_scores_users_UserId",
                table: "scores");

            migrationBuilder.RenameColumn(
                name: "UserId",
                table: "scores",
                newName: "UserModelId");

            migrationBuilder.RenameIndex(
                name: "IX_scores_UserId",
                table: "scores",
                newName: "IX_scores_UserModelId");

            migrationBuilder.AddForeignKey(
                name: "FK_scores_users_UserModelId",
                table: "scores",
                column: "UserModelId",
                principalTable: "users",
                principalColumn: "Id");
        }
    }
}
