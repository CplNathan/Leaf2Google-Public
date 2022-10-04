using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Leaf2Google.Migrations
{
    public partial class Update23 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_t_leafs_securitykey",
                table: "t_leafs_securitykey");

            migrationBuilder.AlterColumn<byte[]>(
                name: "PublicKey",
                table: "t_leafs_securitykey",
                type: "varbinary(900)",
                nullable: false,
                defaultValue: new byte[0],
                oldClrType: typeof(byte[]),
                oldType: "varbinary(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<byte[]>(
                name: "UserId",
                table: "t_leafs_securitykey",
                type: "varbinary(max)",
                nullable: true,
                oldClrType: typeof(byte[]),
                oldType: "varbinary(900)");

            migrationBuilder.AddPrimaryKey(
                name: "PK_t_leafs_securitykey",
                table: "t_leafs_securitykey",
                column: "PublicKey");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_t_leafs_securitykey",
                table: "t_leafs_securitykey");

            migrationBuilder.AlterColumn<byte[]>(
                name: "UserId",
                table: "t_leafs_securitykey",
                type: "varbinary(900)",
                nullable: false,
                defaultValue: new byte[0],
                oldClrType: typeof(byte[]),
                oldType: "varbinary(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<byte[]>(
                name: "PublicKey",
                table: "t_leafs_securitykey",
                type: "varbinary(max)",
                nullable: true,
                oldClrType: typeof(byte[]),
                oldType: "varbinary(900)");

            migrationBuilder.AddPrimaryKey(
                name: "PK_t_leafs_securitykey",
                table: "t_leafs_securitykey",
                column: "UserId");
        }
    }
}
