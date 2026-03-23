using AuthPlaypen.Api.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddAuthPlaypenApi(builder.Configuration);

var app = builder.Build();

app.MapAuthPlaypenInfrastructure();

app.Run();

static void ConfigureSigningCertificate(OpenIddictServerBuilder options, IConfiguration configuration)
{
    var certPath = configuration["OpenIddictSigningOptions:SigningCertificatePath"];
    var certPassword = configuration["OpenIddictSigningOptions:SigningCertificatePassword"];

    if (!string.IsNullOrWhiteSpace(certPath))
    {
        if (!Path.IsPathRooted(certPath))
        {
            certPath = Path.Combine(AppContext.BaseDirectory, certPath);
        }

        if (!File.Exists(certPath))
        {
            throw new InvalidOperationException($"OpenIddict signing certificate file not found: {certPath}");
        }


app.MapGet("/app-config", (IConfiguration config, IWebHostEnvironment environment) =>
{
    var useMockData = config.GetValue<bool>("AdminApp:UseMockData");
    var enableOidcAuth = config.GetValue<bool>("AdminApp:Oidc:EnableAuth");
    var authority = config["AdminApp:Oidc:Authority"];
    var clientId = config["AdminApp:Oidc:ClientId"];
    var redirectPath = config["AdminApp:Oidc:RedirectPath"];
    var postLogoutRedirectPath = config["AdminApp:Oidc:PostLogoutRedirectPath"];

    var useLocalMockDefaults = environment.IsDevelopment() && useMockData;

    if (useLocalMockDefaults)
    {
        authority = "https://localhost:5100";
        clientId = "gatekeeper-web-admin";
        redirectPath = "/auth/callback";
        postLogoutRedirectPath = "/";
    }

    options.AddDevelopmentSigningCertificate();
}
