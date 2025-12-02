using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EduApi.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddHasIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
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
            migrationBuilder.DropIndex(
                name: "IX_scores_Semester",
                table: "scores");

            migrationBuilder.DropIndex(
                name: "IX_scores_UserId",
                table: "scores");

            migrationBuilder.DropIndex(
                name: "IX_scores_UserId_Semester",
                table: "scores");
        }
    }
}
