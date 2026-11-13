using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace AccessHub.BuildingBlocks.Infrastructure.Persistence;

/// <summary>Maps table, column, key and index names to snake_case, the Postgres convention.</summary>
public static class SnakeCaseNaming
{
    /// <summary>Renames every element of the model that still has its default (PascalCase) name.</summary>
    public static void Apply(ModelBuilder modelBuilder)
    {
        foreach (var entity in modelBuilder.Model.GetEntityTypes())
        {
            if (entity.GetTableName() is { } table && entity.GetViewName() is null)
            {
                entity.SetTableName(ToSnakeCase(table));
            }

            var storeObject = entity.GetTableName() is { } t ? StoreObjectIdentifier.Table(t, entity.GetSchema()) : (StoreObjectIdentifier?)null;
            foreach (var property in entity.GetProperties())
            {
                var name = storeObject is { } so ? property.GetColumnName(so) : property.GetColumnName();
                if (name is not null)
                {
                    property.SetColumnName(ToSnakeCase(name));
                }
            }

            foreach (var key in entity.GetKeys())
            {
                key.SetName(ToSnakeCase(key.GetName()!));
            }

            foreach (var foreignKey in entity.GetForeignKeys())
            {
                foreignKey.SetConstraintName(ToSnakeCase(foreignKey.GetConstraintName()!));
            }

            foreach (var index in entity.GetIndexes())
            {
                index.SetDatabaseName(ToSnakeCase(index.GetDatabaseName()!));
            }
        }
    }

    /// <summary>Converts PascalCase or camelCase to snake_case.</summary>
    public static string ToSnakeCase(string name)
    {
        if (IsSnakeCase(name))
        {
            return name;
        }

        var builder = new StringBuilder(name.Length + 8);
        for (var i = 0; i < name.Length; i++)
        {
            var c = name[i];
            if (char.IsUpper(c))
            {
                var startsWord = i > 0 && name[i - 1] != '_' &&
                    (char.IsLower(name[i - 1]) || char.IsDigit(name[i - 1]) || (i + 1 < name.Length && char.IsLower(name[i + 1])));
                if (startsWord)
                {
                    builder.Append('_');
                }

                builder.Append(char.ToLowerInvariant(c));
            }
            else
            {
                builder.Append(c);
            }
        }

        return builder.ToString();
    }

    private static bool IsSnakeCase(string name) => name.All(c => !char.IsUpper(c));
}
