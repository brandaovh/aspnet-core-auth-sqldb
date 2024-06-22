using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using ContactManager.Data;
using ContactManager.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Authorization;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
var connectionString = Environment.GetEnvironmentVariable("DB_CONNECTION_STRING") ?? throw new InvalidOperationException("Environment variable 'DB_CONNECTION_STRING' not found.");

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));
builder.Services.AddDatabaseDeveloperPageExceptionFilter();

// Add Role services to Identity
builder.Services.AddDefaultIdentity<IdentityUser>(
    options => options.SignIn.RequireConfirmedAccount = true)
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>();

builder.Services.AddRazorPages();

// Require authenticated users
builder.Services.AddControllers(config =>
{
    var policy = new AuthorizationPolicyBuilder()
                     .RequireAuthenticatedUser()
                     .Build();
    config.Filters.Add(new AuthorizeFilter(policy));
});

// Authorization handlers.
builder.Services.AddScoped<IAuthorizationHandler,
                      ContactIsOwnerAuthorizationHandler>();

builder.Services.AddSingleton<IAuthorizationHandler,
                      ContactAdministratorsAuthorizationHandler>();

builder.Services.AddSingleton<IAuthorizationHandler,
                      ContactManagerAuthorizationHandler>();

var app = builder.Build();

// Add requirement for secret password
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var context = services.GetRequiredService<ApplicationDbContext>();
    
    try
    {
        context.Database.Migrate();
        
        // Retrieve the secret from configuration
        var testUserPw = builder.Configuration.GetValue<string>("SEEDUSERPW");
        await SeedData.Initialize(services, testUserPw);
        
        // Log successful seed data initialization
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogInformation("Seed data initialized successfully.");
    }
    catch (Exception ex)
    {
        // Log any exceptions during seed data initialization
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred during seed data initialization.");
        throw; // Rethrow the exception to halt the application startup
    }
}

// using (var scope = app.Services.CreateScope())
// {
//     var services = scope.ServiceProvider;

//     await SeedData.Initialize(services);
// }

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.MapRazorPages();

app.Run();
