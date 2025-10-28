using Microsoft.AspNetCore.SignalR;
using Serilog;
using VitaWave.Data;
using VitaWave.WebAPI.Hubs;
using VitaWave.WebAPI.Notifications;

var builder = WebApplication.CreateBuilder(args);

Log.Logger = new LoggerConfiguration()
                    .MinimumLevel.Information()
                    .WriteTo.Console()
                    .CreateLogger();

// AddEvent services to the container.

builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials()
            .SetIsOriginAllowed(origin => true);
    });
});

builder.WebHost.ConfigureKestrel(options =>
{
    options.ListenAnyIP(5000); // expose on LAN, port 5000
});

builder.Services.AddSignalR();

builder.Services.AddSingleton<DataFacilitator>();
builder.Services.AddSingleton<NotificationHandler>();


var app = builder.Build();

var notifier = app.Services.GetRequiredService<NotificationHandler>(); // just to make this guy instantiate off rip, otherwise, won't print out logs or make settings file

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

//app.UseHttpsRedirection();

app.UseCors();

app.UseAuthorization();

app.MapControllers();

app.MapHub<ModuleHub>("/module");
app.MapHub<ChartHub>("/chart");

#if DEBUG
app.MapHub<DevHub>("/dev");
var devHub = app.Services.GetRequiredService<IHubContext<DevHub>>();

#endif

app.Run();
