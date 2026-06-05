using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FilmAPI.Migrations
{
    /// <summary>
    ///     Aggiunge il supporto per l'autenticazione a due fattori (2FA/TOTP) e il reset password. Aggiunge campi: TwoFactorEnabled, TwoFactorSecret, ResetPasswordToken, ResetPasswordExpiresAt alla tabella Users.
    /// </summary>
    /// <summary>
    /// Migrazione che introduce il reset password e l'autenticazione a due fattori aggiungendo i relativi campi su Users.
    /// </summary>
    public partial class AddTwoFactorAndPasswordReset : Migration
    {
        /// <summary>
    ///     Aggiunge il supporto per l'autenticazione a due fattori (2FA/TOTP) e il reset password. Aggiunge campi: TwoFactorEnabled, TwoFactorSecret, ResetPasswordToken, ResetPasswordExpiresAt alla tabella Users.
    /// </summary>
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PasswordResetToken",
                table: "Users",
                type: "varchar(128)",
                maxLength: 128,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<DateTime>(
                name: "ResetTokenExpiry",
                table: "Users",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "TwoFactorEnabled",
                table: "Users",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "TwoFactorSecret",
                table: "Users",
                type: "varchar(64)",
                maxLength: 64,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");
        }

        /// <summary>
    ///     Aggiunge il supporto per l'autenticazione a due fattori (2FA/TOTP) e il reset password. Aggiunge campi: TwoFactorEnabled, TwoFactorSecret, ResetPasswordToken, ResetPasswordExpiresAt alla tabella Users.
    /// </summary>
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PasswordResetToken",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "ResetTokenExpiry",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "TwoFactorEnabled",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "TwoFactorSecret",
                table: "Users");
        }
    }
}
