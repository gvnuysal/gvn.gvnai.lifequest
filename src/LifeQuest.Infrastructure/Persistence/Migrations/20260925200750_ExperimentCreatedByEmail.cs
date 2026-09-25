using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LifeQuest.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ExperimentCreatedByEmail : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "created_by",
                table: "experiments",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(254)",
                oldMaxLength: 254);

            migrationBuilder.AddColumn<string>(
                name: "created_by_email",
                table: "experiments",
                type: "character varying(254)",
                maxLength: 254,
                nullable: false,
                defaultValue: "");

            // Önceki sürümde e-posta, framework'ün denetim alanını gölgeleyen "created_by" kolonundaydı.
            migrationBuilder.Sql("UPDATE experiments SET created_by_email = created_by WHERE created_by_email = '' AND created_by IS NOT NULL;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "created_by_email",
                table: "experiments");

            migrationBuilder.AlterColumn<string>(
                name: "created_by",
                table: "experiments",
                type: "character varying(254)",
                maxLength: 254,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);
        }
    }
}
