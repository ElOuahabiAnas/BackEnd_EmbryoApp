using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EmbryoApp.Migrations
{
    /// <inheritdoc />
    public partial class CreateAttempt : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Attempt",
                columns: table => new
                {
                    AttemptId = table.Column<Guid>(type: "uuid", nullable: false),
                    Score = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false),
                    AttemptedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    Duration = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    UserId = table.Column<string>(type: "text", nullable: false),
                    QuizId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Attempt", x => x.AttemptId);
                    table.ForeignKey(
                        name: "FK_Attempt_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Attempt_Quiz_QuizId",
                        column: x => x.QuizId,
                        principalTable: "Quiz",
                        principalColumn: "QuizId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Attempt_AttemptedAt",
                table: "Attempt",
                column: "AttemptedAt");

            migrationBuilder.CreateIndex(
                name: "IX_Attempt_QuizId",
                table: "Attempt",
                column: "QuizId");

            migrationBuilder.CreateIndex(
                name: "IX_Attempt_UserId",
                table: "Attempt",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Attempt");
        }
    }
}
