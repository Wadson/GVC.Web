using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GVC.Web.Data.Migrations;

/// <summary>
/// Registra o modelo atual como baseline para um banco já existente.
/// As tabelas operacionais anteriores à adoção de migrations não são recriadas.
/// </summary>
public partial class BaselineCurrentSchema : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "DataVersion",
            columns: table => new
            {
                Version = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                AppliedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_DataVersion", x => x.Version);
            });
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "DataVersion");
    }
}
