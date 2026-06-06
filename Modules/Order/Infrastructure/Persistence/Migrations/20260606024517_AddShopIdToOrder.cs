using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EVerland.Migrations.OrderDb
{
    /// <inheritdoc />
    public partial class AddShopIdToOrder : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "ShopId",
                table: "orders",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid());

            migrationBuilder.AddColumn<Guid>(
                name: "SellerId",
                table: "order_items",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid());
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ShopId",
                table: "orders");

            migrationBuilder.DropColumn(
                name: "SellerId",
                table: "order_items");
        }
    }
}
