using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Backend.Migrations
{
    /// <inheritdoc />
    public partial class AddOwnerToServiceName : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "OwnerId",
                table: "ServiceNames",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateIndex(
                name: "IX_ServiceNames_OwnerId",
                table: "ServiceNames",
                column: "OwnerId");

            migrationBuilder.AddForeignKey(
                name: "FK_ServiceNames_BarbershopOwners_OwnerId",
                table: "ServiceNames",
                column: "OwnerId",
                principalTable: "BarbershopOwners",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ServiceNames_BarbershopOwners_OwnerId",
                table: "ServiceNames");

            migrationBuilder.DropIndex(
                name: "IX_ServiceNames_OwnerId",
                table: "ServiceNames");

            migrationBuilder.DropColumn(
                name: "OwnerId",
                table: "ServiceNames");
        }
    }
}
