using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace JMAPI.Migrations
{
    /// <inheritdoc />
    public partial class UpdatePaymentNethodsToAddAmount : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PaymentMethods_Jobs_JobId",
                table: "PaymentMethods");

            migrationBuilder.AlterColumn<int>(
                name: "JobId",
                table: "PaymentMethods",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AddColumn<long>(
                name: "Amount",
                table: "PaymentMethods",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddForeignKey(
                name: "FK_PaymentMethods_Jobs_JobId",
                table: "PaymentMethods",
                column: "JobId",
                principalTable: "Jobs",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PaymentMethods_Jobs_JobId",
                table: "PaymentMethods");

            migrationBuilder.DropColumn(
                name: "Amount",
                table: "PaymentMethods");

            migrationBuilder.AlterColumn<int>(
                name: "JobId",
                table: "PaymentMethods",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AddForeignKey(
                name: "FK_PaymentMethods_Jobs_JobId",
                table: "PaymentMethods",
                column: "JobId",
                principalTable: "Jobs",
                principalColumn: "Id");
        }
    }
}
