using System.Text.Json;
using Schools.Api.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<JwtTokenIssuer>();
builder.Services
    .AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower;
        options.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
    });

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy
            .SetIsOriginAllowed(static origin =>
            {
                if (string.IsNullOrEmpty(origin))
                {
                    return false;
                }

                try
                {
                    var uri = new Uri(origin);
                    var h = uri.Host;
                    if (string.Equals(h, "localhost", StringComparison.OrdinalIgnoreCase)
                        || string.Equals(h, "127.0.0.1", StringComparison.OrdinalIgnoreCase))
                    {
                        return true;
                    }

                    if (h.EndsWith(".railway.app", StringComparison.OrdinalIgnoreCase))
                    {
                        return true;
                    }

                    return false;
                }
                catch
                {
                    return false;
                }
            })
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors();
app.MapControllers();

app.Run();
