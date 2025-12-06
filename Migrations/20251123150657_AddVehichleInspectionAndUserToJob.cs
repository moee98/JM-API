using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace JMAPI.Migrations
{
    /// <inheritdoc />
    public partial class AddVehichleInspectionAndUserToJob : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AppUserId",
                table: "Jobs",
                type: "nvarchar(450)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "VehicleInspectionId",
                table: "Jobs",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Jobs_AppUserId",
                table: "Jobs",
                column: "AppUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Jobs_VehicleInspectionId",
                table: "Jobs",
                column: "VehicleInspectionId");

            migrationBuilder.AddForeignKey(
                name: "FK_Jobs_AspNetUsers_AppUserId",
                table: "Jobs",
                column: "AppUserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Jobs_VehicleInspection_VehicleInspectionId",
                table: "Jobs",
                column: "VehicleInspectionId",
                principalTable: "VehicleInspection",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Jobs_AspNetUsers_AppUserId",
                table: "Jobs");

            migrationBuilder.DropForeignKey(
                name: "FK_Jobs_VehicleInspection_VehicleInspectionId",
                table: "Jobs");

            migrationBuilder.DropIndex(
                name: "IX_Jobs_AppUserId",
                table: "Jobs");

            migrationBuilder.DropIndex(
                name: "IX_Jobs_VehicleInspectionId",
                table: "Jobs");

            migrationBuilder.DropColumn(
                name: "AppUserId",
                table: "Jobs");

            migrationBuilder.DropColumn(
                name: "VehicleInspectionId",
                table: "Jobs");
        }
    }
}
