using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ReviewService.Migrations
{
    public partial class AddReviewImageUrl : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ImageUrl",
                table: "Reviews",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true);
        }
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ImageUrl",
                table: "Reviews");
        }
    }
}

