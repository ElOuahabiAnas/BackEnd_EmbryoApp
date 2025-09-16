using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EmbryoApp.Migrations
{
    /// <inheritdoc />
    public partial class CreateAttemptAnswer : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AttemptAnswer",
                columns: table => new
                {
                    AttemptId = table.Column<Guid>(type: "uuid", nullable: false),
                    QuestionId = table.Column<Guid>(type: "uuid", nullable: false),
                    Response = table.Column<string>(type: "text", nullable: true),
                    IsCorrect = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AttemptAnswer", x => new { x.AttemptId, x.QuestionId });
                    table.ForeignKey(
                        name: "FK_AttemptAnswer_Attempt_AttemptId",
                        column: x => x.AttemptId,
                        principalTable: "Attempt",
                        principalColumn: "AttemptId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AttemptAnswer_Question_QuestionId",
                        column: x => x.QuestionId,
                        principalTable: "Question",
                        principalColumn: "QuestionId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AttemptAnswer_AttemptId",
                table: "AttemptAnswer",
                column: "AttemptId");

            migrationBuilder.CreateIndex(
                name: "IX_AttemptAnswer_QuestionId",
                table: "AttemptAnswer",
                column: "QuestionId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AttemptAnswer");
        }
    }
}
