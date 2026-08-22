using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Friggy.Infrastructure.Persistence.Migrations;

/// <inheritdoc />
public partial class ReplaceWeeklyPlansWithDailyPlans : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            ALTER TABLE inventory_movements
                DROP CONSTRAINT "FK_inventory_movements_meal_plan_entries_meal_plan_entry_id";

            UPDATE inventory_movements
            SET meal_plan_entry_id = NULL
            WHERE meal_plan_entry_id IS NOT NULL;

            DROP TABLE meal_plan_entries;
            DROP TABLE meal_plan_slots;
            DROP TABLE weekly_plans;

            CREATE TABLE daily_plans (
                "Id" uuid NOT NULL,
                date date NOT NULL,
                CONSTRAINT "PK_daily_plans" PRIMARY KEY ("Id")
            );

            CREATE UNIQUE INDEX "IX_daily_plans_date"
                ON daily_plans (date);

            CREATE TABLE meal_plan_slots (
                "Id" uuid NOT NULL,
                daily_plan_id uuid NOT NULL,
                meal_type_id uuid NOT NULL,
                "order" integer NOT NULL,
                planned_time time without time zone NULL,
                CONSTRAINT "PK_meal_plan_slots" PRIMARY KEY ("Id"),
                CONSTRAINT "AK_meal_plan_slots_daily_plan_id_meal_type_id"
                    UNIQUE (daily_plan_id, meal_type_id),
                CONSTRAINT ck_meal_plan_slots_order_non_negative CHECK ("order" >= 0),
                CONSTRAINT "FK_meal_plan_slots_daily_plans_daily_plan_id"
                    FOREIGN KEY (daily_plan_id) REFERENCES daily_plans ("Id") ON DELETE CASCADE,
                CONSTRAINT "FK_meal_plan_slots_meal_types_meal_type_id"
                    FOREIGN KEY (meal_type_id) REFERENCES meal_types ("Id") ON DELETE RESTRICT
            );

            CREATE INDEX "IX_meal_plan_slots_meal_type_id"
                ON meal_plan_slots (meal_type_id);
            CREATE UNIQUE INDEX "IX_meal_plan_slots_daily_plan_id_order"
                ON meal_plan_slots (daily_plan_id, "order");

            CREATE TABLE meal_plan_entries (
                "Id" uuid NOT NULL,
                daily_plan_id uuid NOT NULL,
                meal_type_id uuid NOT NULL,
                recipe_id uuid NOT NULL,
                servings integer NOT NULL DEFAULT 1,
                status integer NOT NULL DEFAULT 0,
                completed_at timestamp with time zone NULL,
                skipped_reason text NULL,
                alternative_description text NULL,
                CONSTRAINT "PK_meal_plan_entries" PRIMARY KEY ("Id"),
                CONSTRAINT ck_meal_plan_entries_servings_positive CHECK (servings > 0),
                CONSTRAINT ck_meal_plan_entries_status_valid CHECK (status IN (0, 1, 2)),
                CONSTRAINT ck_meal_plan_entries_completion_state CHECK (
                    (status = 1 AND completed_at IS NOT NULL) OR
                    (status <> 1 AND completed_at IS NULL)),
                CONSTRAINT ck_meal_plan_entries_skipped_state CHECK (
                    (status = 2 AND skipped_reason IS NOT NULL AND btrim(skipped_reason) <> '') OR
                    (status <> 2 AND skipped_reason IS NULL AND alternative_description IS NULL)),
                CONSTRAINT "FK_meal_plan_entries_daily_plans_daily_plan_id"
                    FOREIGN KEY (daily_plan_id) REFERENCES daily_plans ("Id") ON DELETE CASCADE,
                CONSTRAINT "FK_meal_plan_entries_meal_plan_slots"
                    FOREIGN KEY (daily_plan_id, meal_type_id)
                    REFERENCES meal_plan_slots (daily_plan_id, meal_type_id),
                CONSTRAINT "FK_meal_plan_entries_meal_types_meal_type_id"
                    FOREIGN KEY (meal_type_id) REFERENCES meal_types ("Id") ON DELETE RESTRICT,
                CONSTRAINT "FK_meal_plan_entries_recipes_recipe_id"
                    FOREIGN KEY (recipe_id) REFERENCES recipes ("Id") ON DELETE RESTRICT
            );

            CREATE INDEX "IX_meal_plan_entries_meal_type_id"
                ON meal_plan_entries (meal_type_id);
            CREATE INDEX "IX_meal_plan_entries_recipe_id"
                ON meal_plan_entries (recipe_id);
            CREATE UNIQUE INDEX "IX_meal_plan_entries_daily_plan_id_meal_type_id"
                ON meal_plan_entries (daily_plan_id, meal_type_id);

            ALTER TABLE inventory_movements
                ADD CONSTRAINT "FK_inventory_movements_meal_plan_entries_meal_plan_entry_id"
                FOREIGN KEY (meal_plan_entry_id) REFERENCES meal_plan_entries ("Id") ON DELETE RESTRICT;
            """);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            ALTER TABLE inventory_movements
                DROP CONSTRAINT "FK_inventory_movements_meal_plan_entries_meal_plan_entry_id";

            UPDATE inventory_movements
            SET meal_plan_entry_id = NULL
            WHERE meal_plan_entry_id IS NOT NULL;

            DROP TABLE meal_plan_entries;
            DROP TABLE meal_plan_slots;
            DROP TABLE daily_plans;

            CREATE TABLE weekly_plans (
                "Id" uuid NOT NULL,
                name character varying(120) NOT NULL,
                normalized_name character varying(120) NOT NULL,
                start_date date NOT NULL,
                description text NULL,
                CONSTRAINT "PK_weekly_plans" PRIMARY KEY ("Id")
            );

            CREATE UNIQUE INDEX "IX_weekly_plans_normalized_name"
                ON weekly_plans (normalized_name);

            CREATE TABLE meal_plan_slots (
                "Id" uuid NOT NULL,
                weekly_plan_id uuid NOT NULL,
                date date NOT NULL,
                meal_type_id uuid NOT NULL,
                "order" integer NOT NULL,
                planned_time time without time zone NULL,
                CONSTRAINT "PK_meal_plan_slots" PRIMARY KEY ("Id"),
                CONSTRAINT "AK_meal_plan_slots_weekly_plan_id_date_meal_type_id"
                    UNIQUE (weekly_plan_id, date, meal_type_id),
                CONSTRAINT ck_meal_plan_slots_order_non_negative CHECK ("order" >= 0),
                CONSTRAINT "FK_meal_plan_slots_meal_types_meal_type_id"
                    FOREIGN KEY (meal_type_id) REFERENCES meal_types ("Id") ON DELETE RESTRICT,
                CONSTRAINT "FK_meal_plan_slots_weekly_plans_weekly_plan_id"
                    FOREIGN KEY (weekly_plan_id) REFERENCES weekly_plans ("Id") ON DELETE CASCADE
            );

            CREATE INDEX "IX_meal_plan_slots_meal_type_id"
                ON meal_plan_slots (meal_type_id);
            CREATE UNIQUE INDEX "IX_meal_plan_slots_weekly_plan_id_date_order"
                ON meal_plan_slots (weekly_plan_id, date, "order");

            CREATE TABLE meal_plan_entries (
                "Id" uuid NOT NULL,
                weekly_plan_id uuid NOT NULL,
                date date NOT NULL,
                meal_type_id uuid NOT NULL,
                recipe_id uuid NOT NULL,
                servings integer NOT NULL DEFAULT 1,
                status integer NOT NULL DEFAULT 0,
                completed_at timestamp with time zone NULL,
                skipped_reason text NULL,
                alternative_description text NULL,
                CONSTRAINT "PK_meal_plan_entries" PRIMARY KEY ("Id"),
                CONSTRAINT ck_meal_plan_entries_servings_positive CHECK (servings > 0),
                CONSTRAINT ck_meal_plan_entries_status_valid CHECK (status IN (0, 1, 2)),
                CONSTRAINT ck_meal_plan_entries_completion_state CHECK (
                    (status = 1 AND completed_at IS NOT NULL) OR
                    (status <> 1 AND completed_at IS NULL)),
                CONSTRAINT ck_meal_plan_entries_skipped_state CHECK (
                    (status = 2 AND skipped_reason IS NOT NULL AND btrim(skipped_reason) <> '') OR
                    (status <> 2 AND skipped_reason IS NULL AND alternative_description IS NULL)),
                CONSTRAINT "FK_meal_plan_entries_meal_plan_slots"
                    FOREIGN KEY (weekly_plan_id, date, meal_type_id)
                    REFERENCES meal_plan_slots (weekly_plan_id, date, meal_type_id),
                CONSTRAINT "FK_meal_plan_entries_meal_types_meal_type_id"
                    FOREIGN KEY (meal_type_id) REFERENCES meal_types ("Id") ON DELETE RESTRICT,
                CONSTRAINT "FK_meal_plan_entries_recipes_recipe_id"
                    FOREIGN KEY (recipe_id) REFERENCES recipes ("Id") ON DELETE RESTRICT,
                CONSTRAINT "FK_meal_plan_entries_weekly_plans_weekly_plan_id"
                    FOREIGN KEY (weekly_plan_id) REFERENCES weekly_plans ("Id") ON DELETE CASCADE
            );

            CREATE INDEX "IX_meal_plan_entries_meal_type_id"
                ON meal_plan_entries (meal_type_id);
            CREATE INDEX "IX_meal_plan_entries_recipe_id"
                ON meal_plan_entries (recipe_id);
            CREATE UNIQUE INDEX "IX_meal_plan_entries_weekly_plan_id_date_meal_type_id"
                ON meal_plan_entries (weekly_plan_id, date, meal_type_id);

            ALTER TABLE inventory_movements
                ADD CONSTRAINT "FK_inventory_movements_meal_plan_entries_meal_plan_entry_id"
                FOREIGN KEY (meal_plan_entry_id) REFERENCES meal_plan_entries ("Id") ON DELETE RESTRICT;
            """);
    }
}
