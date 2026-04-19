using System;
using AuthPlaypen.Data.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace AuthPlaypen.Data.Migrations;

[DbContext(typeof(AuthPlaypenDbContext))]
partial class AuthPlaypenDbContextModelSnapshot : ModelSnapshot
{
    protected override void BuildModel(ModelBuilder modelBuilder)
    {
#pragma warning disable 612, 618
        modelBuilder
            .HasAnnotation("ProductVersion", "8.0.7")
            .HasAnnotation("Relational:MaxIdentifierLength", 63);

        NpgsqlModelBuilderExtensions.UseIdentityByDefaultColumns(modelBuilder);

        modelBuilder.Entity("AuthPlaypen.Domain.Entities.ApplicationEntity", b =>
        {
            b.Property<Guid>("Id")
                .ValueGeneratedOnAdd()
                .HasColumnType("uuid");

            b.Property<string>("ClientId")
                .IsRequired()
                .HasColumnType("text");

            b.Property<string>("ClientSecret")
                .IsRequired()
                .HasColumnType("text");

            b.Property<DateTimeOffset>("CreatedAt")
                .HasColumnType("timestamp with time zone");

            b.Property<string>("CreatedBy")
                .IsRequired()
                .HasColumnType("text");

            b.Property<string>("DisplayName")
                .IsRequired()
                .HasColumnType("text");

            b.Property<string>("Flow")
                .IsRequired()
                .HasColumnType("text");

            b.Property<string>("PostLogoutRedirectUris")
                .HasColumnType("text");

            b.Property<string>("RedirectUris")
                .HasColumnType("text");

            b.Property<DateTimeOffset>("UpdatedAt")
                .HasColumnType("timestamp with time zone");

            b.Property<string>("UpdatedBy")
                .IsRequired()
                .HasColumnType("text");

            b.HasKey("Id");

            b.HasIndex("ClientId")
                .IsUnique();

            b.ToTable("applications");
        });

        modelBuilder.Entity("AuthPlaypen.Domain.Entities.ApplicationScopeEntity", b =>
        {
            b.Property<Guid>("ApplicationId")
                .HasColumnType("uuid");

            b.Property<Guid>("ScopeId")
                .HasColumnType("uuid");

            b.HasKey("ApplicationId", "ScopeId");

            b.HasIndex("ScopeId");

            b.ToTable("application_scopes");
        });

        modelBuilder.Entity("AuthPlaypen.Domain.Entities.EntityAuditHistoryEntry", b =>
        {
            b.Property<Guid>("Id")
                .ValueGeneratedOnAdd()
                .HasColumnType("uuid");

            b.Property<string>("Action")
                .IsRequired()
                .HasColumnType("text");

            b.Property<string>("ActorDisplayName")
                .IsRequired()
                .HasColumnType("text");

            b.Property<string>("ActorEmail")
                .HasColumnType("text");

            b.Property<string>("ChangeSummaryJson")
                .HasColumnType("text");

            b.Property<Guid>("EntityId")
                .HasColumnType("uuid");

            b.Property<string>("EntityType")
                .IsRequired()
                .HasColumnType("text");

            b.Property<DateTimeOffset>("OccurredAt")
                .HasColumnType("timestamp with time zone");

            b.HasKey("Id");

            b.HasIndex("EntityType", "EntityId", "OccurredAt");

            b.ToTable("entity_audit_history");
        });

        modelBuilder.Entity("AuthPlaypen.Domain.Entities.ScopeEntity", b =>
        {
            b.Property<Guid>("Id")
                .ValueGeneratedOnAdd()
                .HasColumnType("uuid");

            b.Property<DateTimeOffset>("CreatedAt")
                .HasColumnType("timestamp with time zone");

            b.Property<string>("CreatedBy")
                .IsRequired()
                .HasColumnType("text");

            b.Property<string>("Description")
                .IsRequired()
                .HasColumnType("text");

            b.Property<string>("DisplayName")
                .IsRequired()
                .HasColumnType("text");


            b.Property<string>("ScopeName")
                .IsRequired()
                .HasColumnType("text");

            b.Property<DateTimeOffset>("UpdatedAt")
                .HasColumnType("timestamp with time zone");

            b.Property<string>("UpdatedBy")
                .IsRequired()
                .HasColumnType("text");

            b.HasKey("Id");

            b.HasIndex("ScopeName")
                .IsUnique();

            b.ToTable("scopes");
        });

        modelBuilder.Entity("AuthPlaypen.Domain.Entities.ApplicationScopeEntity", b =>
        {
            b.HasOne("AuthPlaypen.Domain.Entities.ApplicationEntity", "Application")
                .WithMany("ApplicationScopes")
                .HasForeignKey("ApplicationId")
                .OnDelete(DeleteBehavior.Cascade)
                .IsRequired();

            b.HasOne("AuthPlaypen.Domain.Entities.ScopeEntity", "Scope")
                .WithMany("ApplicationScopes")
                .HasForeignKey("ScopeId")
                .OnDelete(DeleteBehavior.Cascade)
                .IsRequired();

            b.Navigation("Application");

            b.Navigation("Scope");
        });
#pragma warning restore 612, 618
    }
}
