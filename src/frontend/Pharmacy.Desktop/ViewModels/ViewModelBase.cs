using CommunityToolkit.Mvvm.ComponentModel;
using FluentValidation;
using Pharmacy.Desktop.Services;
using Pharmacy.Domain.Common;

namespace Pharmacy.Desktop.ViewModels;

public abstract partial class ViewModelBase : ObservableObject
{
    [ObservableProperty] private string _error = "";
    [ObservableProperty] private string _info = "";
    [ObservableProperty] private bool _isBusy;

    /// <summary>Ejecuta una acción mostrando los errores de negocio, validación o conexión en pantalla.</summary>
    protected async Task RunAsync(Func<Task> action, string? success = null)
    {
        Error = "";
        Info = "";
        IsBusy = true;
        try
        {
            await action();
            if (success is not null) Info = success;
        }
        catch (DomainException e) { Error = e.Message; }
        catch (ValidationException e) { Error = string.Join(" ", e.Errors.Select(x => x.ErrorMessage).Distinct()); }
        catch (ApiException e) { Error = e.Message; }
        catch (Exception e) when (e is HttpRequestException or TaskCanceledException)
        {
            Error = "Esta función necesita conexión con el servidor.";
        }
        finally { IsBusy = false; }
    }
}
