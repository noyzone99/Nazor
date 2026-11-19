using DeviceRegistry;
using Microsoft.Extensions.DependencyInjection;
using Nazor;

var builder = WindowsApplication.CreateBuilder(args);

builder.Services.AddDeviceRegistry();

builder.Application.UseMainWindow<App>();

var app = builder.Build();
return app.Run();
