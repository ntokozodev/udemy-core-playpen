using System;
using AuthPlaypen.Data.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AuthPlaypen.Data.Migrations;

[DbContext(typeof(AuthPlaypenDbContext))]
[Migration("202607020001_AddEntityAuditHistory")]
public partial class AddEntityAuditHistory : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "entity_audit_history",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                EntityType = table.Column<string>(type: "text", nullable: false),
                EntityId = table.Column<Guid>(type: "uuid", nullable: false),
                Action = table.Column<string>(type: "text", nullable: false),
                ActorDisplayName = table.Column<string>(type: "text", nullable: false),
                ActorEmail = table.Column<string>(type: "text", nullable: true),
                OccurredAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                ChangeSummaryJson = table.Column<string>(type: "text", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_entity_audit_history", x => x.Id);
            });

        migrationBuilder.CreateIndex(
            name: "IX_entity_audit_history_EntityType_EntityId_OccurredAt",
            table: "entity_audit_history",
            columns: new[] { "EntityType", "EntityId", "OccurredAt" });
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "entity_audit_history");
    }
}
