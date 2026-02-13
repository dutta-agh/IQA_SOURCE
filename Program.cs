using YourApp.Data;
using Microsoft.AspNetCore.HttpOverrides;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();

builder.Services.AddHttpContextAccessor();
builder.Services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();

builder.Services.AddHttpClient();

builder.Services.AddCors(policyBuilder =>
    policyBuilder.AddDefaultPolicy(policy =>
        policy.WithOrigins("*").AllowAnyHeader().AllowAnyMethod().WithExposedHeaders("Content-Disposition").WithExposedHeaders("File-Name"))
);
builder.Services.AddMemoryCache();

var dbOptions = new DbOptions();

builder.Configuration.GetSection("Db").Bind(dbOptions);
builder.Services.AddSingleton<IDbHelper>(new DbHelper(dbOptions));

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseForwardedHeaders(new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
});

app.UsePathBase("/IQA");

app.MapGet("/isalive", () =>
{
    return new
    {
        IsAlive = true,
        Environment = app.Environment.EnvironmentName
    };
});


app.Use((context, next) =>
{
    context.Request.PathBase = new PathString("/IQA");
    return next();
});
app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
