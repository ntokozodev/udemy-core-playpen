using AuthPlaypen.Api.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddAuthPlaypenApi(builder.Configuration, builder.Environment);

var app = builder.Build();

app.MapAuthPlaypenInfrastructure();

app.Run();
