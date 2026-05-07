using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FoodWise.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddDeliveryBoxes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "DeliveryBoxId",
                table: "Deliveries",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DropOffImageUrl",
                table: "Deliveries",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "PickedUpAt",
                table: "Deliveries",
                type: "datetime2",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "DeliveryBoxes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DeliveryPointId = table.Column<int>(type: "int", nullable: false),
                    BoxCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    QrCodeValue = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    IsOccupied = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DeliveryBoxes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DeliveryBoxes_DeliveryPoints_DeliveryPointId",
                        column: x => x.DeliveryPointId,
                        principalTable: "DeliveryPoints",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Deliveries_DeliveryBoxId",
                table: "Deliveries",
                column: "DeliveryBoxId");

            migrationBuilder.CreateIndex(
                name: "IX_DeliveryBoxes_DeliveryPointId",
                table: "DeliveryBoxes",
                column: "DeliveryPointId");

            migrationBuilder.CreateIndex(
                name: "IX_DeliveryBoxes_QrCodeValue",
                table: "DeliveryBoxes",
                column: "QrCodeValue",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Deliveries_DeliveryBoxes_DeliveryBoxId",
                table: "Deliveries",
                column: "DeliveryBoxId",
                principalTable: "DeliveryBoxes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Deliveries_DeliveryBoxes_DeliveryBoxId",
                table: "Deliveries");

            migrationBuilder.DropTable(
                name: "DeliveryBoxes");

            migrationBuilder.DropIndex(
                name: "IX_Deliveries_DeliveryBoxId",
                table: "Deliveries");

            migrationBuilder.DropColumn(
                name: "DeliveryBoxId",
                table: "Deliveries");

            migrationBuilder.DropColumn(
                name: "DropOffImageUrl",
                table: "Deliveries");

            migrationBuilder.DropColumn(
                name: "PickedUpAt",
                table: "Deliveries");
        }
    }
}
