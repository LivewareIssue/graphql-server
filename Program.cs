using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Server;
using Server.Data;
using Server.Entities;
using Server.GraphQL;
using Server.Services;

var builder = WebApplication
    .CreateBuilder(args)
    .AddAuthentication()
    .AddDatabase()
    .AddServer();

builder.Services.AddScoped<TaskService>();
builder.Services.AddScoped<CommentService>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSimulatedLatency(
        min: TimeSpan.FromMilliseconds(500),
        max: TimeSpan.FromMilliseconds(1500)
    );
}

app
    .UseHttpsRedirection()
    .UseCors("DefaultPolicy")
    .UseRouting()
    .UseAuthentication()
    .UseAuthorization();

app.MapGraphQL();

await ApplyMigrations();
await SeedDatabase();
await app.RunAsync();

async Task SeedDatabase()
{
    using var scope = app.Services.CreateScope();

    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<EntUser>>();
    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();

    var testUser = await userManager.FindByEmailAsync("testuser@example.com");
    if (testUser is null)
    {
        testUser = new EntUser
        {
            UserName = "TestUser",
            Email = "testuser@example.com"
        };
        
        var result = await userManager.CreateAsync(testUser, "Password123!");
    }

    var testAdmin = await userManager.FindByEmailAsync("admin@example.com");
    if (testAdmin is null)
    {
        testAdmin = new EntUser
        {
            UserName = "Admin",
            Email = "admin@example.com"
        };

        await userManager.CreateAsync(testAdmin, "Password123!");
    }

    var userRole = await roleManager.FindByNameAsync("User");
    if (userRole is null)
    {
        userRole = new IdentityRole("User");
        await roleManager.CreateAsync(userRole);
    }

    var adminRole = await roleManager.FindByNameAsync("Admin");
    if (adminRole is null)
    {
        adminRole = new IdentityRole("Admin");
        await roleManager.CreateAsync(adminRole);
    }

    if (!await userManager.IsInRoleAsync(testUser, "User"))
    {
        await userManager.AddToRoleAsync(testUser, "User");
    }

    if (!await userManager.IsInRoleAsync(testAdmin, "Admin"))
    {
        await userManager.AddToRoleAsync(testAdmin, "Admin");
    }

    var commentService = scope.ServiceProvider.GetRequiredService<CommentService>();
    var comment = await commentService.QueryAll().SingleOrDefaultAsync();
    if (comment is null)
    {
        comment = new EntComment
        {
            Content = "Comment 1",
            AuthorId = testUser.Id,
            Author = testUser,
        };

        comment = await commentService.CreateAsync(comment);
    }

    var taskService = scope.ServiceProvider.GetRequiredService<TaskService>();

    var statuses = Enum.GetValues<Server.Entities.TaskStatus>();
    var sizes = Enum.GetValues<TaskSize>();
    var priorities = Enum.GetValues<TaskPriority>();

    var random = new Random();

    TaskSize getRandomSize()
    {
        return sizes[random.Next(0, sizes.Length)];
    }

    TaskPriority getRandomPriority()
    {
        return priorities[random.Next(0, priorities.Length)];
    }

    Server.Entities.TaskStatus getRandomStatus()
    {
        return statuses[random.Next(0, statuses.Length)];
    }

    var templates = new List<string>
    {
        "Merge {0} into {1}",
        "Add {0} functionality to {1}",
        "Fix bug in {0}",
        "Refactor {0} module for {1}",
        "Implement {0} for {1} integration",
        "Optimize {0} performance in {1}",
        "Remove deprecated {0} in {1}",
        "Update {0} configuration for {1}"
    };

    var techWords = new List<string>
    {
        "API", "Service", "Component", "Module", "Functionality", "Pipeline", 
        "Cache", "Session", "Handler", "Widget", "Adapter", "Manager", "Resolver",
        "Controller", "Client", "Repository"
    };

    string getRandomTaskTitle()
    {
        string template = templates[random.Next(templates.Count)];
        string x = techWords[random.Next(techWords.Count)];
        string y = techWords[random.Next(techWords.Count)];
        return string.Format(template, x, y);
    }

    var owners = new List<EntUser> { testUser, testAdmin };

    EntUser getRandomOwner()
    {
        return owners[random.Next(owners.Count)];
    }

    for (var i = 0; i < 100; i++)
    {
        var task = new EntTask
        {
            Title = getRandomTaskTitle(),
            Content = "Lorem ipsum dolor sit amet, consectetur adipiscing elit.",
            Status = getRandomStatus(),
            Size = getRandomSize(),
            Priority = getRandomPriority(),
            Owner = getRandomOwner()
        };

        await taskService.CreateAsync(task);
    }
}

async Task ApplyMigrations()
{
    using var scope = app.Services.CreateScope();

    var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    await dbContext.Database.MigrateAsync();

    var tableNames = dbContext
        .Model
        .GetEntityTypes()
        .Select(entityType => entityType.GetTableName())
        .Distinct()
        .ToList();
    
    foreach (var tableName in tableNames)
    {
        dbContext.Database.ExecuteSqlRaw($"DELETE FROM [{tableName}]");
    }
}

namespace Server
{
    public static class WebApplicationBuilderExtensions
    {
        public static WebApplicationBuilder AddAuthentication(this WebApplicationBuilder builder)
        {
            builder.Configuration.AddUserSecrets<Program>();

            builder.Services.Configure<JwtOptions>(builder.Configuration.GetSection("Jwt"));
            var jwtOptions = builder.Configuration.GetSection("Jwt").Get<JwtOptions>()!;

            builder.Services
                .AddAuthentication(options =>
                {
                    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
                })
                .AddJwtBearer(options =>
                {
                    options.TokenValidationParameters =
                        new TokenValidationParameters
                        {
                            ValidIssuer = jwtOptions.ValidIssuer,
                            ValidAudience = jwtOptions.ValidAudience,
                            ValidateIssuerSigningKey = true,
                            IssuerSigningKey = AuthenticationService.GetSecurityKey(jwtOptions),
                        };
                });

            builder.Services
                .AddIdentityCore<EntUser>(options =>
                {
                    options.Password.RequiredLength = 8;
                })
                .AddRoles<IdentityRole>()
                .AddEntityFrameworkStores<ApplicationDbContext>()
                .AddDefaultTokenProviders();
            
            builder.Services.AddScoped<AuthenticationService>();

            return builder;
        }

        public static WebApplicationBuilder AddDatabase(this WebApplicationBuilder builder)
        {
            var connectionStringBuilder = new SqlConnectionStringBuilder(builder.Configuration.GetConnectionString("DefaultConnection"))
            {
                Password = builder.Configuration["DatabasePassword"]
            };

            builder.Services.AddDbContextPool<ApplicationDbContext>(
                options => options
                    .UseSqlServer(connectionStringBuilder.ConnectionString)
                    .UseProjectables()
            );

            return builder;
        }

        public static WebApplicationBuilder AddServer(this WebApplicationBuilder builder)
        {
            builder.Services
                .AddCors(
                    options =>
                    {
                        options.AddPolicy("DefaultPolicy", builder =>
                        {
                            builder
                                .AllowAnyHeader()
                                .AllowAnyMethod()
                                .AllowAnyOrigin();
                        });
                    }
                ).AddHttpContextAccessor();

            builder.Services
                .AddGraphQLServer()
                .ModifyOptions(options =>
                {
                    options.EnableDefer = true;
                })
                .ModifyRequestOptions(options =>
                {
                    options.IncludeExceptionDetails = true;
                })
                .AddGlobalObjectIdentification()
                // .RegisterDbContextFactory<ApplicationDbContext>()
                .AddQueryFieldToMutationPayloads()
                .AddAuthorization(options =>
                {
                    options.AddPolicy("AdminOrCanViewOwnData", policy =>
                    {
                        policy.RequireAssertion(context =>
                        {
                            return true;
                        });
                    });
                })
                .AddTypeExtension<EntUserTypeExtension>()
                .AddProjections()
                .AddQueryType<Query>()
                .AddMutationType<Mutation>();
            
            return builder;
        }

        public static IApplicationBuilder UseSimulatedLatency(
            this IApplicationBuilder app,
            TimeSpan min,
            TimeSpan max
        )
        {
            return app.UseMiddleware(typeof(SimulatedLatencyMiddleware));
        }
    }

    public class SimulatedLatencyMiddleware(RequestDelegate next)
    {
        private readonly RequestDelegate _next = next;
        private readonly ThreadLocal<Random> _random = new (() => new Random());

        public async Task Invoke(HttpContext context)
        {
            context.Request.EnableBuffering();

            using var reader = new StreamReader(context.Request.Body);
            var body = await reader.ReadToEndAsync();
            if (!string.IsNullOrWhiteSpace(body))
            {
                var json = JsonSerializer.Deserialize<JsonObject>(body);
                if (json?.TryGetPropertyValue("operationName", out var operationName) ?? false)
                {
                    if (operationName is not null)
                    {
                        if (operationName.GetValue<string>() != "SideNavQuery")
                        {
                            var delay = _random.Value?.Next(
                                800,
                                1400
                            );

                            if (delay is not null)
                            {
                                await Task.Delay((int)delay);
                            }
                        }
                        else
                        {
                            var delay = _random.Value?.Next(
                                200,
                                400
                            );

                            if (delay is not null)
                            {
                                await Task.Delay((int)delay);
                            }
                        }
                    }
                }
            }

            context.Request.Body.Position = 0;            

            await _next(context);
        }
    }
}