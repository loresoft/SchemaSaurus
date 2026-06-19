using FluentMigrator.Runner;

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace SchemaSaurus.Sqlite.Tests.Fixtures;

public class DatabaseInitializer : IHostedService
{
    private readonly ILogger<DatabaseInitializer> _logger;
    private readonly IMigrationRunner _migrationRunner;

    public DatabaseInitializer(
        ILogger<DatabaseInitializer> logger,
        IMigrationRunner migrationRunner)
    {
        _logger = logger;
        _migrationRunner = migrationRunner;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Starting database migration for {DatabaseType}...",
            _migrationRunner.Processor.DatabaseType);

        // Enable foreign key constraints
        _migrationRunner.Processor.Execute("PRAGMA foreign_keys = ON");

        // run migrations
        _migrationRunner.MigrateUp();

        CreateSqliteForeignKeys();
        CreateSqliteEdgeCaseObjects();

        return Task.CompletedTask;
    }

    private void CreateSqliteForeignKeys()
    {
        RecreateTaskTable();
        RecreateTaskExtendedTable();
        RecreateUserRoleTable();
    }

    private void RecreateTaskExtendedTable()
    {
        _migrationRunner.Processor.Execute("""
            ALTER TABLE "TaskExtended"
            RENAME TO "TaskExtended_Migration"
            """);

        _migrationRunner.Processor.Execute("""
            CREATE TABLE "TaskExtended" (
                "TaskId" UNIQUEIDENTIFIER NOT NULL,
                "UserAgent" TEXT,
                "Browser" NVARCHAR(256),
                "OperatingSystem" NVARCHAR(256),
                "Created" DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
                "CreatedBy" NVARCHAR(100),
                "Updated" DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
                "UpdatedBy" NVARCHAR(100),
                "RowVersion" INTEGER NOT NULL,
                CONSTRAINT "PK_TaskExtended" PRIMARY KEY ("TaskId"),
                CONSTRAINT "FK_TaskExtended_Task_TaskId" FOREIGN KEY ("TaskId") REFERENCES "Task" ("Id")
            )
            """);

        _migrationRunner.Processor.Execute("""
            INSERT INTO "TaskExtended" (
                "TaskId",
                "UserAgent",
                "Browser",
                "OperatingSystem",
                "Created",
                "CreatedBy",
                "Updated",
                "UpdatedBy",
                "RowVersion"
            )
            SELECT
                "TaskId",
                "UserAgent",
                "Browser",
                "OperatingSystem",
                "Created",
                "CreatedBy",
                "Updated",
                "UpdatedBy",
                "RowVersion"
            FROM "TaskExtended_Migration"
            """);

        _migrationRunner.Processor.Execute("""
            DROP TABLE "TaskExtended_Migration"
            """);
    }

    private void RecreateTaskTable()
    {
        _migrationRunner.Processor.Execute("""
            ALTER TABLE "Task"
            RENAME TO "Task_Migration"
            """);

        _migrationRunner.Processor.Execute("""
            CREATE TABLE "Task" (
                "Id" UNIQUEIDENTIFIER NOT NULL,
                "StatusId" INTEGER NOT NULL,
                "PriorityId" INTEGER,
                "Title" NVARCHAR(255) NOT NULL,
                "Description" TEXT,
                "StartDate" DATETIME,
                "DueDate" DATETIME,
                "CompleteDate" DATETIME,
                "AssignedId" INTEGER,
                "Created" DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
                "CreatedBy" NVARCHAR(100),
                "Updated" DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
                "UpdatedBy" NVARCHAR(100),
                "RowVersion" INTEGER NOT NULL,
                CONSTRAINT "PK_Task" PRIMARY KEY ("Id"),
                CONSTRAINT "FK_Task_Priority_PriorityId" FOREIGN KEY ("PriorityId") REFERENCES "Priority" ("Id"),
                CONSTRAINT "FK_Task_Status_StatusId" FOREIGN KEY ("StatusId") REFERENCES "Status" ("Id"),
                CONSTRAINT "FK_Task_User_AssignedId" FOREIGN KEY ("AssignedId") REFERENCES "User" ("Id")
            )
            """);

        _migrationRunner.Processor.Execute("""
            INSERT INTO "Task" (
                "Id",
                "StatusId",
                "PriorityId",
                "Title",
                "Description",
                "StartDate",
                "DueDate",
                "CompleteDate",
                "AssignedId",
                "Created",
                "CreatedBy",
                "Updated",
                "UpdatedBy",
                "RowVersion"
            )
            SELECT
                "Id",
                "StatusId",
                "PriorityId",
                "Title",
                "Description",
                "StartDate",
                "DueDate",
                "CompleteDate",
                "AssignedId",
                "Created",
                "CreatedBy",
                "Updated",
                "UpdatedBy",
                "RowVersion"
            FROM "Task_Migration"
            """);

        _migrationRunner.Processor.Execute("""
            DROP TABLE "Task_Migration"
            """);

        _migrationRunner.Processor.Execute("""
            CREATE INDEX "IX_Task_AssignedId"
            ON "Task" ("AssignedId")
            """);

        _migrationRunner.Processor.Execute("""
            CREATE INDEX "IX_Task_PriorityId"
            ON "Task" ("PriorityId")
            """);

        _migrationRunner.Processor.Execute("""
            CREATE INDEX "IX_Task_StatusId"
            ON "Task" ("StatusId")
            """);
    }

    private void RecreateUserRoleTable()
    {
        _migrationRunner.Processor.Execute("""
            ALTER TABLE "UserRole"
            RENAME TO "UserRole_Migration"
            """);

        _migrationRunner.Processor.Execute("""
            CREATE TABLE "UserRole" (
                "UserId" INTEGER NOT NULL,
                "RoleId" INTEGER NOT NULL,
                CONSTRAINT "PK_UserRole" PRIMARY KEY ("UserId", "RoleId"),
                CONSTRAINT "FK_UserRole_Role_RoleId" FOREIGN KEY ("RoleId") REFERENCES "Role" ("Id"),
                CONSTRAINT "FK_UserRole_User_UserId" FOREIGN KEY ("UserId") REFERENCES "User" ("Id")
            )
            """);

        _migrationRunner.Processor.Execute("""
            INSERT INTO "UserRole" ("UserId", "RoleId")
            SELECT "UserId", "RoleId"
            FROM "UserRole_Migration"
            """);

        _migrationRunner.Processor.Execute("""
            DROP TABLE "UserRole_Migration"
            """);
    }

    private void CreateSqliteEdgeCaseObjects()
    {
        _migrationRunner.Processor.Execute("""
            CREATE TABLE IF NOT EXISTS "Computed Column" (
                "Id" INTEGER PRIMARY KEY,
                "First Name" TEXT NOT NULL,
                "Last Name" TEXT NOT NULL,
                "Full Name" TEXT GENERATED ALWAYS AS ("First Name" || ' ' || "Last Name") VIRTUAL,
                "Search Name" TEXT GENERATED ALWAYS AS (lower("First Name" || ' ' || "Last Name")) STORED
            )
            """);

        _migrationRunner.Processor.Execute("""
            CREATE INDEX IF NOT EXISTS "IX_Computed Column_Search Name_Active"
            ON "Computed Column" ("Search Name")
            WHERE "Last Name" <> ''
            """);

        _migrationRunner.Processor.Execute("""
            CREATE INDEX IF NOT EXISTS "IX_Computed Column_Lower_First Name"
            ON "Computed Column" (lower("First Name"))
            """);

        _migrationRunner.Processor.Execute("""
            CREATE VIEW IF NOT EXISTS "Active Users" AS
            SELECT "Id", "UserName", "EmailAddress"
            FROM "User"
            WHERE "IsDeleted" = 0
            """);

        _migrationRunner.Processor.Execute("""
            CREATE TABLE IF NOT EXISTS "UniqueConstraint" (
                "Id" INTEGER PRIMARY KEY,
                "TenantId" INTEGER NOT NULL,
                "Code" TEXT NOT NULL,
                "ExternalId" TEXT NOT NULL,
                UNIQUE ("Code"),
                UNIQUE ("TenantId", "ExternalId")
            )
            """);

        _migrationRunner.Processor.Execute("""
            CREATE TABLE IF NOT EXISTS "TriggerTarget" (
                "Id" INTEGER PRIMARY KEY,
                "Name" TEXT NOT NULL,
                "AuditCount" INTEGER NOT NULL DEFAULT 0
            )
            """);

        _migrationRunner.Processor.Execute("""
            CREATE TRIGGER IF NOT EXISTS "TR_TriggerTarget_AfterInsert"
            AFTER INSERT ON "TriggerTarget"
            FOR EACH ROW
            BEGIN
                SELECT 1;
            END
            """);

        _migrationRunner.Processor.Execute("""
            CREATE TRIGGER IF NOT EXISTS "TR_TriggerTarget_BeforeUpdate"
            BEFORE UPDATE ON "TriggerTarget"
            FOR EACH ROW
            BEGIN
                SELECT 1;
            END
            """);

        _migrationRunner.Processor.Execute("""
            CREATE TRIGGER IF NOT EXISTS "TR_TriggerTarget_AfterDelete"
            AFTER DELETE ON "TriggerTarget"
            FOR EACH ROW
            BEGIN
                SELECT 1;
            END
            """);

        _migrationRunner.Processor.Execute("""
            CREATE TABLE IF NOT EXISTS "PrimaryKeyTarget" (
                "Id" INTEGER PRIMARY KEY,
                "Code" TEXT NOT NULL
            )
            """);

        _migrationRunner.Processor.Execute("""
            CREATE TABLE IF NOT EXISTS "ImplicitForeignKey" (
                "Id" INTEGER PRIMARY KEY,
                "TargetId" INTEGER NOT NULL,
                FOREIGN KEY ("TargetId") REFERENCES "PrimaryKeyTarget"
            )
            """);

        _migrationRunner.Processor.Execute("""
            CREATE TABLE IF NOT EXISTS "CompositePrimaryKeyTarget" (
                "TenantId" INTEGER NOT NULL,
                "ExternalId" TEXT NOT NULL,
                "Name" TEXT NOT NULL,
                PRIMARY KEY ("TenantId", "ExternalId")
            )
            """);

        _migrationRunner.Processor.Execute("""
            CREATE TABLE IF NOT EXISTS "CompositeImplicitForeignKey" (
                "TenantId" INTEGER NOT NULL,
                "ExternalId" TEXT NOT NULL,
                FOREIGN KEY ("TenantId", "ExternalId") REFERENCES "CompositePrimaryKeyTarget"
            )
            """);
    }


    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}
