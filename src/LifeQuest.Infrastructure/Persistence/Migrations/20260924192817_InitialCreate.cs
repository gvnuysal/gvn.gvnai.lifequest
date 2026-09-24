using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LifeQuest.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "interests",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    code = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    category = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_interests", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "quest_templates",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    code = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    title = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    type = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    difficulty = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    category = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    secondary_category = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    min_minutes = table.Column<int>(type: "integer", nullable: false),
                    max_minutes = table.Column<int>(type: "integer", nullable: false),
                    cost = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    day_parts = table.Column<int>(type: "integer", nullable: false),
                    requires_city = table.Column<bool>(type: "boolean", nullable: false),
                    is_outdoor = table.Column<bool>(type: "boolean", nullable: false),
                    cooldown_days = table.Column<int>(type: "integer", nullable: false),
                    safety = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    risk_score = table.Column<double>(type: "double precision", nullable: false),
                    minimum_age = table.Column<int>(type: "integer", nullable: false),
                    interest_ids = table.Column<List<Guid>>(type: "uuid[]", nullable: false),
                    version = table.Column<int>(type: "integer", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    deleted_by = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<string>(type: "text", nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_quest_templates", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "user_accounts",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    email = table.Column<string>(type: "character varying(254)", maxLength: 254, nullable: false),
                    password_hash = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    display_name = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    birth_year = table.Column<int>(type: "integer", nullable: false),
                    role = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    failed_login_count = table.Column<int>(type: "integer", nullable: false),
                    lockout_ends_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    last_login_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<string>(type: "text", nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_user_accounts", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "interest_relations",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    from_interest_id = table.Column<Guid>(type: "uuid", nullable: false),
                    to_interest_id = table.Column<Guid>(type: "uuid", nullable: false),
                    relation_type = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    weight = table.Column<double>(type: "double precision", nullable: false),
                    confidence = table.Column<double>(type: "double precision", nullable: false),
                    source = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_interest_relations", x => x.id);
                    table.ForeignKey(
                        name: "fk_interest_relations_interests_from_interest_id",
                        column: x => x.from_interest_id,
                        principalTable: "interests",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_interest_relations_interests_to_interest_id",
                        column: x => x.to_interest_id,
                        principalTable: "interests",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "player_progress",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    life_xp = table.Column<int>(type: "integer", nullable: false),
                    life_level = table.Column<int>(type: "integer", nullable: false),
                    total_completed = table.Column<int>(type: "integer", nullable: false),
                    feedback_count = table.Column<int>(type: "integer", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<string>(type: "text", nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_player_progress", x => x.id);
                    table.ForeignKey(
                        name: "fk_player_progress_user_accounts_user_id",
                        column: x => x.user_id,
                        principalTable: "user_accounts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "refresh_tokens",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    token_hash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    family_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    expires_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    revoked_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    revoke_reason = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    replaced_by_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_refresh_tokens", x => x.id);
                    table.ForeignKey(
                        name: "fk_refresh_tokens_user_accounts_user_id",
                        column: x => x.user_id,
                        principalTable: "user_accounts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "user_profiles",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    onboarding_completed = table.Column<bool>(type: "boolean", nullable: false),
                    discovery_radius = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    budget = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    weekly_available_minutes = table.Column<int>(type: "integer", nullable: false),
                    goals = table.Column<int[]>(type: "integer[]", nullable: false),
                    city = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
                    time_zone_id = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<string>(type: "text", nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_user_profiles", x => x.id);
                    table.ForeignKey(
                        name: "fk_user_profiles_user_accounts_user_id",
                        column: x => x.user_id,
                        principalTable: "user_accounts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "user_quests",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    template_id = table.Column<Guid>(type: "uuid", nullable: false),
                    template_code = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    template_version = table.Column<int>(type: "integer", nullable: false),
                    title = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    type = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    difficulty = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    category = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    secondary_category = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    min_minutes = table.Column<int>(type: "integer", nullable: false),
                    max_minutes = table.Column<int>(type: "integer", nullable: false),
                    cost = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    interest_ids = table.Column<List<Guid>>(type: "uuid[]", nullable: false),
                    source = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    offer_date = table.Column<DateOnly>(type: "date", nullable: false),
                    slot = table.Column<int>(type: "integer", nullable: false),
                    is_exploration = table.Column<bool>(type: "boolean", nullable: false),
                    reason_codes = table.Column<List<string>>(type: "text[]", nullable: false),
                    explanation = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    offered_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    expires_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    accepted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    completed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    skipped_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    expired_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    skip_reason = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    rating = table.Column<int>(type: "integer", nullable: true),
                    preference = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    feedback_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    reward_life_xp = table.Column<int>(type: "integer", nullable: false),
                    reward_novelty_multiplier = table.Column<decimal>(type: "numeric(4,2)", precision: 4, scale: 2, nullable: false),
                    reward_primary_category_xp = table.Column<int>(type: "integer", nullable: false),
                    reward_secondary_category_xp = table.Column<int>(type: "integer", nullable: false),
                    score_context = table.Column<double>(type: "double precision", nullable: false),
                    score_diversity = table.Column<double>(type: "double precision", nullable: false),
                    score_feedback_fit = table.Column<double>(type: "double precision", nullable: false),
                    score_friction = table.Column<double>(type: "double precision", nullable: false),
                    score_goal_fit = table.Column<double>(type: "double precision", nullable: false),
                    score_interest = table.Column<double>(type: "double precision", nullable: false),
                    score_novelty = table.Column<double>(type: "double precision", nullable: false),
                    score_repetition = table.Column<double>(type: "double precision", nullable: false),
                    score_risk = table.Column<double>(type: "double precision", nullable: false),
                    score_total = table.Column<double>(type: "double precision", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<string>(type: "text", nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_user_quests", x => x.id);
                    table.ForeignKey(
                        name: "fk_user_quests_user_accounts_user_id",
                        column: x => x.user_id,
                        principalTable: "user_accounts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "xp_transactions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    source_type = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    source_id = table.Column<Guid>(type: "uuid", nullable: false),
                    description = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    life_xp = table.Column<int>(type: "integer", nullable: false),
                    primary_category = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    primary_category_xp = table.Column<int>(type: "integer", nullable: false),
                    secondary_category = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    secondary_category_xp = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_xp_transactions", x => x.id);
                    table.ForeignKey(
                        name: "fk_xp_transactions_user_accounts_user_id",
                        column: x => x.user_id,
                        principalTable: "user_accounts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "category_progress",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    player_progress_id = table.Column<Guid>(type: "uuid", nullable: false),
                    category = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    xp = table.Column<int>(type: "integer", nullable: false),
                    level = table.Column<int>(type: "integer", nullable: false),
                    completed_count = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_category_progress", x => x.id);
                    table.ForeignKey(
                        name: "fk_category_progress_player_progress_player_progress_id",
                        column: x => x.player_progress_id,
                        principalTable: "player_progress",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "user_achievements",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    player_progress_id = table.Column<Guid>(type: "uuid", nullable: false),
                    code = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    unlocked_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_user_achievements", x => x.id);
                    table.ForeignKey(
                        name: "fk_user_achievements_player_progress_player_progress_id",
                        column: x => x.player_progress_id,
                        principalTable: "player_progress",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "user_interests",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_profile_id = table.Column<Guid>(type: "uuid", nullable: false),
                    interest_id = table.Column<Guid>(type: "uuid", nullable: false),
                    weight = table.Column<double>(type: "double precision", nullable: false),
                    source = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_user_interests", x => x.id);
                    table.ForeignKey(
                        name: "fk_user_interests_interests_interest_id",
                        column: x => x.interest_id,
                        principalTable: "interests",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_user_interests_user_profiles_user_profile_id",
                        column: x => x.user_profile_id,
                        principalTable: "user_profiles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_category_progress_player_progress_id_category",
                table: "category_progress",
                columns: new[] { "player_progress_id", "category" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_interest_relations_from_interest_id_to_interest_id",
                table: "interest_relations",
                columns: new[] { "from_interest_id", "to_interest_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_interest_relations_to_interest_id",
                table: "interest_relations",
                column: "to_interest_id");

            migrationBuilder.CreateIndex(
                name: "ix_interests_code",
                table: "interests",
                column: "code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_player_progress_user_id",
                table: "player_progress",
                column: "user_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_quest_templates_code",
                table: "quest_templates",
                column: "code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_quest_templates_is_active_safety",
                table: "quest_templates",
                columns: new[] { "is_active", "safety" });

            migrationBuilder.CreateIndex(
                name: "ix_refresh_tokens_family_id",
                table: "refresh_tokens",
                column: "family_id");

            migrationBuilder.CreateIndex(
                name: "ix_refresh_tokens_token_hash",
                table: "refresh_tokens",
                column: "token_hash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_refresh_tokens_user_id",
                table: "refresh_tokens",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ix_user_accounts_email",
                table: "user_accounts",
                column: "email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_user_achievements_player_progress_id_code",
                table: "user_achievements",
                columns: new[] { "player_progress_id", "code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_user_interests_interest_id",
                table: "user_interests",
                column: "interest_id");

            migrationBuilder.CreateIndex(
                name: "ix_user_interests_user_profile_id_interest_id",
                table: "user_interests",
                columns: new[] { "user_profile_id", "interest_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_user_profiles_user_id",
                table: "user_profiles",
                column: "user_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_user_quests_status_expires_at",
                table: "user_quests",
                columns: new[] { "status", "expires_at" });

            migrationBuilder.CreateIndex(
                name: "ix_user_quests_template_id",
                table: "user_quests",
                column: "template_id");

            migrationBuilder.CreateIndex(
                name: "ix_user_quests_user_id_offer_date_source",
                table: "user_quests",
                columns: new[] { "user_id", "offer_date", "source" });

            migrationBuilder.CreateIndex(
                name: "ix_user_quests_user_id_status_expires_at",
                table: "user_quests",
                columns: new[] { "user_id", "status", "expires_at" });

            migrationBuilder.CreateIndex(
                name: "ux_user_quests_daily_slot",
                table: "user_quests",
                columns: new[] { "user_id", "offer_date", "slot" },
                unique: true,
                filter: "source = 'Daily'");

            migrationBuilder.CreateIndex(
                name: "ix_xp_transactions_source_type_source_id",
                table: "xp_transactions",
                columns: new[] { "source_type", "source_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_xp_transactions_user_id_created_at",
                table: "xp_transactions",
                columns: new[] { "user_id", "created_at" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "category_progress");

            migrationBuilder.DropTable(
                name: "interest_relations");

            migrationBuilder.DropTable(
                name: "quest_templates");

            migrationBuilder.DropTable(
                name: "refresh_tokens");

            migrationBuilder.DropTable(
                name: "user_achievements");

            migrationBuilder.DropTable(
                name: "user_interests");

            migrationBuilder.DropTable(
                name: "user_quests");

            migrationBuilder.DropTable(
                name: "xp_transactions");

            migrationBuilder.DropTable(
                name: "player_progress");

            migrationBuilder.DropTable(
                name: "interests");

            migrationBuilder.DropTable(
                name: "user_profiles");

            migrationBuilder.DropTable(
                name: "user_accounts");
        }
    }
}
