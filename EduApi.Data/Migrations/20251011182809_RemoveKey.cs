using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EduApi.Data.Migrations
{
    /// <inheritdoc />
    public partial class RemoveKey : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_scores_users_UserId",
                table: "scores");

            migrationBuilder.DropUniqueConstraint(
                name: "AK_users_Id",
                table: "users");

            migrationBuilder.DropIndex(
                name: "IX_scores_UserId",
                table: "scores");

            migrationBuilder.AlterColumn<string>(
                name: "Id",
                table: "users",
                type: "TEXT",
                maxLength: 64,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "TEXT",
                oldMaxLength: 64);

            migrationBuilder.AddColumn<string>(
                name: "UserModelUsername",
                table: "scores",
                type: "TEXT",
                nullable: true);

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

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_scores_users_UserModelUsername",
                table: "scores");

            migrationBuilder.DropIndex(
                name: "IX_scores_UserModelUsername",
                table: "scores");

            migrationBuilder.DropColumn(
                name: "UserModelUsername",
                table: "scores");

            migrationBuilder.AlterColumn<string>(
                name: "Id",
                table: "users",
                type: "TEXT",
                maxLength: 64,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "TEXT",
                oldMaxLength: 64,
                oldNullable: true);

            migrationBuilder.AddUniqueConstraint(
                name: "AK_users_Id",
                table: "users",
                column: "Id");

            migrationBuilder.CreateIndex(
                name: "IX_scores_UserId",
                table: "scores",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_scores_users_UserId",
                table: "scores",
                column: "UserId",
                principalTable: "users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
