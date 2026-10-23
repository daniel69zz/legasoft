using Microsoft.Extensions.DependencyInjection;
using Pharmacy.Desktop.Data;

namespace Pharmacy.Desktop.Services;

/// <summary>Ejecuta cada operación en su propio scope de DI, con un DbContext nuevo (una transacción por operación).</summary>
public sealed class Operations(IServiceScopeFactory scopes, SyncWorker worker)
{
    public async Task<TResult> RunAsync<THandler, TResult>(Func<THandler, Task<TResult>> action, bool changesData = false)
        where THandler : notnull
    {
        using var scope = scopes.CreateScope();
        var result = await action(scope.ServiceProvider.GetRequiredService<THandler>());
        if (changesData) worker.Wake();
        return result;
    }

    public async Task RunAsync<THandler>(Func<THandler, Task> action, bool changesData = false) where THandler : notnull
    {
        using var scope = scopes.CreateScope();
        await action(scope.ServiceProvider.GetRequiredService<THandler>());
        if (changesData) worker.Wake();
    }

    public Task<TResult> QueryAsync<TResult>(Func<LocalDb, Task<TResult>> query) => RunAsync(query);
}
