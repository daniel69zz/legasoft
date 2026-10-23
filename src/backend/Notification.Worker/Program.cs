using Notification.Worker;
using Serilog;

var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddSerilog(c => c.ReadFrom.Configuration(builder.Configuration)
    .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] Notification {Message:lj}{NewLine}{Exception}"));
builder.Services.AddSingleton(builder.Configuration.GetSection("Notification").Get<NotificationOptions>() ?? new());
builder.Services.AddHttpClient<AlertScheduler>();
builder.Services.AddHostedService(sp => sp.GetRequiredService<AlertScheduler>());

builder.Build().Run();
