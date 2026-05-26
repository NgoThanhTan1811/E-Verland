using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EVerland.Migrations.OrderDb
{
    /// <inheritdoc />
    public partial class AddIsDeletedToOrders : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "orders",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "orders",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "orders",
                type: "bytea",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "order_items",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "order_items",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "order_items",
                type: "bytea",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "orders");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "orders");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "orders");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "order_items");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "order_items");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "order_items");
        }
    }
}
