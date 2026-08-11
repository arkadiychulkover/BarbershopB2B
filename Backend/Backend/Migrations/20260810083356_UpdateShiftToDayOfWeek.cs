using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Backend.Migrations
{
    /// <inheritdoc />
    public partial class UpdateShiftToDayOfWeek : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Date",
                table: "Shifts");

            migrationBuilder.AddColumn<int>(
                name: "DayOfWeek",
                table: "Shifts",
                type: "integer",
                nullable: false,
                defaultValue: 0);

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

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Appointments_Reviews_ReviewId1",
                table: "Appointments");

            migrationBuilder.DropIndex(
                name: "IX_Appointments_ReviewId1",
                table: "Appointments");

            migrationBuilder.DropColumn(
                name: "DayOfWeek",
                table: "Shifts");

            migrationBuilder.DropColumn(
                name: "ReviewId",
                table: "Appointments");

            migrationBuilder.DropColumn(
                name: "ReviewId1",
                table: "Appointments");

            migrationBuilder.AddColumn<DateOnly>(
                name: "Date",
                table: "Shifts",
                type: "date",
                nullable: false,
                defaultValue: new DateOnly(1, 1, 1));
        }
    }
}
