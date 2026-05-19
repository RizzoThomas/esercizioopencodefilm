using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FilmAPI.Migrations
{
    /// <summary>
    ///     Aggiunge le tabelle Offerte e Vouchers per la gestione di promozioni e sconti. Le offerte possono essere legate a cinema, film o periodi specifici.
    /// </summary>
    /// <summary>
    /// Migrazione che introduce le tabelle Offerte e Vouchers con i relativi indici di unicità e ricerca.
    /// </summary>
    public partial class AddOfferteVoucher : Migration
    {
        /// <summary>
    ///     Aggiunge le tabelle Offerte e Vouchers per la gestione di promozioni e sconti. Le offerte possono essere legate a cinema, film o periodi specifici.
    /// </summary>
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Offerte",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Nome = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Descrizione = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Tipo = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Prezzo = table.Column<decimal>(type: "decimal(65,30)", nullable: false),
                    NumeroBiglietti = table.Column<int>(type: "int", nullable: false),
                    IncludePopcorn = table.Column<int>(type: "int", nullable: false),
                    Attiva = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Offerte", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Vouchers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Codice = table.Column<string>(type: "varchar(255)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ImportoIniziale = table.Column<decimal>(type: "decimal(65,30)", nullable: false),
                    SaldoResiduo = table.Column<decimal>(type: "decimal(65,30)", nullable: false),
                    Stato = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    DataScadenza = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    UserId = table.Column<int>(type: "int", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Vouchers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Vouchers_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id");
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_Vouchers_Codice",
                table: "Vouchers",
                column: "Codice",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Vouchers_UserId",
                table: "Vouchers",
                column: "UserId");
        }

        /// <summary>
    ///     Aggiunge le tabelle Offerte e Vouchers per la gestione di promozioni e sconti. Le offerte possono essere legate a cinema, film o periodi specifici.
    /// </summary>
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Offerte");

            migrationBuilder.DropTable(
                name: "Vouchers");
        }
    }
}
