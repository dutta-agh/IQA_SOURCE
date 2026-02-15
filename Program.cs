using YourApp.Data;
using Microsoft.AspNetCore.HttpOverrides;
using IQA_SOURCE.Middleware;
using IQA_SOURCE.Data;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();

builder.Services.AddHttpContextAccessor();
builder.Services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();

builder.Services.AddHttpClient();

// Add Session Support
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

builder.Services.AddCors(policyBuilder =>
    policyBuilder.AddDefaultPolicy(policy =>
        policy.WithOrigins("*").AllowAnyHeader().AllowAnyMethod().WithExposedHeaders("Content-Disposition").WithExposedHeaders("File-Name"))
);
builder.Services.AddMemoryCache();

var dbOptions = new DbOptions();

builder.Configuration.GetSection("Db").Bind(dbOptions);
builder.Services.AddSingleton<IDbHelper>(new DbHelper(dbOptions));

// Register Admin Repository
builder.Services.AddScoped<IAdminRepository, AdminRepository>();

var app = builder.Build();

// Configure for Linux reverse proxy (nginx/apache)
app.UseForwardedHeaders(new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
});

app.UsePathBase("/IQA");

// Add global exception handling middleware
app.UseMiddleware<GlobalExceptionHandlingMiddleware>();

if (!app.Environment.IsDevelopment())
{
    // Keep this as a fallback, but the middleware handles most cases
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

// Don't use HTTPS redirection when behind a reverse proxy
// app.UseHttpsRedirection();

app.UseStaticFiles();

app.UseRouting();

// Add Session Middleware
app.UseSession();

app.UseAuthorization();

app.MapGet("/isalive", () =>
{
    return new
    {
        IsAlive = true,
        Environment = app.Environment.EnvironmentName
    };
});

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
