using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EVerland.Migrations
{
    /// <inheritdoc />
    public partial class Init_AuthDb : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "EmailVerificationOtps",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Email = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    OtpCode = table.Column<string>(type: "character varying(6)", maxLength: 6, nullable: false),
                    IsVerified = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    ExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    VerifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    AttemptCount = table.Column<int>(type: "integer", nullable: false, defaultValue: 0)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmailVerificationOtps", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_EmailVerificationOtps_Email",
                table: "EmailVerificationOtps",
                column: "Email");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EmailVerificationOtps");
        }
    }
}
