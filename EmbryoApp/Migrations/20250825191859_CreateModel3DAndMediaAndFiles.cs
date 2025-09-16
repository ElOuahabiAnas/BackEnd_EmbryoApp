using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EmbryoApp.Migrations
{
    /// <inheritdoc />
    public partial class CreateModel3DAndMediaAndFiles : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Model3D",
                columns: table => new
                {
                    ModelId = table.Column<Guid>(type: "uuid", nullable: false),
                    Title = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    Discipline = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    EmbryoDay = table.Column<int>(type: "integer", nullable: true),
                    Description = table.Column<string>(type: "text", nullable: true),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    PublishedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    AuthorUserId = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Model3D", x => x.ModelId);
                    table.CheckConstraint("CK_Model3D_Status", "\"Status\" IN ('Draft','Active','Closed')");
                    table.ForeignKey(
                        name: "FK_Model3D_AspNetUsers_AuthorUserId",
                        column: x => x.AuthorUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ModelFiles",
                columns: table => new
                {
                    FileId = table.Column<Guid>(type: "uuid", nullable: false),
                    Path = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: false),
                    FileType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    FileRole = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    IsPrimary = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    Position = table.Column<int>(type: "integer", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    ModelId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ModelFiles", x => x.FileId);
                    table.ForeignKey(
                        name: "FK_ModelFiles_Model3D_ModelId",
                        column: x => x.ModelId,
                        principalTable: "Model3D",
                        principalColumn: "ModelId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ModelMedia",
                columns: table => new
                {
                    MediaId = table.Column<Guid>(type: "uuid", nullable: false),
                    Url = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: false),
                    MediaType = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Legende = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    Position = table.Column<int>(type: "integer", nullable: true),
                    IsPrimary = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    ModelId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ModelMedia", x => x.MediaId);
                    table.CheckConstraint("CK_ModelMedia_MediaType", "\"MediaType\" IN ('Photo','Video')");
                    table.ForeignKey(
                        name: "FK_ModelMedia_Model3D_ModelId",
                        column: x => x.ModelId,
                        principalTable: "Model3D",
                        principalColumn: "ModelId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Model3D_AuthorUserId",
                table: "Model3D",
                column: "AuthorUserId");

            migrationBuilder.CreateIndex(
                name: "IX_ModelFiles_ModelId",
                table: "ModelFiles",
                column: "ModelId");

            migrationBuilder.CreateIndex(
                name: "IX_ModelFiles_ModelId_IsPrimary",
                table: "ModelFiles",
                columns: new[] { "ModelId", "IsPrimary" });

            migrationBuilder.CreateIndex(
                name: "IX_ModelMedia_ModelId",
                table: "ModelMedia",
                column: "ModelId");

            migrationBuilder.CreateIndex(
                name: "IX_ModelMedia_ModelId_IsPrimary",
                table: "ModelMedia",
                columns: new[] { "ModelId", "IsPrimary" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ModelFiles");

            migrationBuilder.DropTable(
                name: "ModelMedia");

            migrationBuilder.DropTable(
                name: "Model3D");
        }
    }
}
