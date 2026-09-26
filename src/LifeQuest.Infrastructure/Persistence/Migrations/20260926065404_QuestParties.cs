using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LifeQuest.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class QuestParties : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "quest_parties",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    template_id = table.Column<Guid>(type: "uuid", nullable: false),
                    quest_title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    category = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    invite_code = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    host_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    expires_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    settled_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<string>(type: "text", nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_quest_parties", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "quest_party_members",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    party_id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    display_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    user_quest_id = table.Column<Guid>(type: "uuid", nullable: false),
                    joined_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    completed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    dropped_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    quest_life_xp = table.Column<int>(type: "integer", nullable: false),
                    bonus_xp = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_quest_party_members", x => x.id);
                    table.ForeignKey(
                        name: "fk_quest_party_members_quest_parties_party_id",
                        column: x => x.party_id,
                        principalTable: "quest_parties",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_quest_party_members_user_accounts_user_id",
                        column: x => x.user_id,
                        principalTable: "user_accounts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_quest_parties_host_user_id",
                table: "quest_parties",
                column: "host_user_id");

            migrationBuilder.CreateIndex(
                name: "ix_quest_parties_invite_code",
                table: "quest_parties",
                column: "invite_code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_quest_party_members_party_id_user_id",
                table: "quest_party_members",
                columns: new[] { "party_id", "user_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_quest_party_members_user_id",
                table: "quest_party_members",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ix_quest_party_members_user_quest_id",
                table: "quest_party_members",
                column: "user_quest_id",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "quest_party_members");

            migrationBuilder.DropTable(
                name: "quest_parties");
        }
    }
}
