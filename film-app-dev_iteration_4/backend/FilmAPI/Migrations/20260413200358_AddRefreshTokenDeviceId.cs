using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FilmAPI.Migrations
{
    /// <inheritdoc />
    public partial class AddRefreshTokenDeviceId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "DeviceId",
                table: "RefreshTokens",
                type: "varchar(128)",
                maxLength: 128,
                nullable: false,
                defaultValue: "web-default")
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.Sql("UPDATE RefreshTokens SET DeviceId = 'web-default' WHERE DeviceId = '';");

            migrationBuilder.CreateIndex(
                name: "IX_RefreshTokens_UserId_DeviceId",
                table: "RefreshTokens",
                columns: new[] { "UserId", "DeviceId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_RefreshTokens_UserId_DeviceId",
                table: "RefreshTokens");

            migrationBuilder.DropColumn(
                name: "DeviceId",
                table: "RefreshTokens");
        }
    }
}
