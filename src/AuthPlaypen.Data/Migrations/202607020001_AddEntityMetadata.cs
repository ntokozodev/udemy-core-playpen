using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AuthPlaypen.Data.Migrations;

public partial class AddEntityMetadata : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<DateTimeOffset>(name: "CreatedAt", table: "applications", type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()");
        migrationBuilder.AddColumn<string>(name: "CreatedBy", table: "applications", type: "text", nullable: false, defaultValue: "Unknown");
        migrationBuilder.AddColumn<DateTimeOffset>(name: "UpdatedAt", table: "applications", type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()");
        migrationBuilder.AddColumn<string>(name: "UpdatedBy", table: "applications", type: "text", nullable: false, defaultValue: "Unknown");

        migrationBuilder.AddColumn<DateTimeOffset>(name: "CreatedAt", table: "scopes", type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()");
        migrationBuilder.AddColumn<string>(name: "CreatedBy", table: "scopes", type: "text", nullable: false, defaultValue: "Unknown");
        migrationBuilder.AddColumn<DateTimeOffset>(name: "UpdatedAt", table: "scopes", type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()");
        migrationBuilder.AddColumn<string>(name: "UpdatedBy", table: "scopes", type: "text", nullable: false, defaultValue: "Unknown");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(name: "CreatedAt", table: "applications");
        migrationBuilder.DropColumn(name: "CreatedBy", table: "applications");
        migrationBuilder.DropColumn(name: "UpdatedAt", table: "applications");
        migrationBuilder.DropColumn(name: "UpdatedBy", table: "applications");
        migrationBuilder.DropColumn(name: "CreatedAt", table: "scopes");
        migrationBuilder.DropColumn(name: "CreatedBy", table: "scopes");
        migrationBuilder.DropColumn(name: "UpdatedAt", table: "scopes");
        migrationBuilder.DropColumn(name: "UpdatedBy", table: "scopes");
    }
}
