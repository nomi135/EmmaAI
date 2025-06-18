using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace API.Data.Migrations
{
    /// <inheritdoc />
    public partial class SurveyFormAdded : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_reminders_AspNetUsers_AppUserId",
                table: "reminders");

            migrationBuilder.DropPrimaryKey(
                name: "PK_reminders",
                table: "reminders");

            migrationBuilder.RenameTable(
                name: "reminders",
                newName: "Reminders");

            migrationBuilder.RenameIndex(
                name: "IX_reminders_AppUserId",
                table: "Reminders",
                newName: "IX_Reminders_AppUserId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Reminders",
                table: "Reminders",
                column: "Id");

            migrationBuilder.CreateTable(
                name: "SurveyForms",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Title = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Path = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ImagePath = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Created = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SurveyForms", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SurveyFormDetail",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Label = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Type = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PageNo = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Left = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Top = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Width = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Height = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    FontName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    FontSize = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SurveyFormId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SurveyFormDetail", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SurveyFormDetail_SurveyForms_SurveyFormId",
                        column: x => x.SurveyFormId,
                        principalTable: "SurveyForms",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_SurveyFormDetail_SurveyFormId",
                table: "SurveyFormDetail",
                column: "SurveyFormId");

            migrationBuilder.AddForeignKey(
                name: "FK_Reminders_AspNetUsers_AppUserId",
                table: "Reminders",
                column: "AppUserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Reminders_AspNetUsers_AppUserId",
                table: "Reminders");

            migrationBuilder.DropTable(
                name: "SurveyFormDetail");

            migrationBuilder.DropTable(
                name: "SurveyForms");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Reminders",
                table: "Reminders");

            migrationBuilder.RenameTable(
                name: "Reminders",
                newName: "reminders");

            migrationBuilder.RenameIndex(
                name: "IX_Reminders_AppUserId",
                table: "reminders",
                newName: "IX_reminders_AppUserId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_reminders",
                table: "reminders",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_reminders_AspNetUsers_AppUserId",
                table: "reminders",
                column: "AppUserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id");
        }
    }
}
