using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EmbryoApp.Migrations
{
    /// <inheritdoc />
    public partial class AddModelComment : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ModelComment",
                columns: table => new
                {
                    ModelCommentId = table.Column<Guid>(type: "uuid", nullable: false),
                    ModelId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<string>(type: "text", nullable: false),
                    Content = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ModelComment", x => x.ModelCommentId);
                    table.ForeignKey(
                        name: "FK_ModelComment_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ModelComment_Model3D_ModelId",
                        column: x => x.ModelId,
                        principalTable: "Model3D",
                        principalColumn: "ModelId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ModelComment_ModelId",
                table: "ModelComment",
                column: "ModelId");

            migrationBuilder.CreateIndex(
                name: "IX_ModelComment_ModelId_CreatedAt",
                table: "ModelComment",
                columns: new[] { "ModelId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_ModelComment_UserId",
                table: "ModelComment",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ModelComment");
        }
    }
}
