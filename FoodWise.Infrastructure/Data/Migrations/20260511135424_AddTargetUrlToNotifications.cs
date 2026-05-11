using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FoodWise.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddTargetUrlToNotifications : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "TargetUrl",
                table: "Notifications",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TargetUrl",
                table: "Notifications");
        }
    }
}
