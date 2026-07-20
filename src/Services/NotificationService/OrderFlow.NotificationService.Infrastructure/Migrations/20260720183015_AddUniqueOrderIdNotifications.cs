using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OrderFlow.NotificationService.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddUniqueOrderIdNotifications : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_Notifications_OrderId",
                table: "Notifications",
                column: "OrderId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Notifications_OrderId",
                table: "Notifications");
        }
    }
}
