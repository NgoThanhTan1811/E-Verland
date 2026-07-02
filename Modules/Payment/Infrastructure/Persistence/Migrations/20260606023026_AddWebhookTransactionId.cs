using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EVerland.Migrations.PaymentDb
{
    public partial class AddWebhookTransactionId : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "TransactionId",
                table: "WebhookEvents",
                type: "character varying(128)",
                maxLength: 128,
                nullable: true);

            migrationBuilder.Sql(@"UPDATE ""WebhookEvents""
SET ""TransactionId"" = COALESCE(""TransactionId"", ""IdempotencyKey"")
WHERE ""TransactionId"" IS NULL;");

            migrationBuilder.Sql(@"DELETE FROM ""WebhookEvents""
WHERE ""TransactionId"" IS NULL;");

            migrationBuilder.AlterColumn<string>(
                name: "TransactionId",
                table: "WebhookEvents",
                type: "character varying(128)",
                maxLength: 128,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(128)",
                oldMaxLength: 128,
                oldNullable: true);

            migrationBuilder.DropIndex(
                name: "IX_WebhookEvents_IdempotencyKey",
                table: "WebhookEvents");

            migrationBuilder.CreateIndex(
                name: "IX_WebhookEvents_TransactionId",
                table: "WebhookEvents",
                column: "TransactionId",
                unique: true);

            migrationBuilder.DropColumn(
                name: "IdempotencyKey",
                table: "WebhookEvents");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "IdempotencyKey",
                table: "WebhookEvents",
                type: "character varying(128)",
                maxLength: 128,
                nullable: true);

            migrationBuilder.Sql(@"UPDATE ""WebhookEvents""
SET ""IdempotencyKey"" = COALESCE(""IdempotencyKey"", ""TransactionId"")
WHERE ""IdempotencyKey"" IS NULL;");

            migrationBuilder.Sql(@"DELETE FROM ""WebhookEvents""
WHERE ""IdempotencyKey"" IS NULL;");

            migrationBuilder.AlterColumn<string>(
                name: "IdempotencyKey",
                table: "WebhookEvents",
                type: "character varying(128)",
                maxLength: 128,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(128)",
                oldMaxLength: 128,
                oldNullable: true);

            migrationBuilder.DropIndex(
                name: "IX_WebhookEvents_TransactionId",
                table: "WebhookEvents");

            migrationBuilder.CreateIndex(
                name: "IX_WebhookEvents_IdempotencyKey",
                table: "WebhookEvents",
                column: "IdempotencyKey",
                unique: true);

            migrationBuilder.DropColumn(
                name: "TransactionId",
                table: "WebhookEvents");
        }
    }
}
