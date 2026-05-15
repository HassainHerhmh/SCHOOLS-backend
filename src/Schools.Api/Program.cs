using System.Net;
using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);

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
            .SetIsOriginAllowed(origin =>
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

                    var hostForIp = h;
                    if (hostForIp.Length >= 2 && hostForIp[0] == '[' && hostForIp[^1] == ']')
                    {
                        hostForIp = hostForIp.Substring(1, hostForIp.Length - 2);
                    }

                    if (IPAddress.TryParse(hostForIp, out var addr))
                    {
                        if (IPAddress.IsLoopback(addr))
                        {
                            return true;
                        }

                        if (addr.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                        {
                            var b = addr.GetAddressBytes();
                            if (b[0] == 10)
                            {
                                return true;
                            }

                            if (b[0] == 172 && b[1] >= 16 && b[1] <= 31)
                            {
                                return true;
                            }

                            if (b[0] == 192 && b[1] == 168)
                            {
                                return true;
                            }
                        }
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
