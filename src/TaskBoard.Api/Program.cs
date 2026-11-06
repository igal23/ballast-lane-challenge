using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using TaskBoard.Application.Auth;
using TaskBoard.Application.Tasks;
using TaskBoard.Infrastructure;
using TaskBoard.Infrastructure.Auth;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddOpenApi();
builder.Services.AddTaskBoardInfrastructure(builder.Configuration);
builder.Services.AddScoped<RegisterUser>();
builder.Services.AddScoped<LoginUser>();
builder.Services.AddScoped<GetCurrentUser>();
builder.Services.AddScoped<CreateTask>();
builder.Services.AddScoped<ListTasks>();
builder.Services.AddScoped<GetTask>();
builder.Services.AddScoped<ReplaceTask>();
builder.Services.AddScoped<DeleteTask>();

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer();
builder.Services
    .AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
    .Configure<IOptions<JwtOptions>, TimeProvider>((options, jwtOptions, timeProvider) =>
    {
        var jwt = jwtOptions.Value;
        options.MapInboundClaims = false;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwt.Issuer,
            ValidateAudience = true,
            ValidAudience = jwt.Audience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.SigningKey)),
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromSeconds(30),
            LifetimeValidator = (notBefore, expires, _, parameters) =>
            {
                if (expires is null)
                {
                    return false;
                }

                var utcNow = timeProvider.GetUtcNow().UtcDateTime;
                return (notBefore is null || notBefore <= utcNow + parameters.ClockSkew)
                    && expires >= utcNow - parameters.ClockSkew;
            }
        };
    });

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();

public partial class Program;
