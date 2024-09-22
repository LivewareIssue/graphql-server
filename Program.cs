using Microsoft.AspNetCore.Identity;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Server.Data;
using Server.Entities;
using Server.GraphQL;
using Server.Services;

var builder = WebApplication.CreateBuilder(args);
var services = builder.Services;

services
    .AddCors(
        options =>
        {
            options.AddPolicy("DefaultPolicy", builder =>
            {
                builder.AllowAnyHeader()
                    .WithMethods("GET", "POST")
                    .WithOrigins("http://localhost:8080");
            });
        }
    );

var connectionStringBuilder = new SqlConnectionStringBuilder(builder.Configuration.GetConnectionString("DefaultConnection"))
{
    Password = builder.Configuration["DatabasePassword"]
};

services.AddDbContextFactory<ApplicationDbContext>(options
    => options.UseSqlServer(connectionStringBuilder.ConnectionString));

services
    .AddTransient<IEntPersonService, EntPersonService>();

services
    .AddIdentity<EntPerson, IdentityRole>(options =>
    {
        options.Password.RequiredLength = 8;
    })
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();

builder.Services
    .AddGraphQLServer()
    .AddQueryType<Query>();

var app = builder.Build();

app.UseHttpsRedirection();
app.UseCors("DefaultPolicy");
app.MapGraphQL();

using var scope = app.Services.CreateScope();
var dbContextFactory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<ApplicationDbContext>>();
using var dbContext = dbContextFactory.CreateDbContext();

await dbContext.Database.MigrateAsync();

var userManager = scope.ServiceProvider.GetRequiredService<UserManager<EntPerson>>();
var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();

// Create Admin role if it doesn't exist
if (!await roleManager.RoleExistsAsync("Admin"))
{
    var adminRole = new IdentityRole("Admin");
    await roleManager.CreateAsync(adminRole);
}

// Create a test user if it doesn't exist
var testUser = await userManager.FindByEmailAsync("testuser@example.com");
if (testUser == null)
{
    testUser = new EntPerson
    {
        UserName = "testuser",
        Email = "testuser@example.com",
        EmailConfirmed = true
    };
    await userManager.CreateAsync(testUser, "Password123!");
}

// Add the test user to Admin role if not already in it
if (!await userManager.IsInRoleAsync(testUser, "Admin"))
{
    await userManager.AddToRoleAsync(testUser, "Admin");
}

var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
logger.LogInformation("User id: {id}", testUser.Id);

await app.RunAsync();
