---
name: create-migration
description: Adds a new EF Core migration to the BankingService Infrastructure project. Use after modifying BankingDbContext or entity configurations — runs the correct dotnet ef command with proper project flags.
---

# Create Migration

Adds an EF Core migration to `src/BankingService.Infrastructure/Persistence/Migrations/`.

## Before running, verify

- `BankingDbContext` or an entity configuration has been updated
- The project builds cleanly (`dotnet build`)

## Command

Always run from the **solution root**:

```bash
dotnet ef migrations add {MigrationName} \
  --project src/BankingService.Infrastructure \
  --startup-project src/BankingService.Api \
  --output-dir Persistence/Migrations
```

## Naming conventions

| Change | Name |
|---|---|
| Initial schema | `InitialCreate` |
| Add column | `Add{Column}To{Table}` |
| Remove column | `Remove{Column}From{Table}` |
| Add table | `Add{Table}Table` |
| Add index | `Add{Index}IndexTo{Table}` |

## After running

1. Review the generated `Up()` — verify it matches the intended change
2. Review `Down()` — verify it correctly reverses the change
3. Check for data loss warnings (`DropColumn`, `DropTable`)
4. Run the app to confirm `Database.MigrateAsync()` applies cleanly

## Checklist

- [ ] Project builds before running the command
- [ ] Command run from solution root with both `--project` and `--startup-project`
- [ ] Generated `Up()` matches the intended schema change
- [ ] Generated `Down()` correctly reverses the change
- [ ] Migration name follows naming conventions
- [ ] App starts successfully after migration