using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OrderManagement.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddOrderIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Orders_CustomerId",
                table: "Orders");

            migrationBuilder.CreateIndex(
                name: "IX_Orders_CreatedAtUtc",
                table: "Orders",
                column: "CreatedAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_Orders_CustomerId_CreatedAtUtc",
                table: "Orders",
                columns: new[] { "CustomerId", "CreatedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_Orders_Status_CreatedAtUtc",
                table: "Orders",
                columns: new[] { "Status", "CreatedAtUtc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Orders_CreatedAtUtc",
                table: "Orders");

            migrationBuilder.DropIndex(
                name: "IX_Orders_CustomerId_CreatedAtUtc",
                table: "Orders");

            migrationBuilder.DropIndex(
                name: "IX_Orders_Status_CreatedAtUtc",
                table: "Orders");

            migrationBuilder.CreateIndex(
                name: "IX_Orders_CustomerId",
                table: "Orders",
                column: "CustomerId");
        }
    }
}
