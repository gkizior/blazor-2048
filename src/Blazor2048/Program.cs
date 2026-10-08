using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Blazor2048;
using Blazor2048.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddScoped(_ => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });
builder.Services.AddScoped<BrowserStorage>();
builder.Services.AddScoped<BestScoreStore>();
builder.Services.AddScoped<BoardSizeStore>();
builder.Services.AddScoped<ThemeService>();
builder.Services.AddScoped<IDocsSource, HttpDocsSource>();
builder.Services.AddSingleton(BuildInfo.FromAssembly(typeof(App).Assembly));
// Scoped = one game for the app's lifetime in WebAssembly, so it survives a trip to the docs.
// It starts at 4x4; GameBoard switches to the saved size on first load.
builder.Services.AddScoped(_ => new Game2048.Core.Game());

await builder.Build().RunAsync();
