using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddTopicsAndMembership : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Topics",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ParentTopicId = table.Column<Guid>(type: "uuid", nullable: true),
                    RootTopicId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true),
                    InviteCode = table.Column<string>(type: "text", nullable: true),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Topics", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Topics_Topics_ParentTopicId",
                        column: x => x.ParentTopicId,
                        principalTable: "Topics",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Topics_Topics_RootTopicId",
                        column: x => x.RootTopicId,
                        principalTable: "Topics",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "TopicMembers",
                columns: table => new
                {
                    RootTopicId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    JoinedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TopicMembers", x => new { x.RootTopicId, x.UserId });
                    table.ForeignKey(
                        name: "FK_TopicMembers_Topics_RootTopicId",
                        column: x => x.RootTopicId,
                        principalTable: "Topics",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TopicMembers_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TopicMembers_UserId",
                table: "TopicMembers",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Topics_InviteCode",
                table: "Topics",
                column: "InviteCode",
                unique: true,
                filter: "\"InviteCode\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Topics_ParentTopicId",
                table: "Topics",
                column: "ParentTopicId");

            migrationBuilder.CreateIndex(
                name: "IX_Topics_RootTopicId",
                table: "Topics",
                column: "RootTopicId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TopicMembers");

            migrationBuilder.DropTable(
                name: "Topics");
        }
    }
}
