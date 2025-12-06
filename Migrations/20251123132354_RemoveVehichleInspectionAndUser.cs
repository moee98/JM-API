using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace JMAPI.Migrations
{
    /// <inheritdoc />
    public partial class RemoveVehichleInspectionAndUser : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Jobs_Users_CreatedByUserId",
                table: "Jobs");

            migrationBuilder.DropForeignKey(
                name: "FK_VehicleInspection_Users_UserId",
                table: "VehicleInspection");

            migrationBuilder.DropForeignKey(
                name: "FK_VehicleInspection_Vehicles_VehicleId",
                table: "VehicleInspection");

            migrationBuilder.DropIndex(
                name: "IX_VehicleInspection_UserId",
                table: "VehicleInspection");

            migrationBuilder.DropIndex(
                name: "IX_Jobs_CreatedByUserId",
                table: "Jobs");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "VehicleInspection");

            migrationBuilder.DropColumn(
                name: "CreatedByUserId",
                table: "Jobs");

            migrationBuilder.AlterColumn<int>(
                name: "VehicleId",
                table: "VehicleInspection",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_VehicleInspection_Vehicles_VehicleId",
                table: "VehicleInspection",
                column: "VehicleId",
                principalTable: "Vehicles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_VehicleInspection_Vehicles_VehicleId",
                table: "VehicleInspection");

            migrationBuilder.AlterColumn<int>(
                name: "VehicleId",
                table: "VehicleInspection",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AddColumn<int>(
                name: "UserId",
                table: "VehicleInspection",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "CreatedByUserId",
                table: "Jobs",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_VehicleInspection_UserId",
                table: "VehicleInspection",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Jobs_CreatedByUserId",
                table: "Jobs",
                column: "CreatedByUserId");

            migrationBuilder.AddForeignKey(
                name: "FK_Jobs_Users_CreatedByUserId",
                table: "Jobs",
                column: "CreatedByUserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_VehicleInspection_Users_UserId",
                table: "VehicleInspection",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_VehicleInspection_Vehicles_VehicleId",
                table: "VehicleInspection",
                column: "VehicleId",
                principalTable: "Vehicles",
                principalColumn: "Id");
        }
    }
}
