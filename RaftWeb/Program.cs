using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using RaftWeb;
using RaftWeb.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

var baseAddress = builder.HostEnvironment.IsDevelopment() || builder.HostEnvironment.BaseAddress.Contains("localhost")
    ? "http://localhost:8500"
    : "http://144.17.92.13:8500";

builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(baseAddress) });
builder.Services.AddScoped<RaftService>();
builder.Services.AddScoped<BalanceConverter>();
builder.Services.AddScoped<QuantityConverter>();

await builder.Build().RunAsync();
