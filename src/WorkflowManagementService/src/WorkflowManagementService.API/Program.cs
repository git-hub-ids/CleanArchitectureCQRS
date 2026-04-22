using Asp.Versioning;
using FluentValidation;
using MassTransit;
using MediatR;
using MediatR.Pipeline;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Scalar.AspNetCore;
using Serilog;
using StackExchange.Redis;
using WorkflowManagementService.API.Authorization;
using WorkflowManagementService.API.Middleware;
using WorkflowManagementService.Application.Common.Behaviors;
using WorkflowManagementService.Application.Common.Interfaces;
using WorkflowManagementService.Application.Features.LookupCategories.Commands.Create;
using WorkflowManagementService.Domain.Repositories;
using WorkflowManagementService.Infrastructure.Cache;
using WorkflowManagementService.Infrastructure.Identity;
using WorkflowManagementService.Infrastructure.Messaging.Consumers;
using WorkflowManagementService.Infrastructure.Persistence;
using WorkflowManagementService.Infrastructure.Persistence.Repositories;

var builder = WebApplication.CreateBuilder(args);

// ── Domain / Application ──────────────────────────────────────────────────────
builder.Services.AddMediatR(cfg =>
{
    cfg.RegisterServicesFromAssembly(typeof(CreateLookupCategoryCommand).Assembly);
    cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
    cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));
});

builder.Services.AddValidatorsFromAssembly(typeof(CreateLookupCategoryCommandValidator).Assembly);

// ── Infrastructure ────────────────────────────────────────────────────────────
builder.Services.AddDbContext<WorkflowDbContext>(opts =>
    opts.UseNpgsql(
        builder.Configuration.GetConnectionString("Postgres"),
        x => x.MigrationsHistoryTable("__EFMigrationsHistory", "workflow")));

builder.Services.AddSingleton<IConnectionMultiplexer>(_ =>
    ConnectionMultiplexer.Connect(builder.Configuration.GetConnectionString("Redis")!));
builder.Services.AddScoped<ICacheService, RedisCacheService>();

builder.Services.AddMassTransit(x =>
{
    x.AddConsumersFromNamespaceContaining<Placeholder>();
    x.UsingRabbitMq((ctx, cfg) =>
    {
        cfg.Host(
            builder.Configuration["RabbitMQ:Host"],
            builder.Configuration["RabbitMQ:VHost"],
            h =>
            {
                h.Username(builder.Configuration["RabbitMQ:Username"]!);
                h.Password(builder.Configuration["RabbitMQ:Password"]!);
            });

        cfg.ConfigureEndpoints(ctx);
    });
});

builder.Services.AddScoped<ILookupCategoryRepository, LookupCategoryRepository>();
builder.Services.AddScoped<ILookupItemRepository, LookupItemRepository>();

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();

// ── Auth ──────────────────────────────────────────────────────────────────────
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(opts =>
    {
        opts.Authority = builder.Configuration["IAM:Authority"];
        opts.Audience = builder.Configuration["IAM:Audience"];
        opts.RequireHttpsMetadata = !builder.Environment.IsDevelopment();
    });

builder.Services.AddScoped<IAuthorizationHandler, PermissionAuthorizationHandler>();
builder.Services.AddAuthorizationBuilder()
    .AddPolicy(PolicyNames.LookupRead,
        p => p.AddRequirements(new PermissionRequirement(Permissions.LookupRead)))
    .AddPolicy(PolicyNames.LookupCreate,
        p => p.AddRequirements(new PermissionRequirement(Permissions.LookupCreate)))
    .AddPolicy(PolicyNames.LookupUpdate,
        p => p.AddRequirements(new PermissionRequirement(Permissions.LookupUpdate)))
    .AddPolicy(PolicyNames.LookupDelete,
        p => p.AddRequirements(new PermissionRequirement(Permissions.LookupDelete)));

// ── API ───────────────────────────────────────────────────────────────────────
builder.Services.AddControllers();

builder.Services.AddApiVersioning(opts =>
{
    opts.DefaultApiVersion = new ApiVersion(1, 0);
    opts.AssumeDefaultVersionWhenUnspecified = true;
    opts.ReportApiVersions = true;
    opts.ApiVersionReader = new UrlSegmentApiVersionReader();
});

builder.Services.AddOpenApi();

// ── Logging ───────────────────────────────────────────────────────────────────
builder.Host.UseSerilog((ctx, cfg) => cfg
    .ReadFrom.Configuration(ctx.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .WriteTo.Seq(ctx.Configuration["Seq:Url"]!));

var app = builder.Build();

app.UseMiddleware<GlobalExceptionMiddleware>();

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();

app.MapOpenApi();
app.MapScalarApiReference();
app.MapControllers();

// Auto-migrate on startup (Dev only — use proper migration runner in Prod)
if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<WorkflowDbContext>();
    //await db.Database.MigrateAsync();
}

await app.RunAsync();
