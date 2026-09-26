using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LifeQuest.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class RealWorldContext : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "local_places",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    kind = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    city = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    city_key = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    name = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    address = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    url = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    note = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    starts_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ends_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    template_ids = table.Column<List<Guid>>(type: "uuid[]", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    created_by_email = table.Column<string>(type: "character varying(254)", maxLength: 254, nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<string>(type: "text", nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_local_places", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_local_places_city_key_kind_ends_at",
                table: "local_places",
                columns: new[] { "city_key", "kind", "ends_at" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "local_places");
        }
    }
}
