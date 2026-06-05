using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FilmAPI.Migrations
{
    /// <summary>
    ///     Aggiunge le tabelle Notifiche e NotificheSoppresse per il sistema di notifiche push/email. Gestisce: invio, lettura, soppressione silenziosa per tipo notifica.
    /// </summary>
    /// <summary>
    /// Migrazione che crea le tabelle Notifiche e NotificheSoppresse con i relativi indici utente.
    /// </summary>
    public partial class AddNotifiche : Migration
    {
        /// <summary>
    ///     Aggiunge le tabelle Notifiche e NotificheSoppresse per il sistema di notifiche push/email. Gestisce: invio, lettura, soppressione silenziosa per tipo notifica.
    /// </summary>
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Notifiche",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    Tipo = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Titolo = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Descrizione = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Icona = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Letto = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Notifiche", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Notifiche_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "NotificheSoppresse",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    SourceId = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NotificheSoppresse", x => x.Id);
                    table.ForeignKey(
                        name: "FK_NotificheSoppresse_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_Notifiche_UserId",
                table: "Notifiche",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_NotificheSoppresse_UserId",
                table: "NotificheSoppresse",
                column: "UserId");
        }

        /// <summary>
    ///     Aggiunge le tabelle Notifiche e NotificheSoppresse per il sistema di notifiche push/email. Gestisce: invio, lettura, soppressione silenziosa per tipo notifica.
    /// </summary>
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Notifiche");

            migrationBuilder.DropTable(
                name: "NotificheSoppresse");
        }
    }
}
