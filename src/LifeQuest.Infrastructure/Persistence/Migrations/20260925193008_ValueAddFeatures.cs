using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LifeQuest.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ValueAddFeatures : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "experiment_id",
                table: "user_quests",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "experiment_variant",
                table: "user_quests",
                type: "character varying(32)",
                maxLength: 32,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "planned_at",
                table: "user_quests",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "experiments",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    hypothesis = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    treatment_overrides = table.Column<string>(type: "jsonb", nullable: false),
                    treatment_share = table.Column<double>(type: "double precision", nullable: false),
                    started_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ended_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    outcome = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    created_by = table.Column<string>(type: "character varying(254)", maxLength: 254, nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_experiments", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "quest_ideas",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    title = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    description = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    category = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    minutes = table.Column<int>(type: "integer", nullable: false),
                    cost = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    is_outdoor = table.Column<bool>(type: "boolean", nullable: false),
                    status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    flags = table.Column<List<string>>(type: "text[]", nullable: false),
                    review_note = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    reviewed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    reviewed_by = table.Column<string>(type: "character varying(254)", maxLength: 254, nullable: true),
                    template_id = table.Column<Guid>(type: "uuid", nullable: true),
                    submitted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<string>(type: "text", nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_quest_ideas", x => x.id);
                    table.ForeignKey(
                        name: "fk_quest_ideas_user_accounts_user_id",
                        column: x => x.user_id,
                        principalTable: "user_accounts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "saved_quests",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    template_id = table.Column<Guid>(type: "uuid", nullable: false),
                    saved_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_saved_quests", x => x.id);
                    table.ForeignKey(
                        name: "fk_saved_quests_user_accounts_user_id",
                        column: x => x.user_id,
                        principalTable: "user_accounts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_user_quests_experiment_id_experiment_variant",
                table: "user_quests",
                columns: new[] { "experiment_id", "experiment_variant" });

            migrationBuilder.CreateIndex(
                name: "ux_experiments_single_running",
                table: "experiments",
                column: "status",
                unique: true,
                filter: "status = 'Running'");

            migrationBuilder.CreateIndex(
                name: "ix_quest_ideas_status_submitted_at",
                table: "quest_ideas",
                columns: new[] { "status", "submitted_at" });

            migrationBuilder.CreateIndex(
                name: "ix_quest_ideas_user_id_submitted_at",
                table: "quest_ideas",
                columns: new[] { "user_id", "submitted_at" });

            migrationBuilder.CreateIndex(
                name: "ix_saved_quests_user_id_template_id",
                table: "saved_quests",
                columns: new[] { "user_id", "template_id" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "experiments");

            migrationBuilder.DropTable(
                name: "quest_ideas");

            migrationBuilder.DropTable(
                name: "saved_quests");

            migrationBuilder.DropIndex(
                name: "ix_user_quests_experiment_id_experiment_variant",
                table: "user_quests");

            migrationBuilder.DropColumn(
                name: "experiment_id",
                table: "user_quests");

            migrationBuilder.DropColumn(
                name: "experiment_variant",
                table: "user_quests");

            migrationBuilder.DropColumn(
                name: "planned_at",
                table: "user_quests");
        }
    }
}
