using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Blazor2048;
using Blazor2048.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddScoped<BrowserStorage>();
builder.Services.AddScoped<BestScoreStore>();
builder.Services.AddScoped<ThemeService>();
builder.Services.AddSingleton(BuildInfo.FromAssembly(typeof(App).Assembly));
// Scoped = one game for the app's lifetime in WebAssembly.
builder.Services.AddScoped(_ => new Game2048.Core.Game());

await builder.Build().RunAsync();
