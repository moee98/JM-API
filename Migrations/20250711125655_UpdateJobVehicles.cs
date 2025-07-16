using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace JMAPI.Migrations
{
    /// <inheritdoc />
    public partial class UpdateJobVehicles : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Jobs_Vehicle_VehicleId",
                table: "Jobs");

            migrationBuilder.DropForeignKey(
                name: "FK_Services_Jobs_JobId",
                table: "Services");

            migrationBuilder.DropIndex(
                name: "IX_Services_JobId",
                table: "Services");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Vehicle",
                table: "Vehicle");

            migrationBuilder.DropColumn(
                name: "JobId",
                table: "Services");

            migrationBuilder.RenameTable(
                name: "Vehicle",
                newName: "Vehicles");

            migrationBuilder.AddColumn<int>(
                name: "VehicleInspectionId",
                table: "Jobs",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddPrimaryKey(
                name: "PK_Vehicles",
                table: "Vehicles",
                column: "Id");

            migrationBuilder.CreateTable(
                name: "JobServices",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    JobId = table.Column<int>(type: "int", nullable: false),
                    ServiceId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_JobServices", x => x.id);
                    table.ForeignKey(
                        name: "FK_JobServices_Jobs_JobId",
                        column: x => x.JobId,
                        principalTable: "Jobs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_JobServices_Services_ServiceId",
                        column: x => x.ServiceId,
                        principalTable: "Services",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "VehicleInspection",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    InspectionDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    InspectionResult = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Comments = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PathToImages = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VehicleInspection", x => x.Id);
                    table.ForeignKey(
                        name: "FK_VehicleInspection_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Jobs_VehicleInspectionId",
                table: "Jobs",
                column: "VehicleInspectionId");

            migrationBuilder.CreateIndex(
                name: "IX_JobServices_JobId",
                table: "JobServices",
                column: "JobId");

            migrationBuilder.CreateIndex(
                name: "IX_JobServices_ServiceId",
                table: "JobServices",
                column: "ServiceId");

            migrationBuilder.CreateIndex(
                name: "IX_VehicleInspection_UserId",
                table: "VehicleInspection",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_Jobs_VehicleInspection_VehicleInspectionId",
                table: "Jobs",
                column: "VehicleInspectionId",
                principalTable: "VehicleInspection",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Jobs_Vehicles_VehicleId",
                table: "Jobs",
                column: "VehicleId",
                principalTable: "Vehicles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Jobs_VehicleInspection_VehicleInspectionId",
                table: "Jobs");

            migrationBuilder.DropForeignKey(
                name: "FK_Jobs_Vehicles_VehicleId",
                table: "Jobs");

            migrationBuilder.DropTable(
                name: "JobServices");

            migrationBuilder.DropTable(
                name: "VehicleInspection");

            migrationBuilder.DropIndex(
                name: "IX_Jobs_VehicleInspectionId",
                table: "Jobs");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Vehicles",
                table: "Vehicles");

            migrationBuilder.DropColumn(
                name: "VehicleInspectionId",
                table: "Jobs");

            migrationBuilder.RenameTable(
                name: "Vehicles",
                newName: "Vehicle");

            migrationBuilder.AddColumn<int>(
                name: "JobId",
                table: "Services",
                type: "int",
                nullable: true);

            migrationBuilder.AddPrimaryKey(
                name: "PK_Vehicle",
                table: "Vehicle",
                column: "Id");

            migrationBuilder.CreateIndex(
                name: "IX_Services_JobId",
                table: "Services",
                column: "JobId");

            migrationBuilder.AddForeignKey(
                name: "FK_Jobs_Vehicle_VehicleId",
                table: "Jobs",
                column: "VehicleId",
                principalTable: "Vehicle",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Services_Jobs_JobId",
                table: "Services",
                column: "JobId",
                principalTable: "Jobs",
                principalColumn: "Id");
        }
    }
}
