using System;
using System.Globalization;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FilmAPI.Migrations
{
    /// <inheritdoc />
    public partial class AddMultisalaTicketing : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CinemaPreferitoId",
                table: "Users",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "CreditoResiduo",
                table: "Users",
                type: "decimal(10,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "CastText",
                table: "Films",
                type: "varchar(2000)",
                maxLength: 2000,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<DateOnly>(
                name: "DataRilascio",
                table: "Films",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DescrizioneLunga",
                table: "Films",
                type: "varchar(2000)",
                maxLength: 2000,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "CodiceLocale",
                table: "Cinemas",
                type: "varchar(50)",
                maxLength: 50,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<double>(
                name: "Latitudine",
                table: "Cinemas",
                type: "double",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "Longitudine",
                table: "Cinemas",
                type: "double",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Telefono",
                table: "Cinemas",
                type: "varchar(20)",
                maxLength: 20,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Sale",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    CinemaId = table.Column<int>(type: "int", nullable: false),
                    NumeroProgressivo = table.Column<int>(type: "int", nullable: false),
                    TipoSala = table.Column<int>(type: "int", nullable: false),
                    Nome = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Supplemento = table.Column<decimal>(type: "decimal(10,2)", nullable: false),
                    IsAttiva = table.Column<bool>(type: "tinyint(1)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Sale", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Sale_Cinemas_CinemaId",
                        column: x => x.CinemaId,
                        principalTable: "Cinemas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "SalaPosti",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    SalaId = table.Column<int>(type: "int", nullable: false),
                    Settore = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Fila = table.Column<int>(type: "int", nullable: false),
                    Numero = table.Column<int>(type: "int", nullable: false),
                    PosX = table.Column<int>(type: "int", nullable: true),
                    PosY = table.Column<int>(type: "int", nullable: true),
                    IsWheelchair = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    IsAttivo = table.Column<bool>(type: "tinyint(1)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SalaPosti", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SalaPosti_Sale_SalaId",
                        column: x => x.SalaId,
                        principalTable: "Sale",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Shows",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    CinemaId = table.Column<int>(type: "int", nullable: false),
                    SalaId = table.Column<int>(type: "int", nullable: false),
                    FilmId = table.Column<int>(type: "int", nullable: false),
                    StartAtUtc = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    DurataMinutiSnapshot = table.Column<int>(type: "int", nullable: false),
                    PrezzoBase = table.Column<decimal>(type: "decimal(10,2)", nullable: false),
                    SupplementoSala = table.Column<decimal>(type: "decimal(10,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Shows", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Shows_Cinemas_CinemaId",
                        column: x => x.CinemaId,
                        principalTable: "Cinemas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Shows_Films_FilmId",
                        column: x => x.FilmId,
                        principalTable: "Films",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Shows_Sale_SalaId",
                        column: x => x.SalaId,
                        principalTable: "Sale",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Ordini",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    CodiceOrdine = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    ShowId = table.Column<int>(type: "int", nullable: false),
                    CinemaId = table.Column<int>(type: "int", nullable: false),
                    SalaId = table.Column<int>(type: "int", nullable: false),
                    FilmId = table.Column<int>(type: "int", nullable: false),
                    HoldToken = table.Column<string>(type: "varchar(120)", maxLength: 120, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    NumeroBiglietti = table.Column<int>(type: "int", nullable: false),
                    TotaleLordo = table.Column<decimal>(type: "decimal(10,2)", nullable: false),
                    ImportoCredito = table.Column<decimal>(type: "decimal(10,2)", nullable: false),
                    ImportoCarta = table.Column<decimal>(type: "decimal(10,2)", nullable: false),
                    StripePaymentIntentId = table.Column<string>(type: "varchar(120)", maxLength: 120, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    IdempotencyKey = table.Column<string>(type: "varchar(120)", maxLength: 120, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Stato = table.Column<int>(type: "int", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    PaidAtUtc = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    TicketEmailSentAtUtc = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    TicketEmailLastError = table.Column<string>(type: "varchar(1000)", maxLength: 1000, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Ordini", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Ordini_Cinemas_CinemaId",
                        column: x => x.CinemaId,
                        principalTable: "Cinemas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Ordini_Films_FilmId",
                        column: x => x.FilmId,
                        principalTable: "Films",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Ordini_Sale_SalaId",
                        column: x => x.SalaId,
                        principalTable: "Sale",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Ordini_Shows_ShowId",
                        column: x => x.ShowId,
                        principalTable: "Shows",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Ordini_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Biglietti",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    OrdineId = table.Column<int>(type: "int", nullable: false),
                    ShowId = table.Column<int>(type: "int", nullable: false),
                    SalaPostoId = table.Column<int>(type: "int", nullable: false),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    CodiceBiglietto = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    BarcodeValue = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    PrezzoBase = table.Column<decimal>(type: "decimal(10,2)", nullable: false),
                    Supplemento = table.Column<decimal>(type: "decimal(10,2)", nullable: false),
                    PrezzoTotale = table.Column<decimal>(type: "decimal(10,2)", nullable: false),
                    Stato = table.Column<int>(type: "int", nullable: false),
                    ValidatoAtUtc = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    ValidatoDaUserId = table.Column<int>(type: "int", nullable: true),
                    ValidatoCinemaId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Biglietti", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Biglietti_Cinemas_ValidatoCinemaId",
                        column: x => x.ValidatoCinemaId,
                        principalTable: "Cinemas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Biglietti_Ordini_OrdineId",
                        column: x => x.OrdineId,
                        principalTable: "Ordini",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Biglietti_SalaPosti_SalaPostoId",
                        column: x => x.SalaPostoId,
                        principalTable: "SalaPosti",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Biglietti_Shows_ShowId",
                        column: x => x.ShowId,
                        principalTable: "Shows",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Biglietti_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Biglietti_Users_ValidatoDaUserId",
                        column: x => x.ValidatoDaUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "MovimentiCredito",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    Tipo = table.Column<int>(type: "int", nullable: false),
                    Importo = table.Column<decimal>(type: "decimal(10,2)", nullable: false),
                    SaldoPre = table.Column<decimal>(type: "decimal(10,2)", nullable: false),
                    SaldoPost = table.Column<decimal>(type: "decimal(10,2)", nullable: false),
                    OperatoreUserId = table.Column<int>(type: "int", nullable: true),
                    CinemaId = table.Column<int>(type: "int", nullable: true),
                    OrdineId = table.Column<int>(type: "int", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    Note = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MovimentiCredito", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MovimentiCredito_Cinemas_CinemaId",
                        column: x => x.CinemaId,
                        principalTable: "Cinemas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MovimentiCredito_Ordini_OrdineId",
                        column: x => x.OrdineId,
                        principalTable: "Ordini",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MovimentiCredito_Users_OperatoreUserId",
                        column: x => x.OperatoreUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MovimentiCredito_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "ShowPostiStato",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    ShowId = table.Column<int>(type: "int", nullable: false),
                    SalaPostoId = table.Column<int>(type: "int", nullable: false),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    Stato = table.Column<int>(type: "int", nullable: false),
                    HoldToken = table.Column<string>(type: "varchar(120)", maxLength: 120, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ScadeAtUtc = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    OrdineId = table.Column<int>(type: "int", nullable: true),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ShowPostiStato", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ShowPostiStato_Ordini_OrdineId",
                        column: x => x.OrdineId,
                        principalTable: "Ordini",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ShowPostiStato_SalaPosti_SalaPostoId",
                        column: x => x.SalaPostoId,
                        principalTable: "SalaPosti",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ShowPostiStato_Shows_ShowId",
                        column: x => x.ShowId,
                        principalTable: "Shows",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ShowPostiStato_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_Users_CinemaPreferitoId",
                table: "Users",
                column: "CinemaPreferitoId");

            migrationBuilder.CreateIndex(
                name: "IX_Biglietti_CodiceBiglietto",
                table: "Biglietti",
                column: "CodiceBiglietto",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Biglietti_OrdineId",
                table: "Biglietti",
                column: "OrdineId");

            migrationBuilder.CreateIndex(
                name: "IX_Biglietti_SalaPostoId",
                table: "Biglietti",
                column: "SalaPostoId");

            migrationBuilder.CreateIndex(
                name: "IX_Biglietti_ShowId_SalaPostoId",
                table: "Biglietti",
                columns: new[] { "ShowId", "SalaPostoId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Biglietti_UserId",
                table: "Biglietti",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Biglietti_ValidatoCinemaId",
                table: "Biglietti",
                column: "ValidatoCinemaId");

            migrationBuilder.CreateIndex(
                name: "IX_Biglietti_ValidatoDaUserId",
                table: "Biglietti",
                column: "ValidatoDaUserId");

            migrationBuilder.CreateIndex(
                name: "IX_MovimentiCredito_CinemaId",
                table: "MovimentiCredito",
                column: "CinemaId");

            migrationBuilder.CreateIndex(
                name: "IX_MovimentiCredito_OperatoreUserId",
                table: "MovimentiCredito",
                column: "OperatoreUserId");

            migrationBuilder.CreateIndex(
                name: "IX_MovimentiCredito_OrdineId",
                table: "MovimentiCredito",
                column: "OrdineId");

            migrationBuilder.CreateIndex(
                name: "IX_MovimentiCredito_UserId",
                table: "MovimentiCredito",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Ordini_CinemaId",
                table: "Ordini",
                column: "CinemaId");

            migrationBuilder.CreateIndex(
                name: "IX_Ordini_CodiceOrdine",
                table: "Ordini",
                column: "CodiceOrdine",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Ordini_FilmId",
                table: "Ordini",
                column: "FilmId");

            migrationBuilder.CreateIndex(
                name: "IX_Ordini_IdempotencyKey",
                table: "Ordini",
                column: "IdempotencyKey",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Ordini_SalaId",
                table: "Ordini",
                column: "SalaId");

            migrationBuilder.CreateIndex(
                name: "IX_Ordini_ShowId",
                table: "Ordini",
                column: "ShowId");

            migrationBuilder.CreateIndex(
                name: "IX_Ordini_UserId",
                table: "Ordini",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_SalaPosti_SalaId_Settore_Fila_Numero",
                table: "SalaPosti",
                columns: new[] { "SalaId", "Settore", "Fila", "Numero" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Sale_CinemaId_NumeroProgressivo",
                table: "Sale",
                columns: new[] { "CinemaId", "NumeroProgressivo" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ShowPostiStato_HoldToken",
                table: "ShowPostiStato",
                column: "HoldToken");

            migrationBuilder.CreateIndex(
                name: "IX_ShowPostiStato_OrdineId",
                table: "ShowPostiStato",
                column: "OrdineId");

            migrationBuilder.CreateIndex(
                name: "IX_ShowPostiStato_SalaPostoId",
                table: "ShowPostiStato",
                column: "SalaPostoId");

            migrationBuilder.CreateIndex(
                name: "IX_ShowPostiStato_ScadeAtUtc",
                table: "ShowPostiStato",
                column: "ScadeAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_ShowPostiStato_ShowId_SalaPostoId",
                table: "ShowPostiStato",
                columns: new[] { "ShowId", "SalaPostoId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ShowPostiStato_UserId",
                table: "ShowPostiStato",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Shows_CinemaId_SalaId_StartAtUtc",
                table: "Shows",
                columns: new[] { "CinemaId", "SalaId", "StartAtUtc" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Shows_FilmId",
                table: "Shows",
                column: "FilmId");

            migrationBuilder.CreateIndex(
                name: "IX_Shows_SalaId",
                table: "Shows",
                column: "SalaId");

            migrationBuilder.AddForeignKey(
                name: "FK_Users_Cinemas_CinemaPreferitoId",
                table: "Users",
                column: "CinemaPreferitoId",
                principalTable: "Cinemas",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            var defaultTicketPrice = 8.50m;
            var envDefaultTicketPrice = Environment.GetEnvironmentVariable("DEFAULT_TICKET_PRICE");
            if (!string.IsNullOrWhiteSpace(envDefaultTicketPrice)
                && (decimal.TryParse(envDefaultTicketPrice, NumberStyles.Number, CultureInfo.InvariantCulture, out var parsedPrice)
                    || decimal.TryParse(envDefaultTicketPrice, NumberStyles.Number, CultureInfo.GetCultureInfo("it-IT"), out parsedPrice)
                    || decimal.TryParse(envDefaultTicketPrice, out parsedPrice)))
            {
                defaultTicketPrice = parsedPrice;
            }

            var defaultTicketPriceSql = defaultTicketPrice.ToString("0.00", CultureInfo.InvariantCulture);

            migrationBuilder.Sql($@"
-- =====================================================
-- DATA MIGRATION: AddMultisalaTicketing
-- This handles:
-- 1. Backfill CreditoResiduo = 0 for existing users
-- 2. Create default 'Sala 1' for each cinema that doesn't have any sale
-- 3. Migrate legacy Proiezioni to Shows with conflict handling
-- =====================================================

-- 1. Backfill CreditoResiduo = 0 for existing users
UPDATE Users SET CreditoResiduo = 0 WHERE CreditoResiduo IS NULL;

-- 2. Create default 'Sala 1' for each cinema that doesn't have any sale
INSERT INTO Sale (CinemaId, NumeroProgressivo, TipoSala, Nome, Supplemento, IsAttiva)
SELECT c.Id, 1, 0, 'Sala 1', 0, true
FROM Cinemas c
WHERE NOT EXISTS (SELECT 1 FROM Sale s WHERE s.CinemaId = c.Id);

-- 3. Build legacy slots (StartAt + EndAt from Data/Ora + durata film)
DROP TEMPORARY TABLE IF EXISTS _LegacyProiezioni;
CREATE TEMPORARY TABLE _LegacyProiezioni (
    ProiezioneId INT NOT NULL PRIMARY KEY,
    CinemaId INT NOT NULL,
    FilmId INT NOT NULL,
    StartAtUtc DATETIME NOT NULL,
    EndAtUtc DATETIME NOT NULL,
    DurataMinuti INT NOT NULL
);

INSERT INTO _LegacyProiezioni (ProiezioneId, CinemaId, FilmId, StartAtUtc, EndAtUtc, DurataMinuti)
SELECT
    p.Id,
    p.CinemaId,
    p.FilmId,
    DATE_ADD(DATE(p.Data), INTERVAL TIME_TO_SEC(TIME(p.Ora)) SECOND) AS StartAtUtc,
    DATE_ADD(
        DATE_ADD(DATE(p.Data), INTERVAL TIME_TO_SEC(TIME(p.Ora)) SECOND),
        INTERVAL COALESCE(f.Durata, 120) MINUTE
    ) AS EndAtUtc,
    COALESCE(f.Durata, 120) AS DurataMinuti
FROM Proiezioni p
INNER JOIN Films f ON f.Id = p.FilmId;

-- 4. Mark overlaps/conflicts for Sala 1 assignment
DROP TEMPORARY TABLE IF EXISTS _LegacyConflitti;
CREATE TEMPORARY TABLE _LegacyConflitti (
    ProiezioneId INT NOT NULL PRIMARY KEY,
    CinemaId INT NOT NULL,
    FilmId INT NOT NULL,
    StartAtUtc DATETIME NOT NULL,
    DurataMinuti INT NOT NULL
);

INSERT INTO _LegacyConflitti (ProiezioneId, CinemaId, FilmId, StartAtUtc, DurataMinuti)
SELECT
    lp.ProiezioneId,
    lp.CinemaId,
    lp.FilmId,
    lp.StartAtUtc,
    lp.DurataMinuti
FROM _LegacyProiezioni lp
WHERE EXISTS (
    SELECT 1
    FROM _LegacyProiezioni lp2
    WHERE lp2.CinemaId = lp.CinemaId
      AND lp2.ProiezioneId <> lp.ProiezioneId
      AND lp.StartAtUtc < lp2.EndAtUtc
      AND lp.EndAtUtc > lp2.StartAtUtc
);

-- 5. Non-conflicting Proiezioni -> Sala 1
INSERT INTO Shows (CinemaId, SalaId, FilmId, StartAtUtc, DurataMinutiSnapshot, PrezzoBase, SupplementoSala)
SELECT
    lp.CinemaId,
    s1.Id AS SalaId,
    lp.FilmId,
    lp.StartAtUtc,
    lp.DurataMinuti,
    {defaultTicketPriceSql},
    0
FROM _LegacyProiezioni lp
INNER JOIN Sale s1
    ON s1.CinemaId = lp.CinemaId
   AND s1.NumeroProgressivo = 1
LEFT JOIN _LegacyConflitti lc
    ON lc.ProiezioneId = lp.ProiezioneId
WHERE lc.ProiezioneId IS NULL
  AND NOT EXISTS (
      SELECT 1
      FROM Shows sh
      WHERE sh.CinemaId = lp.CinemaId
        AND sh.SalaId = s1.Id
        AND sh.StartAtUtc = lp.StartAtUtc
  );

-- 6. Conflicting Proiezioni -> auto-migrate salas (one sala per conflicting show)
DROP TEMPORARY TABLE IF EXISTS _LegacyAssegnazioniConflitti;
CREATE TEMPORARY TABLE _LegacyAssegnazioniConflitti (
    ProiezioneId INT NOT NULL PRIMARY KEY,
    CinemaId INT NOT NULL,
    FilmId INT NOT NULL,
    StartAtUtc DATETIME NOT NULL,
    DurataMinuti INT NOT NULL,
    NumeroProgressivo INT NOT NULL,
    KEY IX_Conflitti_Cinema_Numero (CinemaId, NumeroProgressivo)
);

INSERT INTO _LegacyAssegnazioniConflitti (ProiezioneId, CinemaId, FilmId, StartAtUtc, DurataMinuti, NumeroProgressivo)
SELECT
    lc.ProiezioneId,
    lc.CinemaId,
    lc.FilmId,
    lc.StartAtUtc,
    lc.DurataMinuti,
    base.MaxNumeroProgressivo + ROW_NUMBER() OVER (
        PARTITION BY lc.CinemaId
        ORDER BY lc.StartAtUtc, lc.ProiezioneId
    ) AS NumeroProgressivo
FROM _LegacyConflitti lc
INNER JOIN (
    SELECT c.Id AS CinemaId, COALESCE(MAX(s.NumeroProgressivo), 0) AS MaxNumeroProgressivo
    FROM Cinemas c
    LEFT JOIN Sale s ON s.CinemaId = c.Id
    GROUP BY c.Id
) base ON base.CinemaId = lc.CinemaId;

INSERT INTO Sale (CinemaId, NumeroProgressivo, TipoSala, Nome, Supplemento, IsAttiva)
SELECT
    ac.CinemaId,
    ac.NumeroProgressivo,
    0,
    CONCAT('Sala auto-migrata ', ac.NumeroProgressivo),
    0,
    true
FROM _LegacyAssegnazioniConflitti ac
LEFT JOIN Sale s
    ON s.CinemaId = ac.CinemaId
   AND s.NumeroProgressivo = ac.NumeroProgressivo
WHERE s.Id IS NULL;

INSERT INTO Shows (CinemaId, SalaId, FilmId, StartAtUtc, DurataMinutiSnapshot, PrezzoBase, SupplementoSala)
SELECT
    ac.CinemaId,
    s.Id AS SalaId,
    ac.FilmId,
    ac.StartAtUtc,
    ac.DurataMinuti,
    {defaultTicketPriceSql},
    0
FROM _LegacyAssegnazioniConflitti ac
INNER JOIN Sale s
    ON s.CinemaId = ac.CinemaId
   AND s.NumeroProgressivo = ac.NumeroProgressivo
WHERE NOT EXISTS (
    SELECT 1
    FROM Shows sh
    WHERE sh.CinemaId = ac.CinemaId
      AND sh.SalaId = s.Id
      AND sh.StartAtUtc = ac.StartAtUtc
);

DROP TEMPORARY TABLE IF EXISTS _LegacyAssegnazioniConflitti;
DROP TEMPORARY TABLE IF EXISTS _LegacyConflitti;
DROP TEMPORARY TABLE IF EXISTS _LegacyProiezioni;
");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Users_Cinemas_CinemaPreferitoId",
                table: "Users");

            migrationBuilder.DropTable(
                name: "Biglietti");

            migrationBuilder.DropTable(
                name: "MovimentiCredito");

            migrationBuilder.DropTable(
                name: "ShowPostiStato");

            migrationBuilder.DropTable(
                name: "Ordini");

            migrationBuilder.DropTable(
                name: "SalaPosti");

            migrationBuilder.DropTable(
                name: "Shows");

            migrationBuilder.DropTable(
                name: "Sale");

            migrationBuilder.DropIndex(
                name: "IX_Users_CinemaPreferitoId",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "CinemaPreferitoId",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "CreditoResiduo",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "CastText",
                table: "Films");

            migrationBuilder.DropColumn(
                name: "DataRilascio",
                table: "Films");

            migrationBuilder.DropColumn(
                name: "DescrizioneLunga",
                table: "Films");

            migrationBuilder.DropColumn(
                name: "CodiceLocale",
                table: "Cinemas");

            migrationBuilder.DropColumn(
                name: "Latitudine",
                table: "Cinemas");

            migrationBuilder.DropColumn(
                name: "Longitudine",
                table: "Cinemas");

            migrationBuilder.DropColumn(
                name: "Telefono",
                table: "Cinemas");
        }
    }
}
