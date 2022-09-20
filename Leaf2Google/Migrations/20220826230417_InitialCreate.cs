using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Leaf2Google.Migrations
{
    public partial class InitialCreate : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Leafs",
                columns: table => new
                {
                    LeafId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    NissanUsername = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    NissanPassword = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Leafs", x => x.LeafId);
                });

            migrationBuilder.CreateTable(
                name: "Audits",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AuditOwnerLeafId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Context = table.Column<int>(type: "int", nullable: false),
                    Action = table.Column<int>(type: "int", nullable: false),
                    Data = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Audits", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Audits_Leafs_AuditOwnerLeafId",
                        column: x => x.AuditOwnerLeafId,
                        principalTable: "Leafs",
                        principalColumn: "LeafId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Audits_AuditOwnerLeafId",
                table: "Audits",
                column: "AuditOwnerLeafId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Audits");

            migrationBuilder.DropTable(
                name: "Leafs");
        }
    }
}