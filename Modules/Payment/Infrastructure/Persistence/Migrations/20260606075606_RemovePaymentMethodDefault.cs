using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EVerland.Migrations.PaymentDb
{
    /// <inheritdoc />
    public partial class RemovePaymentMethodDefault : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "Method",
                table: "Payments",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text",
                oldDefaultValue: "COD");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "Method",
                table: "Payments",
                type: "text",
                nullable: false,
                defaultValue: "COD",
                oldClrType: typeof(string),
                oldType: "text");
        }
    }
}
