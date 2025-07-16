using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace JMAPI.Migrations
{
    /// <inheritdoc />
    public partial class RemoveVehicleInspectionFromJob : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Jobs_VehicleInspection_VehicleInspectionId",
                table: "Jobs");

            migrationBuilder.DropIndex(
                name: "IX_Jobs_VehicleInspectionId",
                table: "Jobs");

            migrationBuilder.DropColumn(
                name: "VehicleInspectionId",
                table: "Jobs");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "VehicleInspectionId",
                table: "Jobs",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_Jobs_VehicleInspectionId",
                table: "Jobs",
                column: "VehicleInspectionId");

            migrationBuilder.AddForeignKey(
                name: "FK_Jobs_VehicleInspection_VehicleInspectionId",
                table: "Jobs",
                column: "VehicleInspectionId",
                principalTable: "VehicleInspection",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
