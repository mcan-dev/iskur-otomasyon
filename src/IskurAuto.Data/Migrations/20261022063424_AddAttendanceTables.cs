using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IskurAuto.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddAttendanceTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AttendanceSessions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Date = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    StartTime = table.Column<TimeOnly>(type: "TEXT", nullable: false),
                    EndTime = table.Column<TimeOnly>(type: "TEXT", nullable: false),
                    Title = table.Column<string>(type: "TEXT", maxLength: 150, nullable: true),
                    DepartmentId = table.Column<int>(type: "INTEGER", nullable: true),
                    SecretKey = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    CreatedByUserId = table.Column<int>(type: "INTEGER", nullable: false),
                    ClosedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AttendanceSessions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AttendanceSessions_Departments_DepartmentId",
                        column: x => x.DepartmentId,
                        principalTable: "Departments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AttendanceSessions_Users_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AttendanceRecords",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    StudentProfileId = table.Column<int>(type: "INTEGER", nullable: false),
                    AttendanceSessionId = table.Column<int>(type: "INTEGER", nullable: false),
                    Date = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    CheckedInAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    WorkPlanDayId = table.Column<int>(type: "INTEGER", nullable: true),
                    IsManual = table.Column<bool>(type: "INTEGER", nullable: false),
                    ManualReason = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    CreatedByUserId = table.Column<int>(type: "INTEGER", nullable: true),
                    VoidedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    VoidedByUserId = table.Column<int>(type: "INTEGER", nullable: true),
                    VoidReason = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AttendanceRecords", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AttendanceRecords_AttendanceSessions_AttendanceSessionId",
                        column: x => x.AttendanceSessionId,
                        principalTable: "AttendanceSessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AttendanceRecords_StudentProfiles_StudentProfileId",
                        column: x => x.StudentProfileId,
                        principalTable: "StudentProfiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AttendanceRecords_Users_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AttendanceRecords_Users_VoidedByUserId",
                        column: x => x.VoidedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AttendanceRecords_WorkPlanDays_WorkPlanDayId",
                        column: x => x.WorkPlanDayId,
                        principalTable: "WorkPlanDays",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AttendanceRecords_AttendanceSessionId",
                table: "AttendanceRecords",
                column: "AttendanceSessionId");

            migrationBuilder.CreateIndex(
                name: "IX_AttendanceRecords_CreatedByUserId",
                table: "AttendanceRecords",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_AttendanceRecords_StudentProfileId_Date",
                table: "AttendanceRecords",
                columns: new[] { "StudentProfileId", "Date" });

            migrationBuilder.CreateIndex(
                name: "IX_AttendanceRecords_VoidedByUserId",
                table: "AttendanceRecords",
                column: "VoidedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_AttendanceRecords_WorkPlanDayId",
                table: "AttendanceRecords",
                column: "WorkPlanDayId");

            migrationBuilder.CreateIndex(
                name: "UX_Attendance_OneActivePerStudentSession",
                table: "AttendanceRecords",
                columns: new[] { "StudentProfileId", "AttendanceSessionId" },
                unique: true,
                filter: "\"VoidedAt\" IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_AttendanceSessions_CreatedByUserId",
                table: "AttendanceSessions",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_AttendanceSessions_Date",
                table: "AttendanceSessions",
                column: "Date");

            migrationBuilder.CreateIndex(
                name: "IX_AttendanceSessions_DepartmentId",
                table: "AttendanceSessions",
                column: "DepartmentId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AttendanceRecords");

            migrationBuilder.DropTable(
                name: "AttendanceSessions");
        }
    }
}
