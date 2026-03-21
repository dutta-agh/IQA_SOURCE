using IQA_SOURCE.Data;
using IQA_SOURCE.Services;
using IQA_SOURCE.Models.Admin;
using YourApp.Data;
using Microsoft.AspNetCore.HttpOverrides;
using IQA_SOURCE.Middleware;

var builder = WebApplication.CreateBuilder(args);

// Initialize Constants from appsettings
IQA_SOURCE.Constants.Initialize(builder.Configuration);

builder.Services.AddControllersWithViews()
    .AddJsonOptions(options =>
    {

    });

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
    options.Cookie.Name = ".IQA.Session";
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
});

builder.Services.AddCors(policyBuilder =>
    policyBuilder.AddDefaultPolicy(policy =>
        policy.WithOrigins("*").AllowAnyHeader().AllowAnyMethod().WithExposedHeaders("Content-Disposition").WithExposedHeaders("File-Name"))
);
builder.Services.AddMemoryCache();

// Register DbOptions as a singleton so it can be injected
var dbOptions = new DbOptions();
builder.Configuration.GetSection("Db").Bind(dbOptions);
builder.Services.AddSingleton(dbOptions);

// Register DbHelper as scoped (not both singleton and scoped)
builder.Services.AddScoped<IDbHelper, DbHelper>();

// Configure Image Storage Settings
builder.Services.Configure<ImageStorageSettings>(
    builder.Configuration.GetSection("ImageStorage"));

// Register repositories
builder.Services.AddScoped<IAdminRepository, AdminRepository>();
builder.Services.AddScoped<IAdminUserRepository, AdminUserRepository>();
builder.Services.AddScoped<IAdminMenuRepository, AdminMenuRepository>();
builder.Services.AddScoped<IAssessmentTypeRepository, AssessmentTypeRepository>();
builder.Services.AddScoped<IQuestionMasterRepository, QuestionMasterRepository>();
builder.Services.AddScoped<IImageRepository, ImageRepository>();
builder.Services.AddScoped<IDashboardRepository, DashboardRepository>();
builder.Services.AddScoped<IActionLogRepository, ActionLogRepository>();
builder.Services.AddScoped<ISpeedTestRepository, SpeedTestRepository>();
builder.Services.AddScoped<IUserResponseRepository, UserResponseRepository>();
builder.Services.AddScoped<IQuestionAnswerRepository, QuestionAnswerRepository>();
builder.Services.AddScoped<IImageQualityRepository, ImageQualityRepository>();
builder.Services.AddScoped<ISystemCheckParamRepository, SystemCheckParamRepository>();
builder.Services.AddScoped<IImageGroupRepository, ImageGroupRepository>();
builder.Services.AddScoped<IBulkOperationsRepository, BulkOperationsRepository>();
builder.Services.AddScoped<IColorblindnessRepository, ColorblindnessRepository>();

// Register services
builder.Services.AddScoped<IImageMetadataService, ImageMetadataService>();
builder.Services.AddScoped<ISessionService, SessionService>();

builder.Services.Configure<Microsoft.AspNetCore.Http.Features.FormOptions>(options =>
{
    options.ValueCountLimit          = 100000;
    options.MultipartBodyLengthLimit = 2147483648; // 2 GB
});

builder.WebHost.ConfigureKestrel(options =>
{
    options.Limits.MaxRequestBodySize = 2147483648; // 2 GB
});

var app = builder.Build();

// Configure for Linux reverse proxy (nginx/apache)
app.UseForwardedHeaders(new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
});

// MUST come before UseRouting — tells ASP.NET Core about the /IQA/ sub-path
var subAppPath = builder.Configuration.GetValue<string>("AppSettings:SubApplicationPath")?.TrimEnd('/') ?? "";
if (!string.IsNullOrWhiteSpace(subAppPath))
{
    app.UsePathBase(subAppPath);
}

// Add global exception handling middleware
app.UseMiddleware<GlobalExceptionHandlingMiddleware>();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseStaticFiles(); // Default wwwroot

// Serve images from Linux server path
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new Microsoft.Extensions.FileProviders.PhysicalFileProvider(
        builder.Configuration.GetValue<string>("ImageStorage:BasePath")),
    RequestPath = builder.Configuration.GetValue<string>("ImageStorage:WebBasePath")
});

app.UseRouting();

// Add Session Middleware
app.UseSession();

app.UseMiddleware<ActionLoggingMiddleware>();

app.UseAuthorization();

app.MapGet("/isalive", () =>
{
    return new
    {
        IsAlive = true,
        Environment = app.Environment.EnvironmentName
    };
});

// ── Assessment-specific routes FIRST (most specific routes first) ──────────
app.MapControllerRoute( 
    name: "assessmentGetIntroContent",
    pattern: "Assessment/GetIntroContent",
    defaults: new { controller = "Assessment", action = "GetIntroContent" });

app.MapControllerRoute(
    name: "assessmentGetImageAssessmentIntro",
    pattern: "Assessment/GetImageAssessmentIntro",
    defaults: new { controller = "Assessment", action = "GetImageAssessmentIntro" });

app.MapControllerRoute(
    name: "assessmentSaveSpeedTestLog",
    pattern: "Assessment/SaveSpeedTestLog",
    defaults: new { controller = "Assessment", action = "SaveSpeedTestLog" });

app.MapControllerRoute(
    name: "assessmentSubmitResponses",
    pattern: "Assessment/SubmitResponses",
    defaults: new { controller = "Assessment", action = "SubmitResponses" });

app.MapControllerRoute(
    name: "assessmentGetSessionInfo",
    pattern: "Assessment/GetSessionInfo",
    defaults: new { controller = "Assessment", action = "GetSessionInfo" });

app.MapControllerRoute(
    name: "assessmentGetAssessmentSettings",
    pattern: "Assessment/GetAssessmentSettings",
    defaults: new { controller = "Assessment", action = "GetAssessmentSettings" });

// Image Assessment routes
app.MapControllerRoute(
    name: "assessmentImageAssessmentWithCode",
    pattern: "Assessment/{assessmentCode}/ImageAssessment",
    defaults: new { controller = "Assessment", action = "ImageAssessment" });

app.MapControllerRoute(
    name: "assessmentImageAssessment",
    pattern: "Assessment/ImageAssessment/{assessmentCode?}",
    defaults: new { controller = "Assessment", action = "ImageAssessment" });

app.MapControllerRoute(
    name: "assessmentSubmitImageQualityRating",
    pattern: "Assessment/SubmitImageQualityRating",
    defaults: new { controller = "Assessment", action = "SubmitImageQualityRating" });

app.MapControllerRoute(
    name: "assessmentGetImageAssessmentProgress",
    pattern: "Assessment/GetImageAssessmentProgress",
    defaults: new { controller = "Assessment", action = "GetImageAssessmentProgress" });

// ── Sort Assessment routes ─────────────────────────────────────────────────
app.MapControllerRoute(
    name: "assessmentSortAssessmentWithCode",
    pattern: "Assessment/{assessmentCode}/SortAssessment",
    defaults: new { controller = "Assessment", action = "SortAssessment" });

app.MapControllerRoute(
    name: "assessmentSortAssessment",
    pattern: "Assessment/SortAssessment/{assessmentCode?}",
    defaults: new { controller = "Assessment", action = "SortAssessment" });

app.MapControllerRoute(
    name: "assessmentSubmitSortRatings",
    pattern: "Assessment/SubmitSortRatings",
    defaults: new { controller = "Assessment", action = "SubmitSortRatings" });
// ──────────────────────────────────────────────────────────────────────────

// Questions routes
app.MapControllerRoute(
    name: "assessmentQuestionsWithCode",
    pattern: "Assessment/{assessmentCode}/Questions",
    defaults: new { controller = "Assessment", action = "Questions" });

app.MapControllerRoute(
    name: "assessmentQuestions",
    pattern: "Assessment/Questions/{assessmentCode}",
    defaults: new { controller = "Assessment", action = "Questions" });

app.MapControllerRoute(
    name: "assessmentSpeedTest",
    pattern: "Assessment/{assessmentType}/SpeedTest",
    defaults: new { controller = "Assessment", action = "SpeedTest" });

app.MapControllerRoute(
    name: "assessmentIndex",
    pattern: "Assessment/{assessmentType}",
    defaults: new { controller = "Assessment", action = "Index" });

// ── Add explicit download routes BEFORE short URL routes ────────────────────
app.MapControllerRoute(
    name: "adminDownloadColorblindnessResultsExcel",
    pattern: "Admin/DownloadColorblindnessResultsExcel",
    defaults: new { controller = "Admin", action = "DownloadColorblindnessResultsExcel" });

app.MapControllerRoute(
    name: "adminDownloadSpeedTestLogsExcel",
    pattern: "Admin/DownloadSpeedTestLogsExcel",
    defaults: new { controller = "Admin", action = "DownloadSpeedTestLogsExcel" });

app.MapControllerRoute(
    name: "adminDownloadQuestionAnswersExcel",
    pattern: "Admin/DownloadQuestionAnswersExcel",
    defaults: new { controller = "Admin", action = "DownloadQuestionAnswersExcel" });

// Short URL format routes (MUST be after all specific routes)
app.MapControllerRoute(
    name: "assessmentShortImageAssessment",
    pattern: "{assessmentType}/ImageAssessment",
    defaults: new { controller = "Assessment", action = "ImageAssessment", assessmentCode = "" },
    constraints: new { assessmentType = "^(?!Admin|Account|api|Download).*$" });

app.MapControllerRoute(
    name: "assessmentShortSortAssessment",
    pattern: "{assessmentType}/SortAssessment",
    defaults: new { controller = "Assessment", action = "SortAssessment", assessmentCode = "" },
    constraints: new { assessmentType = "^(?!Admin|Account|api|Download).*$" });

app.MapControllerRoute(
    name: "assessmentShortSpeedTest",
    pattern: "{assessmentType}/SpeedTest",
    defaults: new { controller = "Assessment", action = "SpeedTest" },
    constraints: new { assessmentType = "^(?!Admin|Account|api|Download).*$" });

app.MapControllerRoute(
    name: "assessmentShortQuestions",
    pattern: "{assessmentType}/Questions",
    defaults: new { controller = "Assessment", action = "Questions", assessmentCode = "" },
    constraints: new { assessmentType = "^(?!Admin|Account|api|Download).*$" });

app.MapControllerRoute(
    name: "assessmentShortIndex",
    pattern: "{assessmentType}",
    defaults: new { controller = "Assessment", action = "Index" },
    constraints: new { assessmentType = "^(?!Admin|Account|api|Download).*$" });

app.MapControllerRoute(
    name: "assessmentColorblindnessTest",
    pattern: "Assessment/{assessmentType}/ColorblindnessTest",
    defaults: new { controller = "Assessment", action = "ColorblindnessTest" });

app.MapControllerRoute(
    name: "assessmentColorblindnessTestShort",
    pattern: "{assessmentType}/ColorblindnessTest",
    defaults: new { controller = "Assessment", action = "ColorblindnessTest" },
    constraints: new { assessmentType = "^(?!Admin|Account|api).*$" });

// ── Colorblindness Test Images routes ──────────────────────────────────────
app.MapControllerRoute(
    name: "assessmentGetColorblindnessImagesForTestWithType",
    pattern: "Assessment/{assessmentType}/GetColorblindnessImagesForTest",
    defaults: new { controller = "Assessment", action = "GetColorblindnessImagesForTest" });

app.MapControllerRoute(
    name: "assessmentGetColorblindnessImagesForTestShort",
    pattern: "{assessmentType}/GetColorblindnessImagesForTest",
    defaults: new { controller = "Assessment", action = "GetColorblindnessImagesForTest" },
    constraints: new { assessmentType = "^(?!Admin|Account|api).*$" });

app.MapControllerRoute(
    name: "assessmentGetColorblindnessImages",
    pattern: "Assessment/GetColorblindnessImagesForTest",
    defaults: new { controller = "Assessment", action = "GetColorblindnessImagesForTest" });

// ── Colorblindness Submit routes ──────────────────────────────────────────
app.MapControllerRoute(
    name: "assessmentSubmitColorblindnessResponses",
    pattern: "Assessment/SubmitColorblindnessResponses",
    defaults: new { controller = "Assessment", action = "SubmitColorblindnessResponses" });

app.MapControllerRoute(
    name: "assessmentSubmitColorblindnessResponsesShort",
    pattern: "{assessmentType}/SubmitColorblindnessResponses",
    defaults: new { controller = "Assessment", action = "SubmitColorblindnessResponses" },
    constraints: new { assessmentType = "^(?!Admin|Account|api).*$" });
// ──────────────────────────────────────────────────────────────────────────

// Admin routes
app.MapControllerRoute(
    name: "admin",
    pattern: "Admin/{action=Login}/{id?}",
    defaults: new { controller = "Admin" });

// Default route
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Admin}/{action=Login}/{id?}");

app.Run();
