using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace JMAPI.Migrations
{
    /// <inheritdoc />
    public partial class AddAttachmentStorageForExpenseItemsAndVehicleInspections : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ExpenseItemAttachments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ExpenseItemId = table.Column<int>(type: "int", nullable: false),
                    FileName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ContentType = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    FileSize = table.Column<long>(type: "bigint", nullable: false),
                    Data = table.Column<byte[]>(type: "varbinary(max)", nullable: false),
                    UploadedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExpenseItemAttachments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ExpenseItemAttachments_ExpenseItems_ExpenseItemId",
                        column: x => x.ExpenseItemId,
                        principalTable: "ExpenseItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "VehicleInspectionAttachments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    VehicleInspectionId = table.Column<int>(type: "int", nullable: false),
                    FileName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ContentType = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    FileSize = table.Column<long>(type: "bigint", nullable: false),
                    Data = table.Column<byte[]>(type: "varbinary(max)", nullable: false),
                    UploadedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VehicleInspectionAttachments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_VehicleInspectionAttachments_VehicleInspection_VehicleInspectionId",
                        column: x => x.VehicleInspectionId,
                        principalTable: "VehicleInspection",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ExpenseItemAttachments_ExpenseItemId",
                table: "ExpenseItemAttachments",
                column: "ExpenseItemId");

            migrationBuilder.CreateIndex(
                name: "IX_VehicleInspectionAttachments_VehicleInspectionId",
                table: "VehicleInspectionAttachments",
                column: "VehicleInspectionId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ExpenseItemAttachments");

            migrationBuilder.DropTable(
                name: "VehicleInspectionAttachments");
        }
    }
}
