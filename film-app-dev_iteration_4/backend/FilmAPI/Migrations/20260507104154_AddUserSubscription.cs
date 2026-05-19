using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FilmAPI.Migrations
{
    /// <summary>
    ///     Aggiunge la tabella UserSubscriptions per la gestione degli abbonamenti utente. Include: data inizio, data fine, tipo abbonamento, stato (attivo/scaduto/cancellato).
    /// </summary>
    /// <summary>
    /// Migrazione che crea la tabella UserSubscriptions con i relativi indici per abbonamenti, utenti e stato.
    /// </summary>
    public partial class AddUserSubscription : Migration
    {
        /// <summary>
    ///     Aggiunge la tabella UserSubscriptions per la gestione degli abbonamenti utente. Include: data inizio, data fine, tipo abbonamento, stato (attivo/scaduto/cancellato).
    /// </summary>
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "UserSubscriptions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    AbbonamentoId = table.Column<int>(type: "int", nullable: false),
                    MetodoPagamento = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    AutoRinnovo = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    DataInizio = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    DataScadenza = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    Stato = table.Column<string>(type: "varchar(255)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserSubscriptions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserSubscriptions_Abbonamenti_AbbonamentoId",
                        column: x => x.AbbonamentoId,
                        principalTable: "Abbonamenti",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_UserSubscriptions_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_UserSubscriptions_AbbonamentoId",
                table: "UserSubscriptions",
                column: "AbbonamentoId");

            migrationBuilder.CreateIndex(
                name: "IX_UserSubscriptions_DataScadenza",
                table: "UserSubscriptions",
                column: "DataScadenza");

            migrationBuilder.CreateIndex(
                name: "IX_UserSubscriptions_UserId_Stato",
                table: "UserSubscriptions",
                columns: new[] { "UserId", "Stato" });
        }

        /// <summary>
    ///     Aggiunge la tabella UserSubscriptions per la gestione degli abbonamenti utente. Include: data inizio, data fine, tipo abbonamento, stato (attivo/scaduto/cancellato).
    /// </summary>
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "UserSubscriptions");
        }
    }
}
