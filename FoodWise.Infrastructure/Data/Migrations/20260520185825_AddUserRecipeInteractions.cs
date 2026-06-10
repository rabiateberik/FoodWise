using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FoodWise.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddUserRecipeInteractions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "UserRecipeInteractions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    RecipeId = table.Column<int>(type: "int", nullable: false),
                    InteractionType = table.Column<int>(type: "int", nullable: false),
                    StockItemId = table.Column<int>(type: "int", nullable: true),
                    RecommendationScore = table.Column<int>(type: "int", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserRecipeInteractions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserRecipeInteractions_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_UserRecipeInteractions_Recipes_RecipeId",
                        column: x => x.RecipeId,
                        principalTable: "Recipes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UserRecipeInteractions_StockItems_StockItemId",
                        column: x => x.StockItemId,
                        principalTable: "StockItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_UserRecipeInteractions_RecipeId",
                table: "UserRecipeInteractions",
                column: "RecipeId");

            migrationBuilder.CreateIndex(
                name: "IX_UserRecipeInteractions_StockItemId",
                table: "UserRecipeInteractions",
                column: "StockItemId");

            migrationBuilder.CreateIndex(
                name: "IX_UserRecipeInteractions_UserId_RecipeId_InteractionType_CreatedAt",
                table: "UserRecipeInteractions",
                columns: new[] { "UserId", "RecipeId", "InteractionType", "CreatedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "UserRecipeInteractions");
        }
    }
}
