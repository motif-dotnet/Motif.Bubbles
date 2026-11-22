using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Motif.Bubbles;
using Motif.Bubbles.Adapters;
using Motif.Bubbles.App;
using Motif.Suite;

// The composition root: the only place that knows which adapters are used.
var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// Each adapter is built over its Seam's real Fitting.
builder.Services.AddSingleton<ForOpeningRepos>(_ =>
    new ForOpeningReposViaGitHub(new HttpClientHandler()));
builder.Services.AddSingleton<JsInteropLocalFolders>();
builder.Services.AddSingleton<LocalFolders>(sp =>
    sp.GetRequiredService<JsInteropLocalFolders>());
builder.Services.AddSingleton<
    ForOpeningFolders, ForOpeningFoldersViaPickerAndDrop>();
builder.Services.AddSingleton<MotifBubblesApplication>();
builder.Services.AddSingleton<ForDoing>(sp =>
    sp.GetRequiredService<MotifBubblesApplication>());
builder.Services.AddSingleton<ForSeeing>(sp =>
    sp.GetRequiredService<MotifBubblesApplication>());
builder.Services.AddSingleton<Browser>();
builder.Services.AddSingleton<Toaster>();

var host = builder.Build();
await host.Services.GetRequiredService<JsInteropLocalFolders>().Listen();
await host.RunAsync();
