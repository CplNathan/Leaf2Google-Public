using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Leaf2Google.Migrations
{
    public partial class Update24 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IV",
                table: "t_leafs_leaf");

            migrationBuilder.DropColumn(
                name: "Key",
                table: "t_leafs_leaf");

            migrationBuilder.DropColumn(
                name: "NissanPasswordBytes",
                table: "t_leafs_leaf");

            migrationBuilder.AddColumn<string>(
                name: "NissanPassword",
                table: "t_leafs_leaf",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "NissanPassword",
                table: "t_leafs_leaf");

            migrationBuilder.AddColumn<byte[]>(
                name: "IV",
                table: "t_leafs_leaf",
                type: "varbinary(max)",
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<byte[]>(
                name: "Key",
                table: "t_leafs_leaf",
                type: "varbinary(max)",
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<byte[]>(
                name: "NissanPasswordBytes",
                table: "t_leafs_leaf",
                type: "varbinary(max)",
                nullable: false,
                defaultValue: new byte[0]);
        }
    }
}
