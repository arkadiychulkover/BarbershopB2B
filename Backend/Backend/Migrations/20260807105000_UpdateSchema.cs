using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Backend.Migrations
{    public partial class UpdateSchema : Migration
    {        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PasswordHash",
                table: "BarbershopOwners",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "WalletAddress",
                table: "BarbershopOwners",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "Tranxactions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OwnerId = table.Column<Guid>(type: "uuid", nullable: false),
                    Time = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Amount = table.Column<decimal>(type: "numeric", nullable: false),
                    TxhHash = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Tranxactions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Tranxactions_BarbershopOwners_OwnerId",
                        column: x => x.OwnerId,
                        principalTable: "BarbershopOwners",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Tranxactions_OwnerId",
                table: "Tranxactions",
                column: "OwnerId");
        }        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Tranxactions");

            migrationBuilder.DropColumn(
                name: "PasswordHash",
                table: "BarbershopOwners");

            migrationBuilder.DropColumn(
                name: "WalletAddress",
                table: "BarbershopOwners");
        }
    }
}
