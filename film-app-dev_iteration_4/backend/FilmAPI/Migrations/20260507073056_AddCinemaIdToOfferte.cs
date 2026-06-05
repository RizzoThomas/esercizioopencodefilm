using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FilmAPI.Migrations
{
    /// <summary>
    ///     Aggiunge il campo CinemaId alla tabella Offerte per permettere offerte specifiche per cinema. Aggiorna i dati esistenti con valore null (offerta valida per tutti i cinema).
    /// </summary>
    /// <summary>
    /// Migrazione che aggiunge CinemaId alle offerte e crea l'indice collegato, poi rimuove la struttura al rollback.
    /// </summary>
    public partial class AddCinemaIdToOfferte : Migration
    {
        /// <summary>
    ///     Aggiunge il campo CinemaId alla tabella Offerte per permettere offerte specifiche per cinema. Aggiorna i dati esistenti con valore null (offerta valida per tutti i cinema).
    /// </summary>
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

        /// <summary>
    ///     Aggiunge il campo CinemaId alla tabella Offerte per permettere offerte specifiche per cinema. Aggiorna i dati esistenti con valore null (offerta valida per tutti i cinema).
    /// </summary>
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
