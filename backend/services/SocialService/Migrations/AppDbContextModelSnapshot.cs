using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;
using SocialService.Data;

#nullable disable

namespace SocialService.Migrations
{
    [DbContext(typeof(AppDbContext))]
    partial class AppDbContextModelSnapshot : ModelSnapshot
    {
        protected override void BuildModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "8.0.0")
                .HasAnnotation("Relational:MaxIdentifierLength", 63);

            NpgsqlModelBuilderExtensions.UseIdentityByDefaultColumns(modelBuilder);

            modelBuilder.Entity("SocialService.Models.Follow", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uuid");

                    b.Property<DateTime>("CreatedAt")
                        .HasColumnType("timestamp with time zone");

                    b.Property<Guid>("FollowedId")
                        .HasColumnType("uuid");

                    b.Property<string>("FollowedName")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<Guid>("FollowerId")
                        .HasColumnType("uuid");

                    b.Property<string>("FollowerName")
                        .IsRequired()
                        .HasColumnType("text");

                    b.HasKey("Id");

                    b.HasIndex("FollowedId");

                    b.HasIndex("FollowerId", "FollowedId")
                        .IsUnique();

                    b.ToTable("Follows");
                });
#pragma warning restore 612, 618
        }
    }
}

