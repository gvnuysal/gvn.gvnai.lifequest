using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LifeQuest.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Localization : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "description_en",
                table: "xp_transactions",
                type: "character varying(150)",
                maxLength: 150,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "open_accepted_count",
                table: "weekly_summaries",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "description_en",
                table: "user_quests",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "explanation_en",
                table: "user_quests",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "title_en",
                table: "user_quests",
                type: "character varying(150)",
                maxLength: 150,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "description_en",
                table: "quest_templates",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "title_en",
                table: "quest_templates",
                type: "character varying(150)",
                maxLength: 150,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "quest_title_en",
                table: "quest_parties",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "name_en",
                table: "interests",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "description_en",
                table: "xp_transactions");

            migrationBuilder.DropColumn(
                name: "open_accepted_count",
                table: "weekly_summaries");

            migrationBuilder.DropColumn(
                name: "description_en",
                table: "user_quests");

            migrationBuilder.DropColumn(
                name: "explanation_en",
                table: "user_quests");

            migrationBuilder.DropColumn(
                name: "title_en",
                table: "user_quests");

            migrationBuilder.DropColumn(
                name: "description_en",
                table: "quest_templates");

            migrationBuilder.DropColumn(
                name: "title_en",
                table: "quest_templates");

            migrationBuilder.DropColumn(
                name: "quest_title_en",
                table: "quest_parties");

            migrationBuilder.DropColumn(
                name: "name_en",
                table: "interests");
        }
    }
}
