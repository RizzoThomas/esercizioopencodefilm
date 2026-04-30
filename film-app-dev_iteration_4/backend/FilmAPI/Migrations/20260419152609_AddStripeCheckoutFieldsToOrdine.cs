using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FilmAPI.Migrations
{
    /// <inheritdoc />
    public partial class AddStripeCheckoutFieldsToOrdine : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "CheckoutCompletedAtUtc",
                table: "Ordini",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CheckoutExpiresAtUtc",
                table: "Ordini",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "CreditoRiservato",
                table: "Ordini",
                type: "decimal(10,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "LastPaymentError",
                table: "Ordini",
                type: "varchar(1000)",
                maxLength: 1000,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "StripeCheckoutSessionId",
                table: "Ordini",
                type: "varchar(120)",
                maxLength: 120,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CheckoutCompletedAtUtc",
                table: "Ordini");

            migrationBuilder.DropColumn(
                name: "CheckoutExpiresAtUtc",
                table: "Ordini");

            migrationBuilder.DropColumn(
                name: "CreditoRiservato",
                table: "Ordini");

            migrationBuilder.DropColumn(
                name: "LastPaymentError",
                table: "Ordini");

            migrationBuilder.DropColumn(
                name: "StripeCheckoutSessionId",
                table: "Ordini");
        }
    }
}
