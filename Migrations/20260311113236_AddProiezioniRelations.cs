using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CognomeNomeAPI.Migrations
{
    /// <inheritdoc />
    public partial class AddProiezioniRelations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_Proiezioni_FilmId",
                table: "Proiezioni",
                column: "FilmId");

            migrationBuilder.AddForeignKey(
                name: "FK_Proiezioni_Cinemas_CinemaId",
                table: "Proiezioni",
                column: "CinemaId",
                principalTable: "Cinemas",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Proiezioni_Films_FilmId",
                table: "Proiezioni",
                column: "FilmId",
                principalTable: "Films",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Proiezioni_Cinemas_CinemaId",
                table: "Proiezioni");

            migrationBuilder.DropForeignKey(
                name: "FK_Proiezioni_Films_FilmId",
                table: "Proiezioni");

            migrationBuilder.DropIndex(
                name: "IX_Proiezioni_FilmId",
                table: "Proiezioni");
        }
    }
}
