using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Backend.Migrations
{
    /// <inheritdoc />
    public partial class FixReviewMapping : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Appointments_Reviews_ReviewId1",
                table: "Appointments");

            migrationBuilder.DropIndex(
                name: "IX_Reviews_AppointmentId",
                table: "Reviews");

            migrationBuilder.DropIndex(
                name: "IX_Appointments_ReviewId1",
                table: "Appointments");

            migrationBuilder.DropColumn(
                name: "ReviewId",
                table: "Appointments");

            migrationBuilder.DropColumn(
                name: "ReviewId1",
                table: "Appointments");

            migrationBuilder.CreateIndex(
                name: "IX_Reviews_AppointmentId",
                table: "Reviews",
                column: "AppointmentId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Reviews_AppointmentId",
                table: "Reviews");

            migrationBuilder.AddColumn<Guid>(
                name: "ReviewId",
                table: "Appointments",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "ReviewId1",
                table: "Appointments",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Reviews_AppointmentId",
                table: "Reviews",
                column: "AppointmentId");

            migrationBuilder.CreateIndex(
                name: "IX_Appointments_ReviewId1",
                table: "Appointments",
                column: "ReviewId1");

            migrationBuilder.AddForeignKey(
                name: "FK_Appointments_Reviews_ReviewId1",
                table: "Appointments",
                column: "ReviewId1",
                principalTable: "Reviews",
                principalColumn: "Id");
        }
    }
}
