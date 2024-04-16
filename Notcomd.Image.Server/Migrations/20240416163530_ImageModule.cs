using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Notcomd.Image.Server.Migrations
{
    /// <inheritdoc />
    public partial class ImageModule : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Notcomd_Image_Evaluate_Module",
                columns: table => new
                {
                    Image_Ownership = table.Column<string>(type: "text", nullable: false),
                    Image_Level = table.Column<long>(type: "bigint", nullable: false),
                    Image_Type = table.Column<string>(type: "text", nullable: false),
                    Image_Size = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Notcomd_Image_Evaluate_Module", x => x.Image_Ownership);
                });

            migrationBuilder.CreateTable(
                name: "Notcomd_Image_Ownership_Module",
                columns: table => new
                {
                    Image_Ownership_UserName = table.Column<string>(type: "text", nullable: false),
                    Image_User_HeadPort = table.Column<string>(type: "text", nullable: false),
                    Image_Privati = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Notcomd_Image_Ownership_Module", x => x.Image_Ownership_UserName);
                });

            migrationBuilder.CreateTable(
                name: "Notcomd_Image_Table",
                columns: table => new
                {
                    Image_Url = table.Column<string>(type: "text", nullable: false),
                    ImageTage = table.Column<List<string>>(type: "text[]", nullable: false),
                    Image_Name = table.Column<string>(type: "text", nullable: false),
                    Image_Remake = table.Column<string>(type: "Text", nullable: false),
                    Image_CreateTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Notcomd_Image_Table", x => x.Image_Url);
                    table.ForeignKey(
                        name: "FK_Notcomd_Image_Table_Notcomd_Image_Evaluate_Module_Image_Url",
                        column: x => x.Image_Url,
                        principalTable: "Notcomd_Image_Evaluate_Module",
                        principalColumn: "Image_Ownership",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Notcomd_Image_Table_Notcomd_Image_Ownership_Module_Image_Url",
                        column: x => x.Image_Url,
                        principalTable: "Notcomd_Image_Ownership_Module",
                        principalColumn: "Image_Ownership_UserName",
                        onDelete: ReferentialAction.Cascade);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Notcomd_Image_Table");

            migrationBuilder.DropTable(
                name: "Notcomd_Image_Evaluate_Module");

            migrationBuilder.DropTable(
                name: "Notcomd_Image_Ownership_Module");
        }
    }
}
