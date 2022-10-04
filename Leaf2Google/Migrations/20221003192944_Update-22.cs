using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Leaf2Google.Migrations
{
    public partial class Update22 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<byte[]>(
                name: "CredentialId",
                table: "t_leafs_securitykey",
                type: "varbinary(max)",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CredentialId",
                table: "t_leafs_securitykey");
        }
    }
}
