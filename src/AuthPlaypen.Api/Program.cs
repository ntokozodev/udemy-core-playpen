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

        var certificate = new X509Certificate2(certPath, certPassword);
        options.AddSigningCertificate(certificate);
        return;
    }

    options.AddDevelopmentSigningCertificate();
}
