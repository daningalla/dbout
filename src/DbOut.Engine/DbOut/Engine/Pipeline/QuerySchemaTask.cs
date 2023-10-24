using DbOut.Metadata;
using DbOut.Options;
using DbOut.Services;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace DbOut.Engine.Pipeline;

public class QuerySchemaTask : CommandTask
{
    private readonly IRuntimeServices _runtimeServices;
    private readonly IMemoryCache _memoryCache;

    /// <inheritdoc />
    public QuerySchemaTask(ILogger<QuerySchemaTask> logger,
        IRuntimeServices runtimeServices,
        IMemoryCache memoryCache) 
        : base(logger, new[] { CommandMode.Execute, CommandMode.GetSchema })
    {
        _runtimeServices = runtimeServices;
        _memoryCache = memoryCache;
    }

    /// <inheritdoc />
    protected override async Task InvokeCoreAsync(JobContext context, CancellationToken cancellationToken)
    {
        var connectionContext = _runtimeServices.ConnectionContext;
        var datasource = RuntimeOptions.ThrowIfNullReference("Options.DataSource", context.Options.DataSource);
        var selectMode = datasource.SelectColumns is { Length: > 0 } ? "runtime option" : "default";
        var excludeMode = datasource.ExcludedColumns is { Length: > 0 } ? "runtime option" : "default";
        
        var selectionPredicate = new Predicate<string>(columnName =>
        {
            var select = (datasource.SelectColumns is { Length: > 0 } && datasource.SelectColumns.Contains(columnName))
                         || datasource.SelectColumns == null || datasource.SelectColumns.Length == 0;
            var exclude = datasource.ExcludedColumns is { Length: > 0 } &&
                          datasource.ExcludedColumns.Contains(columnName);
            var result = select && !exclude;
            
            Logger.LogTrace("Select column {name} (select={selectMode}/exclude={excludeMode})={result}",
                columnName,
                selectMode,
                excludeMode,
                result);

            return result;
        });
        
        var schema = await connectionContext.GetColumnSchemaAsync(selectionPredicate, cancellationToken);

        foreach (var metadata in schema)
        {
            if (Logger.IsEnabled(LogLevel.Trace))
            {
                Logger.LogTrace("Column metadata {name} = ordinal={ordinal}, converter={converter}",
                    metadata.ColumnName,
                    metadata.OrdinalPosition,
                    metadata.ValueConverter);
            }
            else if (Logger.IsEnabled(LogLevel.Debug))
            {
                Logger.LogDebug("Column metadata {name} = ordinal={ordinal}, data type={converter}",
                    metadata.ColumnName,
                    metadata.OrdinalPosition,
                    metadata.AnnotatedDataType ?? metadata.DataType);
            }
        }
        
        Logger.LogInformation("Retrieved column schema (columns={count})", schema.Count);

        if (context.Options.CommandMode == CommandMode.GetSchema)
        {
            foreach (var metadata in schema)
            {
                Logger.LogInformation("Column {name}: ordinal={ordinal}, nullable={nullable}, index={index}, value type={converter}",
                    metadata.ColumnName,
                    metadata.OrdinalPosition,
                    metadata.IsNullable,
                    metadata.IndexType,
                    metadata.ValueConverter);
            }
        }

        _memoryCache.Set(nameof(ColumnSchema), schema);
    }
}