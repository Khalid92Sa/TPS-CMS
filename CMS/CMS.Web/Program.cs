using CMS.Application.ActionFilters;
using CMS.Application.Configrations;
using CMS.Application.Middlewares;
using CMS.Domain;
using CMS.Repository.Implementation;
using CMS.Repository.Interfaces;
using CMS.Repository.Repositories;
using CMS.Services.Interfaces;
using CMS.Services.Services;
using Hangfire;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;
using StackExchange.Profiling;
using System;
using System.Reflection;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.Host.SetupConfiguration(Assembly.GetExecutingAssembly().GetName().Name.ToLower().Replace(".", "-")).UseSerilog();

builder.Services.AddControllersWithViews(options =>
{
    options.Filters.AddService<LoggingActionFilter>();
});

builder.Services.AddHttpContextAccessor();

builder.Services.AddScoped<ICarrerOfferService, CarrerOfferService>();
builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    options.UseLazyLoadingProxies();
    options.UseSqlServer(builder.Configuration.GetConnectionString("Defultconiction"));
});

builder.Services.AddScoped(typeof(IUserRepository), typeof(UserRepository));
builder.Services.AddTransient<IAccountService, AccountService>();

builder.Services.AddScoped(typeof(ICompanyRepository), typeof(CompanyRepository));
builder.Services.AddTransient<ICompanyService, CompanyService>();

builder.Services.AddScoped(typeof(ICountryRepository), typeof(CountryRepository));
builder.Services.AddTransient<ICountryService, CountryService>();

builder.Services.AddScoped(typeof(IPositionRepository), typeof(PositionRepository));
builder.Services.AddTransient<IPositionService, PositionService>();

builder.Services.AddScoped(typeof(IStatusRepository), typeof(StatusRepository));
builder.Services.AddTransient<IStatusService, StatusService>();

builder.Services.AddScoped(typeof(ICarrerOfferRepository), typeof(CarrerOfferRepository));
builder.Services.AddScoped<ICarrerOfferService, CarrerOfferService>();

builder.Services.AddScoped(typeof(ICandidateRepository), typeof(CandidateRepository));
builder.Services.AddScoped<ICandidateService, CandidateService>();

builder.Services.AddScoped(typeof(IAttachmentRepository), typeof(AttachmentRepository));
builder.Services.AddScoped<IAttachmentService, AttachmentService>();

builder.Services.AddScoped(typeof(ITrackRepository), typeof(TrackRepository));
builder.Services.AddScoped<ITrackService, TrackService>();

builder.Services.AddScoped(typeof(IInterviewsRepository), typeof(InterviewsRepository));
builder.Services.AddTransient<IInterviewsService, InterviewsService>();
builder.Services.AddTransient<ISearchInterviewsService, SearchInterviewsService>();

builder.Services.AddScoped(typeof(INotificationsRepository), typeof(NotificationsRepository));
builder.Services.AddTransient<INotificationsService, NotificationsService>();

builder.Services.AddTransient<IEmailService, EmailService>();
builder.Services.AddScoped(typeof(ITemplatesRepository), typeof(TemplatesRepository));
builder.Services.AddTransient<ITemplatesService, TemplatesService>();

builder.Services.AddTransient<IReportingService, ReportingService>();
builder.Services.AddScoped<LoggingActionFilter>();

builder.Services.AddDefaultIdentity<IdentityUser>()
    .AddDefaultTokenProviders()
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/users/login";
    options.LogoutPath = "/users/logout";
    options.AccessDeniedPath = "/users/accessDenied";
});
builder.Services.Configure<IdentityOptions>(x =>
{
    x.Password.RequireDigit = false;
    x.Password.RequiredLength = 5;
    x.Password.RequireNonAlphanumeric = false;
    x.Password.RequireLowercase = false;
    x.Password.RequireUppercase = false;
    x.User.RequireUniqueEmail = false;
});

// Hangfire setup
builder.Services.AddHangfire(x => x.UseSqlServerStorage(builder.Configuration.GetConnectionString("Defultconiction")));
builder.Services.AddHangfireServer();

builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromDays(20);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.IOTimeout = TimeSpan.FromDays(20);
});

builder.Services.AddMiniProfiler(options =>
{
    options.RouteBasePath = "/profiler"; // profiler/
    options.ColorScheme = ColorScheme.Dark;
    options.EnableMvcViewProfiling = true;
    options.EnableMvcFilterProfiling = true;
    options.ShowControls = true;
    options.EnableDebugMode = true;
    options.PopupShowTimeWithChildren = true;
    options.ShouldProfile = request => true;
    options.PopupMaxTracesToShow = 20;
    options.PopupShowTimeWithChildren = true;
}).AddEntityFramework();

builder.Services.AddRazorPages()
    .AddMvcOptions(options =>
    {
        options.MaxModelValidationErrors = 50;
        options.ModelBindingMessageProvider.SetValueMustNotBeNullAccessor(
            _ => "This field is required.");
    });

WebApplication app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseMiniProfiler();
    app.UseDeveloperExceptionPage();
}
else
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();
app.UseHangfireDashboard("/jobs");
app.UseSession();
app.UseMiddleware<RoleBasedRedirectionMiddleware>();



app.MapControllerRoute(
    name: "default",
    pattern: "{controller=users}/{action=login}/{id?}");

app.Logger.LogInformation("Starting the app");

app.Run();
