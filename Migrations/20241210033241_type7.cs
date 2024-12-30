using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WebApplication1.Migrations
{
    /// <inheritdoc />
    public partial class type7 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "ChatId",
                table: "TelegramUsers",
                type: "nvarchar(450)",
                nullable: false,
                oldClrType: typeof(long),
                oldType: "bigint");

            migrationBuilder.AlterColumn<string>(
                name: "UserId",
                table: "ScheduledMessages",
                type: "nvarchar(450)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AddUniqueConstraint(
                name: "AK_TelegramUsers_ChatId",
                table: "TelegramUsers",
                column: "ChatId");

            migrationBuilder.CreateIndex(
                name: "IX_ScheduledMessages_UserId",
                table: "ScheduledMessages",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_ScheduledMessages_TelegramUsers_UserId",
                table: "ScheduledMessages",
                column: "UserId",
                principalTable: "TelegramUsers",
                principalColumn: "ChatId",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ScheduledMessages_TelegramUsers_UserId",
                table: "ScheduledMessages");

            migrationBuilder.DropUniqueConstraint(
                name: "AK_TelegramUsers_ChatId",
                table: "TelegramUsers");

            migrationBuilder.DropIndex(
                name: "IX_ScheduledMessages_UserId",
                table: "ScheduledMessages");

            migrationBuilder.AlterColumn<long>(
                name: "ChatId",
                table: "TelegramUsers",
                type: "bigint",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");

            migrationBuilder.AlterColumn<string>(
                name: "UserId",
                table: "ScheduledMessages",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");
        }
    }
}
