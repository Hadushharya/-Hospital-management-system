using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HospitalManagementSystem.Migrations
{
    /// <inheritdoc />
    public partial class AddNewFieldPrescription : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "BasedOnLabRequestId",
                table: "Prescriptions",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Prescriptions_BasedOnLabRequestId",
                table: "Prescriptions",
                column: "BasedOnLabRequestId");

            migrationBuilder.AddForeignKey(
                name: "FK_Prescriptions_LabRequests_BasedOnLabRequestId",
                table: "Prescriptions",
                column: "BasedOnLabRequestId",
                principalTable: "LabRequests",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Prescriptions_LabRequests_BasedOnLabRequestId",
                table: "Prescriptions");

            migrationBuilder.DropIndex(
                name: "IX_Prescriptions_BasedOnLabRequestId",
                table: "Prescriptions");

            migrationBuilder.DropColumn(
                name: "BasedOnLabRequestId",
                table: "Prescriptions");
        }
    }
}
