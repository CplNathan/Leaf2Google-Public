using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Leaf2Google.Migrations
{
    public partial class Update_2 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "test",
                table: "Leafs");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "test",
                table: "Leafs",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }
    }
}