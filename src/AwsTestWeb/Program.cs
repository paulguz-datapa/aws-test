using Amazon.SecretsManager;
using AwsTestWeb.Options;
using AwsTestWeb.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorPages();
builder.Services.AddSingleton<IAmazonSecretsManager>(_ => new AmazonSecretsManagerClient());
builder.Services.AddSingleton<IRdsAuthTokenProvider, AwsRdsAuthTokenProvider>();
builder.Services.AddScoped<IRdsProbeService, RdsProbeService>();
builder.Services.AddScoped<ISecretsProbeService, SecretsProbeService>();
builder.Services.AddSingleton(sp =>
{
    var configuration = sp.GetRequiredService<IConfiguration>();

    return new RdsOptions
    {
        Region = configuration["RDS_REGION"] ?? configuration["AWS_REGION"] ?? configuration["AWS_DEFAULT_REGION"],
        Host = configuration["RDS_HOST"],
        Port = int.TryParse(configuration["RDS_PORT"], out var port) ? port : 5432,
        Database = configuration["RDS_DATABASE"],
        Username = configuration["RDS_USERNAME"],
        Query = configuration["RDS_QUERY"] ?? "select id from dev.dashboards order by id limit 1",
        SslMode = configuration["RDS_SSL_MODE"] ?? "Require",
        RootCertificatePath = configuration["RDS_ROOT_CERTIFICATE"]
    };
});
builder.Services.AddSingleton(sp =>
{
    var configuration = sp.GetRequiredService<IConfiguration>();

    return new SecretOptions
    {
        SecretId = configuration["SECRET_ID"],
        KeyName = configuration["SECRET_JSON_KEY"] ?? "iPhone passcode"
    };
});

var app = builder.Build();

if (builder.Configuration.GetValue("DetailedErrors", true))
{
    app.UseDeveloperExceptionPage();
}
else
{
    app.UseExceptionHandler("/Error");
}

app.UseRouting();
app.UseAuthorization();

app.MapGet("/health", () => Results.Ok(new { status = "ok" }));
app.MapStaticAssets();
app.MapRazorPages()
   .WithStaticAssets();

app.Run();

public partial class Program;
