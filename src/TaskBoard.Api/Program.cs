using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Cors.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using TaskBoard.Application.Auth;
using TaskBoard.Application.Tasks;
using TaskBoard.Api.Configuration;
using TaskBoard.Api.Http;
using TaskBoard.Infrastructure;
using TaskBoard.Infrastructure.Auth;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.Configure<ApiBehaviorOptions>(options =>
{
    options.InvalidModelStateResponseFactory = context =>
        new BadRequestObjectResult(ResultMapping.InvalidRequest(context.ModelState.Keys));
});
builder.Services.AddOpenApi();
builder.Services.AddOptions<AllowedOriginsOptions>()
    .Configure(options =>
        options.Origins = builder.Configuration.GetSection("AllowedOrigins").Get<string[]>() ?? [])
    .Validate(options => options.Origins.Length > 0, "AllowedOrigins must contain at least one origin.")
    .Validate(options => options.Origins.All(origin => Uri.TryCreate(origin, UriKind.Absolute, out _)),
        "AllowedOrigins contains an invalid absolute URI.")
    .ValidateOnStart();
builder.Services.AddCors();
builder.Services.AddOptions<CorsOptions>()
    .Configure<IOptions<AllowedOriginsOptions>>((options, allowedOrigins) =>
    {
        options.AddPolicy("Frontend", policy => policy
            .WithOrigins(allowedOrigins.Value.Origins)
            .AllowAnyHeader()
            .AllowAnyMethod());
    });
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
        options.Events = new JwtBearerEvents
        {
            OnChallenge = async context =>
            {
                context.HandleResponse();
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                await context.Response.WriteAsJsonAsync(
                    ResultMapping.Unauthorized("auth.unauthorized"),
                    options: null,
                    contentType: "application/problem+json",
                    cancellationToken: context.HttpContext.RequestAborted);
            }
        };
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

if (builder.Configuration.GetValue("HttpsRedirection:Enabled", true))
{
    app.UseHttpsRedirection();
}
app.UseCors("Frontend");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();

public partial class Program;
