using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EVerland.Migrations.ProductDb
{
    /// <inheritdoc />
    public partial class ChangeUniqueSkuCodeToSkuId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Skus_SkuCode",
                table: "Skus");

            migrationBuilder.CreateIndex(
                name: "IX_Skus_Id",
                table: "Skus",
                column: "Id",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Skus_Id",
                table: "Skus");

            migrationBuilder.CreateIndex(
                name: "IX_Skus_SkuCode",
                table: "Skus",
                column: "SkuCode",
                unique: true);
        }
    }
}
