using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Leaf2Google.Migrations
{
    public partial class Update19 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "t_leafs_securitykey",
                columns: table => new
                {
                    KeyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OwnerCarModelId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    credentialId = table.Column<byte[]>(type: "varbinary(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_t_leafs_securitykey", x => x.KeyId);
                    table.ForeignKey(
                        name: "FK_t_leafs_securitykey_t_leafs_leaf_OwnerCarModelId",
                        column: x => x.OwnerCarModelId,
                        principalTable: "t_leafs_leaf",
                        principalColumn: "CarModelId");
                });

            migrationBuilder.CreateIndex(
                name: "IX_t_leafs_securitykey_OwnerCarModelId",
                table: "t_leafs_securitykey",
                column: "OwnerCarModelId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "t_leafs_securitykey");
        }
    }
}
