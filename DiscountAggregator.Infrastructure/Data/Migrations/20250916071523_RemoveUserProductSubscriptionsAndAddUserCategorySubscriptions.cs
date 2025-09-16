using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DiscountAggregator.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class RemoveUserProductSubscriptionsAndAddUserCategorySubscriptions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DELETE FROM \"ProductPriceHistories\"");
            migrationBuilder.Sql("DELETE FROM \"SearchQueries\"");
            migrationBuilder.Sql("DELETE FROM \"UserProductSubscriptions\"");
            migrationBuilder.Sql("DELETE FROM \"Products\"");
            migrationBuilder.Sql("DELETE FROM \"Users\"");


            // Удаляем таблицу UserProductSubscriptions
            migrationBuilder.DropTable(
                name: "UserProductSubscriptions");


            migrationBuilder.CreateTable(
                name: "UserCategorySubscriptions",
                columns: table => new
                {
                    UserId = table.Column<long>(type: "bigint", nullable: false),
                    Keyword = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    SourceFilter = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    SubscribedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserCategorySubscriptions", x => new { x.UserId, x.Keyword, x.SourceFilter });
                    table.ForeignKey(
                        name: "FK_UserCategorySubscriptions_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_UserCategorySubscriptions_UserId_IsActive",
                table: "UserCategorySubscriptions",
                columns: new[] { "UserId", "IsActive" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Удаляем таблицу UserCategorySubscriptions
            migrationBuilder.DropTable(
                name: "UserCategorySubscriptions");

            // Восстанавливаем таблицу UserProductSubscriptions
            migrationBuilder.CreateTable(
                name: "UserProductSubscriptions",
                columns: table => new
                {
                    UserId = table.Column<long>(type: "bigint", nullable: false),
                    ProductId = table.Column<Guid>(type: "uuid", nullable: false),
                    SubscribedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserProductSubscriptions", x => new { x.UserId, x.ProductId });
                    table.ForeignKey(
                        name: "FK_UserProductSubscriptions_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UserProductSubscriptions_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_UserProductSubscriptions_ProductId",
                table: "UserProductSubscriptions",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_UserProductSubscriptions_UserId_IsActive",
                table: "UserProductSubscriptions",
                columns: new[] { "UserId", "IsActive" });
        }
    }
}
