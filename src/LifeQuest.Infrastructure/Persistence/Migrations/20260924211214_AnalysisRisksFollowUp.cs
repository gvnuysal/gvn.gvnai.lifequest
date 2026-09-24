using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LifeQuest.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AnalysisRisksFollowUp : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "effort",
                table: "user_quests",
                type: "character varying(32)",
                maxLength: 32,
                nullable: false,
                defaultValue: "Light");

            migrationBuilder.AddColumn<string>(
                name: "narration_source",
                table: "user_quests",
                type: "character varying(32)",
                maxLength: 32,
                nullable: false,
                defaultValue: "Template");

            migrationBuilder.AddColumn<string>(
                name: "max_physical_effort",
                table: "user_profiles",
                type: "character varying(32)",
                maxLength: 32,
                nullable: false,
                defaultValue: "Vigorous");

            migrationBuilder.AddColumn<string>(
                name: "notification_preference",
                table: "user_profiles",
                type: "character varying(32)",
                maxLength: 32,
                nullable: false,
                defaultValue: "WeeklySummary");

            migrationBuilder.AddColumn<string>(
                name: "effort",
                table: "quest_templates",
                type: "character varying(32)",
                maxLength: 32,
                nullable: false,
                defaultValue: "Light");

            migrationBuilder.AddColumn<bool>(
                name: "is_starter",
                table: "quest_templates",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "weekly_summaries",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    week_start = table.Column<DateOnly>(type: "date", nullable: false),
                    completed_count = table.Column<int>(type: "integer", nullable: false),
                    xp_earned = table.Column<int>(type: "integer", nullable: false),
                    new_categories = table.Column<int[]>(type: "integer[]", nullable: false),
                    top_category = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    title = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    message = table.Column<string>(type: "character varying(400)", maxLength: 400, nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    read_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_weekly_summaries", x => x.id);
                    table.ForeignKey(
                        name: "fk_weekly_summaries_user_accounts_user_id",
                        column: x => x.user_id,
                        principalTable: "user_accounts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_weekly_summaries_user_id_week_start",
                table: "weekly_summaries",
                columns: new[] { "user_id", "week_start" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "weekly_summaries");

            migrationBuilder.DropColumn(
                name: "effort",
                table: "user_quests");

            migrationBuilder.DropColumn(
                name: "narration_source",
                table: "user_quests");

            migrationBuilder.DropColumn(
                name: "max_physical_effort",
                table: "user_profiles");

            migrationBuilder.DropColumn(
                name: "notification_preference",
                table: "user_profiles");

            migrationBuilder.DropColumn(
                name: "effort",
                table: "quest_templates");

            migrationBuilder.DropColumn(
                name: "is_starter",
                table: "quest_templates");
        }
    }
}
