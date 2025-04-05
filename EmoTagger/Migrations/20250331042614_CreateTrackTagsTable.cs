using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace EmoTagger.Migrations
{
    /// <inheritdoc />
    public partial class CreateTrackTagsTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "reset_token",
                table: "Users",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "reset_token_expiry",
                table: "Users",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "tags",
                columns: table => new
                {
                    TagId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TagName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tags", x => x.TagId);
                });

            migrationBuilder.CreateTable(
                name: "music",
                columns: table => new
                {
                    musicid = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    title = table.Column<string>(type: "text", nullable: false),
                    artist = table.Column<string>(type: "text", nullable: false),
                    filename = table.Column<string>(type: "text", nullable: false),
                    createdat = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    primary_tag_id = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_music", x => x.musicid);
                    table.ForeignKey(
                        name: "FK_music_tags_primary_tag_id",
                        column: x => x.primary_tag_id,
                        principalTable: "tags",
                        principalColumn: "TagId");
                });

            migrationBuilder.CreateTable(
                name: "playhistory",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<int>(type: "integer", nullable: false),
                    MusicId = table.Column<int>(type: "integer", nullable: false),
                    PlayCount = table.Column<int>(type: "integer", nullable: false),
                    LastPlayedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_playhistory", x => x.Id);
                    table.ForeignKey(
                        name: "FK_playhistory_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_playhistory_music_MusicId",
                        column: x => x.MusicId,
                        principalTable: "music",
                        principalColumn: "musicid",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "tracktags",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<int>(type: "integer", nullable: false),
                    MusicId = table.Column<int>(type: "integer", nullable: false),
                    TagId = table.Column<int>(type: "integer", nullable: false),
                    TagCount = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tracktags", x => x.Id);
                    table.ForeignKey(
                        name: "FK_tracktags_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_tracktags_music_MusicId",
                        column: x => x.MusicId,
                        principalTable: "music",
                        principalColumn: "musicid",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_tracktags_tags_TagId",
                        column: x => x.TagId,
                        principalTable: "tags",
                        principalColumn: "TagId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_music_primary_tag_id",
                table: "music",
                column: "primary_tag_id");

            migrationBuilder.CreateIndex(
                name: "IX_playhistory_MusicId",
                table: "playhistory",
                column: "MusicId");

            migrationBuilder.CreateIndex(
                name: "IX_playhistory_UserId_MusicId",
                table: "playhistory",
                columns: new[] { "UserId", "MusicId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_tracktags_MusicId",
                table: "tracktags",
                column: "MusicId");

            migrationBuilder.CreateIndex(
                name: "IX_tracktags_TagId",
                table: "tracktags",
                column: "TagId");

            migrationBuilder.CreateIndex(
                name: "IX_tracktags_UserId",
                table: "tracktags",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "playhistory");

            migrationBuilder.DropTable(
                name: "tracktags");

            migrationBuilder.DropTable(
                name: "music");

            migrationBuilder.DropTable(
                name: "tags");

            migrationBuilder.DropColumn(
                name: "reset_token",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "reset_token_expiry",
                table: "Users");
        }
    }
}
