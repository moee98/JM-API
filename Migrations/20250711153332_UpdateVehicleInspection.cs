using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace JMAPI.Migrations
{
    /// <inheritdoc />
    public partial class UpdateVehicleInspection : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "VehicleId",
                table: "VehicleInspection",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_VehicleInspection_VehicleId",
                table: "VehicleInspection",
                column: "VehicleId");

            migrationBuilder.AddForeignKey(
                name: "FK_VehicleInspection_Vehicles_VehicleId",
                table: "VehicleInspection",
                column: "VehicleId",
                principalTable: "Vehicles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_VehicleInspection_Vehicles_VehicleId",
                table: "VehicleInspection");

            migrationBuilder.DropIndex(
                name: "IX_VehicleInspection_VehicleId",
                table: "VehicleInspection");

            migrationBuilder.DropColumn(
                name: "VehicleId",
                table: "VehicleInspection");
        }
    }
}
