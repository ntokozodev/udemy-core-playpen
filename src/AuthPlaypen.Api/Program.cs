using AuthPlaypen.Api.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddAuthPlaypenApi(builder.Configuration);

var app = builder.Build();

app.MapAuthPlaypenInfrastructure();

app.Run();
