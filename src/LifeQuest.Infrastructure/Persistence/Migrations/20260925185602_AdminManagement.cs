using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LifeQuest.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AdminManagement : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "suspended_at",
                table: "user_accounts",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "suspended_until",
                table: "user_accounts",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "suspension_reason",
                table: "user_accounts",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "source",
                table: "quest_templates",
                type: "character varying(32)",
                maxLength: 32,
                nullable: false,
                defaultValue: "Seed");

            migrationBuilder.CreateTable(
                name: "admin_audit_entries",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    actor_id = table.Column<Guid>(type: "uuid", nullable: false),
                    actor_email = table.Column<string>(type: "character varying(254)", maxLength: 254, nullable: false),
                    action = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    target_type = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    target_id = table.Column<Guid>(type: "uuid", nullable: true),
                    target_label = table.Column<string>(type: "character varying(254)", maxLength: 254, nullable: false),
                    reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    details = table.Column<string>(type: "jsonb", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_admin_audit_entries", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "recommendation_settings",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    overrides = table.Column<string>(type: "jsonb", nullable: false),
                    revision = table.Column<int>(type: "integer", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<string>(type: "character varying(254)", maxLength: 254, nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_recommendation_settings", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_admin_audit_entries_action_created_at",
                table: "admin_audit_entries",
                columns: new[] { "action", "created_at" });

            migrationBuilder.CreateIndex(
                name: "ix_admin_audit_entries_created_at",
                table: "admin_audit_entries",
                column: "created_at",
                descending: new bool[0]);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "admin_audit_entries");

            migrationBuilder.DropTable(
                name: "recommendation_settings");

            migrationBuilder.DropColumn(
                name: "suspended_at",
                table: "user_accounts");

            migrationBuilder.DropColumn(
                name: "suspended_until",
                table: "user_accounts");

            migrationBuilder.DropColumn(
                name: "suspension_reason",
                table: "user_accounts");

            migrationBuilder.DropColumn(
                name: "source",
                table: "quest_templates");
        }
    }
}
