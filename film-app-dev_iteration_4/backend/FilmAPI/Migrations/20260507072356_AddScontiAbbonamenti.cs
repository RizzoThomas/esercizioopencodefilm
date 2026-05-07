using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FilmAPI.Migrations
{
    /// <inheritdoc />
    public partial class AddScontiAbbonamenti : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "InEvidenza",
                table: "Offerte",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<decimal>(
                name: "PrezzoOriginale",
                table: "Offerte",
                type: "decimal(65,30)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ScontoPercentuale",
                table: "Offerte",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "Abbonamenti",
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
                    PrezzoAnnuale = table.Column<decimal>(type: "decimal(65,30)", nullable: true),
                    ScontoPercentuale = table.Column<int>(type: "int", nullable: false),
                    NumeroBigliettiPerMese = table.Column<int>(type: "int", nullable: false),
                    IncludePopcornPerMese = table.Column<int>(type: "int", nullable: false),
                    Attivo = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Abbonamenti", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Abbonamenti");

            migrationBuilder.DropColumn(
                name: "InEvidenza",
                table: "Offerte");

            migrationBuilder.DropColumn(
                name: "PrezzoOriginale",
                table: "Offerte");

            migrationBuilder.DropColumn(
                name: "ScontoPercentuale",
                table: "Offerte");
        }
    }
}
