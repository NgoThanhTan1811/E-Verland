using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EVerland.Migrations
{
    /// <inheritdoc />
    public partial class InitialShippingDb : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "shipping_orders",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OrderId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Provider = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    ProviderOrderCode = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    ClientOrderCode = table.Column<string>(type: "text", nullable: true),
                    Status = table.Column<string>(type: "text", nullable: false),
                    ProviderStatus = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    ServiceId = table.Column<int>(type: "integer", nullable: true),
                    ServiceTypeId = table.Column<int>(type: "integer", nullable: true),
                    PaymentTypeId = table.Column<int>(type: "integer", nullable: true),
                    CodAmount = table.Column<decimal>(type: "numeric", nullable: false),
                    InsuranceValue = table.Column<decimal>(type: "numeric", nullable: false),
                    Weight = table.Column<int>(type: "integer", nullable: false),
                    Length = table.Column<int>(type: "integer", nullable: false),
                    Width = table.Column<int>(type: "integer", nullable: false),
                    Height = table.Column<int>(type: "integer", nullable: false),
                    ExpectedDeliveryTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    TotalFee = table.Column<decimal>(type: "numeric", nullable: false),
                    FeeSnapshot = table.Column<string>(type: "jsonb", nullable: true),
                    ToAddress = table.Column<string>(type: "jsonb", nullable: false),
                    FromAddress = table.Column<string>(type: "jsonb", nullable: true),
                    Items = table.Column<string>(type: "jsonb", nullable: false),
                    Note = table.Column<string>(type: "text", nullable: true),
                    RequiredNote = table.Column<string>(type: "text", nullable: true),
                    LastSyncedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "bytea", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_shipping_orders", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_shipping_orders_OrderId",
                table: "shipping_orders",
                column: "OrderId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_shipping_orders_ProviderOrderCode",
                table: "shipping_orders",
                column: "ProviderOrderCode");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "shipping_orders");
        }
    }
}
