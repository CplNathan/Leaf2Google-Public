using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Leaf2Google.Migrations
{
    public partial class Update_5 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "NissanPassword",
                table: "Leafs");

            migrationBuilder.AddColumn<byte[]>(
                name: "NissanPasswordBytes",
                table: "Leafs",
                type: "varbinary(max)",
                nullable: false,
                defaultValue: new byte[0]);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "NissanPasswordBytes",
                table: "Leafs");

            migrationBuilder.AddColumn<string>(
                name: "NissanPassword",
                table: "Leafs",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }
    }
}