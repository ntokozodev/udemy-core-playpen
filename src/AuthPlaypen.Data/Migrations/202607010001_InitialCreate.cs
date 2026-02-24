using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AuthPlaypen.Data.Migrations;

public partial class InitialCreate : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "applications",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                DisplayName = table.Column<string>(type: "text", nullable: false),
                ClientId = table.Column<string>(type: "text", nullable: false),
                ClientSecret = table.Column<string>(type: "text", nullable: false),
                Flow = table.Column<string>(type: "text", nullable: false),
                PostLogoutRedirectUris = table.Column<string>(type: "text", nullable: true),
                RedirectUris = table.Column<string>(type: "text", nullable: true),
                CreatedBy = table.Column<string>(type: "text", nullable: false, defaultValue: "Unknown"),
                CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                UpdatedBy = table.Column<string>(type: "text", nullable: false, defaultValue: "Unknown"),
                UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()")
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_applications", x => x.Id);
            });

        migrationBuilder.CreateTable(
            name: "scopes",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                DisplayName = table.Column<string>(type: "text", nullable: false),
                ScopeName = table.Column<string>(type: "text", nullable: false),
                Description = table.Column<string>(type: "text", nullable: false),
                CreatedBy = table.Column<string>(type: "text", nullable: false, defaultValue: "Unknown"),
                CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                UpdatedBy = table.Column<string>(type: "text", nullable: false, defaultValue: "Unknown"),
                UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()")
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_scopes", x => x.Id);
            });

        migrationBuilder.CreateTable(
            name: "application_scopes",
            columns: table => new
            {
                ApplicationId = table.Column<Guid>(type: "uuid", nullable: false),
                ScopeId = table.Column<Guid>(type: "uuid", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_application_scopes", x => new { x.ApplicationId, x.ScopeId });
                table.ForeignKey(
                    name: "FK_application_scopes_applications_ApplicationId",
                    column: x => x.ApplicationId,
                    principalTable: "applications",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "FK_application_scopes_scopes_ScopeId",
                    column: x => x.ScopeId,
                    principalTable: "scopes",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "IX_application_scopes_ScopeId",
            table: "application_scopes",
            column: "ScopeId");

        migrationBuilder.CreateIndex(
            name: "IX_applications_ClientId",
            table: "applications",
            column: "ClientId",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_scopes_ScopeName",
            table: "scopes",
            column: "ScopeName",
            unique: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "application_scopes");

        migrationBuilder.DropTable(
            name: "applications");

        migrationBuilder.DropTable(
            name: "scopes");
    }
}
