using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FoodWise.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddQrVerificationFieldsToDelivery : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsQrVerified",
                table: "Deliveries",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "QrVerifiedAt",
                table: "Deliveries",
                type: "datetime2",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsQrVerified",
                table: "Deliveries");

            migrationBuilder.DropColumn(
                name: "QrVerifiedAt",
                table: "Deliveries");
        }
    }
}
