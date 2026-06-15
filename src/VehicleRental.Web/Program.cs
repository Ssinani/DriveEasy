using Blazored.LocalStorage;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using VehicleRental.Web;
using VehicleRental.Web.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// Base HttpClient (NO handler)
builder.Services.AddScoped(sp =>
{
    return new HttpClient
    {
        BaseAddress = new Uri("https://localhost:56560/")
    };
});

builder.Services.AddBlazoredLocalStorage();
builder.Services.AddAuthorizationCore();

builder.Services.AddScoped<AuthenticationStateProvider, JwtAuthStateProvider>();
builder.Services.AddScoped<JwtAuthStateProvider>();

builder.Services.AddScoped<IAuthClientService, AuthClientService>();
builder.Services.AddScoped<IVehicleClientService, VehicleClientService>();
builder.Services.AddScoped<IReservationClientService, ReservationClientService>();
builder.Services.AddScoped<IUserClientService, UserClientService>();

await builder.Build().RunAsync();