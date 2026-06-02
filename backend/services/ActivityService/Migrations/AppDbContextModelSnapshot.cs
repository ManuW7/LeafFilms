using System;
using ActivityService.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace ActivityService.Migrations
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

            modelBuilder.Entity("ActivityService.Models.Playlist", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uuid");

                    b.Property<DateTime>("CreatedAt")
                        .HasColumnType("timestamp with time zone");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<Guid>("UserId")
                        .HasColumnType("uuid");

                    b.HasKey("Id");

                    b.ToTable("Playlists");
                });

            modelBuilder.Entity("ActivityService.Models.PlaylistMovie", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uuid");

                    b.Property<DateTime>("AddedAt")
                        .HasColumnType("timestamp with time zone");

                    b.Property<Guid>("MovieId")
                        .HasColumnType("uuid");

                    b.Property<string>("MovieTitle")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<Guid>("PlaylistId")
                        .HasColumnType("uuid");

                    b.HasKey("Id");

                    b.HasIndex("PlaylistId", "MovieId")
                        .IsUnique();

                    b.ToTable("PlaylistMovies");
                });

            modelBuilder.Entity("ActivityService.Models.WatchHistory", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uuid");

                    b.Property<Guid>("MovieId")
                        .HasColumnType("uuid");

                    b.Property<string>("MovieTitle")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<Guid>("UserId")
                        .HasColumnType("uuid");

                    b.Property<DateTime>("WatchedAt")
                        .HasColumnType("timestamp with time zone");

                    b.HasKey("Id");

                    b.HasIndex("UserId");

                    b.ToTable("WatchHistory");
                });

            modelBuilder.Entity("ActivityService.Models.WatchlistItem", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uuid");

                    b.Property<DateTime>("AddedAt")
                        .HasColumnType("timestamp with time zone");

                    b.Property<Guid>("MovieId")
                        .HasColumnType("uuid");

                    b.Property<string>("MovieTitle")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<Guid>("UserId")
                        .HasColumnType("uuid");

                    b.HasKey("Id");

                    b.HasIndex("UserId", "MovieId")
                        .IsUnique();

                    b.ToTable("WatchlistItems");
                });

            modelBuilder.Entity("ActivityService.Models.PlaylistMovie", b =>
                {
                    b.HasOne("ActivityService.Models.Playlist", "Playlist")
                        .WithMany("Movies")
                        .HasForeignKey("PlaylistId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Playlist");
                });

            modelBuilder.Entity("ActivityService.Models.Playlist", b =>
                {
                    b.Navigation("Movies");
                });
#pragma warning restore 612, 618
        }
    }
}

