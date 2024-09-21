using Server.GraphQL;

var builder = WebApplication.CreateBuilder(args);
builder
    .Services
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

builder.Services
    .AddGraphQLServer()
    .AddQueryType<Query>();

var app = builder.Build();
app.UseHttpsRedirection();
app.UseCors("DefaultPolicy");
app.MapGraphQL();
app.Run();
