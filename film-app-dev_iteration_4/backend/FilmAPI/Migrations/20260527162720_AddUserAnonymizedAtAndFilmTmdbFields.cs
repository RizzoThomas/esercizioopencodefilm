using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FilmAPI.Migrations
{
    /// <inheritdoc />
    public partial class AddUserAnonymizedAtAndFilmTmdbFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AccountActionTokens_Users_CreatedByUserId",
                table: "AccountActionTokens");

            migrationBuilder.DropForeignKey(
                name: "FK_Biglietti_Cinemas_ValidatoCinemaId",
                table: "Biglietti");

            migrationBuilder.DropForeignKey(
                name: "FK_Biglietti_Users_ValidatoDaUserId",
                table: "Biglietti");

            migrationBuilder.DropForeignKey(
                name: "FK_UserSecurityAuditLogs_Users_ActorUserId",
                table: "UserSecurityAuditLogs");

            migrationBuilder.DropForeignKey(
                name: "FK_UserSecurityAuditLogs_Users_UserId",
                table: "UserSecurityAuditLogs");

            migrationBuilder.DropForeignKey(
                name: "FK_UserSubscriptions_Abbonamenti_AbbonamentoId",
                table: "UserSubscriptions");

            migrationBuilder.DropIndex(
                name: "IX_WatchlistItems_UserId",
                table: "WatchlistItems");

            migrationBuilder.AlterColumn<string>(
                name: "TwoFactorSecret",
                table: "Users",
                type: "varchar(128)",
                maxLength: 128,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "varchar(64)",
                oldMaxLength: 64,
                oldNullable: true)
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AlterColumn<string>(
                name: "PasswordHash",
                table: "Users",
                type: "longtext",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "varchar(255)",
                oldMaxLength: 255,
                oldNullable: true)
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<DateTime>(
                name: "AnonymizedAtUtc",
                table: "Users",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "PrivacyPolicyAcceptedAtUtc",
                table: "Users",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PrivacyPolicyVersion",
                table: "Users",
                type: "varchar(50)",
                maxLength: 50,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<DateTime>(
                name: "TermsAcceptedAtUtc",
                table: "Users",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TermsAcceptedVersion",
                table: "Users",
                type: "varchar(50)",
                maxLength: 50,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AlterColumn<string>(
                name: "ImdbId",
                table: "Films",
                type: "varchar(20)",
                maxLength: 20,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "varchar(50)",
                oldMaxLength: 50,
                oldNullable: true)
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "CancellationReason",
                table: "Biglietti",
                type: "varchar(500)",
                maxLength: 500,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<DateTime>(
                name: "CancelledAtUtc",
                table: "Biglietti",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CancelledByUserId",
                table: "Biglietti",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "OrdineRefundId",
                table: "Biglietti",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "OrdineRefund",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    OrdineId = table.Column<int>(type: "int", nullable: false),
                    Importo = table.Column<decimal>(type: "decimal(10,2)", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    CompletedAtUtc = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    Motivo = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ExternalRefundId = table.Column<string>(type: "varchar(120)", maxLength: 120, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Stato = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrdineRefund", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OrdineRefund_Ordini_OrdineId",
                        column: x => x.OrdineId,
                        principalTable: "Ordini",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_Biglietti_CancelledByUserId",
                table: "Biglietti",
                column: "CancelledByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Biglietti_OrdineRefundId",
                table: "Biglietti",
                column: "OrdineRefundId");

            migrationBuilder.CreateIndex(
                name: "IX_OrdineRefund_OrdineId",
                table: "OrdineRefund",
                column: "OrdineId");

            migrationBuilder.AddForeignKey(
                name: "FK_AccountActionTokens_Users_CreatedByUserId",
                table: "AccountActionTokens",
                column: "CreatedByUserId",
                principalTable: "Users",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Biglietti_Cinemas_ValidatoCinemaId",
                table: "Biglietti",
                column: "ValidatoCinemaId",
                principalTable: "Cinemas",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Biglietti_OrdineRefund_OrdineRefundId",
                table: "Biglietti",
                column: "OrdineRefundId",
                principalTable: "OrdineRefund",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Biglietti_Users_CancelledByUserId",
                table: "Biglietti",
                column: "CancelledByUserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Biglietti_Users_ValidatoDaUserId",
                table: "Biglietti",
                column: "ValidatoDaUserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_UserSecurityAuditLogs_Users_ActorUserId",
                table: "UserSecurityAuditLogs",
                column: "ActorUserId",
                principalTable: "Users",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_UserSecurityAuditLogs_Users_UserId",
                table: "UserSecurityAuditLogs",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_UserSubscriptions_Abbonamenti_AbbonamentoId",
                table: "UserSubscriptions",
                column: "AbbonamentoId",
                principalTable: "Abbonamenti",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AccountActionTokens_Users_CreatedByUserId",
                table: "AccountActionTokens");

            migrationBuilder.DropForeignKey(
                name: "FK_Biglietti_Cinemas_ValidatoCinemaId",
                table: "Biglietti");

            migrationBuilder.DropForeignKey(
                name: "FK_Biglietti_OrdineRefund_OrdineRefundId",
                table: "Biglietti");

            migrationBuilder.DropForeignKey(
                name: "FK_Biglietti_Users_CancelledByUserId",
                table: "Biglietti");

            migrationBuilder.DropForeignKey(
                name: "FK_Biglietti_Users_ValidatoDaUserId",
                table: "Biglietti");

            migrationBuilder.DropForeignKey(
                name: "FK_UserSecurityAuditLogs_Users_ActorUserId",
                table: "UserSecurityAuditLogs");

            migrationBuilder.DropForeignKey(
                name: "FK_UserSecurityAuditLogs_Users_UserId",
                table: "UserSecurityAuditLogs");

            migrationBuilder.DropForeignKey(
                name: "FK_UserSubscriptions_Abbonamenti_AbbonamentoId",
                table: "UserSubscriptions");

            migrationBuilder.DropTable(
                name: "OrdineRefund");

            migrationBuilder.DropIndex(
                name: "IX_Biglietti_CancelledByUserId",
                table: "Biglietti");

            migrationBuilder.DropIndex(
                name: "IX_Biglietti_OrdineRefundId",
                table: "Biglietti");

            migrationBuilder.DropColumn(
                name: "AnonymizedAtUtc",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "PrivacyPolicyAcceptedAtUtc",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "PrivacyPolicyVersion",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "TermsAcceptedAtUtc",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "TermsAcceptedVersion",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "CancellationReason",
                table: "Biglietti");

            migrationBuilder.DropColumn(
                name: "CancelledAtUtc",
                table: "Biglietti");

            migrationBuilder.DropColumn(
                name: "CancelledByUserId",
                table: "Biglietti");

            migrationBuilder.DropColumn(
                name: "OrdineRefundId",
                table: "Biglietti");

            migrationBuilder.AlterColumn<string>(
                name: "TwoFactorSecret",
                table: "Users",
                type: "varchar(64)",
                maxLength: 64,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "varchar(128)",
                oldMaxLength: 128,
                oldNullable: true)
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AlterColumn<string>(
                name: "PasswordHash",
                table: "Users",
                type: "varchar(255)",
                maxLength: 255,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "longtext",
                oldNullable: true)
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AlterColumn<string>(
                name: "ImdbId",
                table: "Films",
                type: "varchar(50)",
                maxLength: 50,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "varchar(20)",
                oldMaxLength: 20,
                oldNullable: true)
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_WatchlistItems_UserId",
                table: "WatchlistItems",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_AccountActionTokens_Users_CreatedByUserId",
                table: "AccountActionTokens",
                column: "CreatedByUserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Biglietti_Cinemas_ValidatoCinemaId",
                table: "Biglietti",
                column: "ValidatoCinemaId",
                principalTable: "Cinemas",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Biglietti_Users_ValidatoDaUserId",
                table: "Biglietti",
                column: "ValidatoDaUserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_UserSecurityAuditLogs_Users_ActorUserId",
                table: "UserSecurityAuditLogs",
                column: "ActorUserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_UserSecurityAuditLogs_Users_UserId",
                table: "UserSecurityAuditLogs",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_UserSubscriptions_Abbonamenti_AbbonamentoId",
                table: "UserSubscriptions",
                column: "AbbonamentoId",
                principalTable: "Abbonamenti",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
