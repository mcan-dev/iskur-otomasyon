using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IskurAuto.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddTerminations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TerminationDecisions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    StudentProfileId = table.Column<int>(type: "INTEGER", nullable: false),
                    Basis = table.Column<string>(type: "TEXT", maxLength: 30, nullable: false),
                    DecisionDate = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    Note = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: false),
                    LeaveDaysUsed = table.Column<decimal>(type: "TEXT", precision: 5, scale: 2, nullable: false),
                    UnexcusedAbsenceDays = table.Column<int>(type: "INTEGER", nullable: false),
                    DecidedByUserId = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TerminationDecisions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TerminationDecisions_StudentProfiles_StudentProfileId",
                        column: x => x.StudentProfileId,
                        principalTable: "StudentProfiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TerminationDecisions_Users_DecidedByUserId",
                        column: x => x.DecidedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TerminationDecisions_DecidedByUserId",
                table: "TerminationDecisions",
                column: "DecidedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_TerminationDecisions_StudentProfileId_DecisionDate",
                table: "TerminationDecisions",
                columns: new[] { "StudentProfileId", "DecisionDate" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TerminationDecisions");
        }
    }
}
