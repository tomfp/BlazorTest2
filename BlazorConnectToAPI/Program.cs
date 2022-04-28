using System.Text;
using BlazorConnectToAPI;
using FP.MissingLink;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

var config = builder.Configuration;
var currentConfig = config["currentConfig"];
var apiBase = config[$"{currentConfig}:TypedClient:ApiUrl"];
var apiVer = config[$"{currentConfig}:TypedClient:ApiVersion"];
builder.Logging.AddConfiguration(
    builder.Configuration.GetSection("Logging"));
builder.Services.AddLogging();

builder.Services.Configure<HttpClientSettings>(
    config.GetSection($"{currentConfig}:TypedClient"));
builder.Services.AddSingleton<ITypedClientConfig, TypedClientConfig>();

builder.Services.AddHttpClient<ICaseDataService, CaseDataService>(client =>
    {
        client.BaseAddress = new Uri(apiBase);
    })
    .ConfigureHttpClient(ConfigureHttpClient);


builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });

await builder.Build().RunAsync();


static void ConfigureHttpClient(HttpClient httpClient)
{
    var userName = @"FOOTPRINT\Tom.Brown";
    // this simply passes the current windows user name to the API on each request
    var basicCreds = Convert.ToBase64String(Encoding.ASCII.GetBytes(userName + ":doesn't matter"));
    httpClient.Timeout = TimeSpan.FromSeconds(120);
    httpClient.DefaultRequestHeaders.Add("Authorization", "Basic " + basicCreds);
    httpClient.DefaultRequestHeaders.Add("X-Version", "2.0");
}
