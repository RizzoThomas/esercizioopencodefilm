using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FilmAPI.Migrations
{
    /// <inheritdoc />
    public partial class AddTmdbFieldsToFilm : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "BackdropPath",
                table: "Films",
                type: "varchar(500)",
                maxLength: 500,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "Homepage",
                table: "Films",
                type: "varchar(500)",
                maxLength: 500,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "ImdbId",
                table: "Films",
                type: "varchar(50)",
                maxLength: 50,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "OriginalLanguage",
                table: "Films",
                type: "varchar(10)",
                maxLength: 10,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<double>(
                name: "Popularity",
                table: "Films",
                type: "double",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TmdbId",
                table: "Films",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "VoteAverage",
                table: "Films",
                type: "double",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "VoteCount",
                table: "Films",
                type: "int",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BackdropPath",
                table: "Films");

            migrationBuilder.DropColumn(
                name: "Homepage",
                table: "Films");

            migrationBuilder.DropColumn(
                name: "ImdbId",
                table: "Films");

            migrationBuilder.DropColumn(
                name: "OriginalLanguage",
                table: "Films");

            migrationBuilder.DropColumn(
                name: "Popularity",
                table: "Films");

            migrationBuilder.DropColumn(
                name: "TmdbId",
                table: "Films");

            migrationBuilder.DropColumn(
                name: "VoteAverage",
                table: "Films");

            migrationBuilder.DropColumn(
                name: "VoteCount",
                table: "Films");
        }
    }
}
