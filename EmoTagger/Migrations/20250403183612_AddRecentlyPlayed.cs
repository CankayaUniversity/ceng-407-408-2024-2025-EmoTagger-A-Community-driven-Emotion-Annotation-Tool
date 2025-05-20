using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace EmoTagger.Migrations
{
    /// <inheritdoc />
    public partial class AddRecentlyPlayed : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "playhistory");

            migrationBuilder.CreateTable(
                name: "RecentlyPlayed",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    MusicId = table.Column<int>(type: "integer", nullable: false),
                    UserId = table.Column<int>(type: "integer", nullable: false),
                    PlayedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RecentlyPlayed", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RecentlyPlayed_music_MusicId",
                        column: x => x.MusicId,
                        principalTable: "music",
                        principalColumn: "musicid",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RecentlyTagged",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    MusicId = table.Column<int>(type: "integer", nullable: false),
                    UserId = table.Column<int>(type: "integer", nullable: false),
                    TagName = table.Column<string>(type: "text", nullable: false),
                    TaggedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RecentlyTagged", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RecentlyTagged_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RecentlyTagged_music_MusicId",
                        column: x => x.MusicId,
                        principalTable: "music",
                        principalColumn: "musicid",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_RecentlyPlayed_MusicId",
                table: "RecentlyPlayed",
                column: "MusicId");

            migrationBuilder.CreateIndex(
                name: "IX_RecentlyTagged_MusicId",
                table: "RecentlyTagged",
                column: "MusicId");

            migrationBuilder.CreateIndex(
                name: "IX_RecentlyTagged_UserId",
                table: "RecentlyTagged",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RecentlyPlayed");

            migrationBuilder.DropTable(
                name: "RecentlyTagged");

            migrationBuilder.CreateTable(
                name: "playhistory",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    MusicId = table.Column<int>(type: "integer", nullable: false),
                    UserId = table.Column<int>(type: "integer", nullable: false),
                    LastPlayedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    PlayCount = table.Column<int>(type: "integer", nullable: false)
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

            migrationBuilder.CreateIndex(
                name: "IX_playhistory_MusicId",
                table: "playhistory",
                column: "MusicId");

            migrationBuilder.CreateIndex(
                name: "IX_playhistory_UserId_MusicId",
                table: "playhistory",
                columns: new[] { "UserId", "MusicId" },
                unique: true);
        }
    }
}
