using Maf.DemoConnection;

var builder = WebApplication.CreateBuilder(new WebApplicationOptions
{
    Args = args,
    ContentRootPath = AppContext.BaseDirectory
});
builder.Configuration.AddDemoConfiguration(args);
if (builder.Configuration["urls"] is null) builder.WebHost.UseUrls("http://localhost:5089");
builder.Services.AddRazorPages();
var app = builder.Build();
app.UseStaticFiles();
app.MapRazorPages();
app.Run();
