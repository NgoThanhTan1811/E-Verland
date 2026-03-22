using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EVerland.Migrations.Payment
{
    /// <inheritdoc />
    public partial class AddPaymentUrl : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PaymentUrl",
                table: "Payments",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PaymentUrl",
                table: "Payments");
        }
    }
}
