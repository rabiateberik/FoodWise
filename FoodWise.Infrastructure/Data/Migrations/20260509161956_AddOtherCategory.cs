using Microsoft.EntityFrameworkCore.Migrations;
using System;
#nullable disable

namespace FoodWise.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddOtherCategory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "Categories",
                columns: new[] { "Id", "Name", "Description", "CreatedAt", "UpdatedAt", "IsActive" },
                values: new object[]
                {
            7,
            "Diğer",
            "Kullanıcı tarafından eklenen ve belirli kategoriye atanamayan ürünler için varsayılan kategori.",
            DateTime.Now,
            null,
            true
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 7);
        }
    }
}
