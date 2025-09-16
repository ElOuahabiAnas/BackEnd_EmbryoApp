using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EmbryoApp.Migrations
{
    /// <inheritdoc />
    public partial class CreateQuiz : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Quiz",
                columns: table => new
                {
                    QuizId = table.Column<Guid>(type: "uuid", nullable: false),
                    Description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    TimeLimit = table.Column<int>(type: "integer", nullable: true),
                    Attempts = table.Column<int>(type: "integer", nullable: true),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    PublishedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ModelId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Quiz", x => x.QuizId);
                    table.CheckConstraint("CK_Quiz_Status", "\"Status\" IN ('Draft','Active','Closed')");
                    table.ForeignKey(
                        name: "FK_Quiz_Model3D_ModelId",
                        column: x => x.ModelId,
                        principalTable: "Model3D",
                        principalColumn: "ModelId",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Quiz_ModelId",
                table: "Quiz",
                column: "ModelId");

            migrationBuilder.CreateIndex(
                name: "IX_Quiz_PublishedAt",
                table: "Quiz",
                column: "PublishedAt");

            migrationBuilder.CreateIndex(
                name: "IX_Quiz_Status",
                table: "Quiz",
                column: "Status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Quiz");
        }
    }
}
