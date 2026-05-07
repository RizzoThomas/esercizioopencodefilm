using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FilmAPI.Migrations
{
    /// <inheritdoc />
    public partial class AddCinemaIdToOfferte : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CinemaId",
                table: "Offerte",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Offerte_CinemaId",
                table: "Offerte",
                column: "CinemaId");

            migrationBuilder.AddForeignKey(
                name: "FK_Offerte_Cinemas_CinemaId",
                table: "Offerte",
                column: "CinemaId",
                principalTable: "Cinemas",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Offerte_Cinemas_CinemaId",
                table: "Offerte");

            migrationBuilder.DropIndex(
                name: "IX_Offerte_CinemaId",
                table: "Offerte");

            migrationBuilder.DropColumn(
                name: "CinemaId",
                table: "Offerte");
        }
    }
}
