using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Leaf2Google.Migrations
{
    public partial class Update_14 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "Deleted",
                table: "t_auths_auth",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastExecute",
                table: "t_auths_auth",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastQuery",
                table: "t_auths_auth",
                type: "datetime2",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Deleted",
                table: "t_auths_auth");

            migrationBuilder.DropColumn(
                name: "LastExecute",
                table: "t_auths_auth");

            migrationBuilder.DropColumn(
                name: "LastQuery",
                table: "t_auths_auth");
        }
    }
}