using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CatalogueService.Migrations
{
    /// <inheritdoc />
    public partial class AddMovieTitleRu : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "TitleRu",
                table: "Movies",
                type: "character varying(300)",
                maxLength: 300,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Movies_TitleRu",
                table: "Movies",
                column: "TitleRu");

            migrationBuilder.Sql("""
                UPDATE "Movies" SET "TitleRu" = 'Начало' WHERE "Title" = 'Inception' AND "TitleRu" IS NULL;
                UPDATE "Movies" SET "TitleRu" = 'Матрица' WHERE "Title" = 'The Matrix' AND "TitleRu" IS NULL;
                UPDATE "Movies" SET "TitleRu" = 'Интерстеллар' WHERE "Title" = 'Interstellar' AND "TitleRu" IS NULL;
                UPDATE "Movies" SET "TitleRu" = 'Темный рыцарь' WHERE "Title" = 'The Dark Knight' AND "TitleRu" IS NULL;
                UPDATE "Movies" SET "TitleRu" = 'Паразиты' WHERE "Title" = 'Parasite' AND "TitleRu" IS NULL;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Movies_TitleRu",
                table: "Movies");

            migrationBuilder.DropColumn(
                name: "TitleRu",
                table: "Movies");
        }
    }
}
