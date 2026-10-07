using System.Globalization;
using System.Threading.RateLimiting;
using AmHerb.Web.Data;
using AmHerb.Web.Domain;
using AmHerb.Web.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

if (args.Contains("--setup-admin")) AdminConsole.ReadCredentials();
var builder = WebApplication.CreateBuilder(args);
var connection = builder.Configuration.GetConnectionString("DefaultConnection");
if (string.IsNullOrWhiteSpace(connection)) throw new InvalidOperationException("Configure ConnectionStrings:DefaultConnection in user secrets or environment variables.");
builder.Services.AddDbContext<AmHerbDbContext>(o => o.UseSqlServer(connection));
builder.Services.AddDefaultIdentity<AppUser>(o =>
{
    o.SignIn.RequireConfirmedAccount = true; o.User.RequireUniqueEmail = true;
    o.Password.RequiredLength = 5; o.Password.RequireDigit = false; o.Password.RequireLowercase = false;
    o.Password.RequireUppercase = false; o.Password.RequireNonAlphanumeric = false;
    o.Lockout.MaxFailedAccessAttempts = 5; o.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
}).AddRoles<IdentityRole>().AddEntityFrameworkStores<AmHerbDbContext>().AddPasswordValidator<SimplePasswordValidator>();
builder.Services.ConfigureApplicationCookie(o => { o.Cookie.HttpOnly = true; o.Cookie.SameSite = SameSiteMode.Lax; o.Cookie.SecurePolicy = builder.Environment.IsDevelopment() ? CookieSecurePolicy.SameAsRequest : CookieSecurePolicy.Always; o.ExpireTimeSpan = TimeSpan.FromHours(8); });
builder.Services.ConfigureApplicationCookie(o =>
{
    o.Events.OnRedirectToLogin = context => { if (context.Request.Path.StartsWithSegments("/api")) context.Response.StatusCode = 401; else context.Response.Redirect(context.RedirectUri); return Task.CompletedTask; };
    o.Events.OnRedirectToAccessDenied = context => { if (context.Request.Path.StartsWithSegments("/api")) context.Response.StatusCode = 403; else context.Response.Redirect(context.RedirectUri); return Task.CompletedTask; };
});
if (!string.IsNullOrEmpty(builder.Configuration["Authentication:Google:ClientId"])) builder.Services.AddAuthentication().AddGoogle(o => { o.ClientId = builder.Configuration["Authentication:Google:ClientId"]!; o.ClientSecret = builder.Configuration["Authentication:Google:ClientSecret"]!; });
builder.Services.AddAuthorization(o =>
{
    foreach (var p in new[] { ("Prices", new[] { "Marketing" }), ("Tokens", new[] { "Finance" }), ("Refunds", new[] { "Finance" }), ("Payments", new[] { "Finance" }), ("Reports", new[] { "Finance", "Marketing" }), ("Inventory", new[] { "Warehouse" }), ("Members", new[] { "CustomerService" }), ("Orders", new[] { "CustomerService", "Warehouse", "Finance" }), ("Settings", Array.Empty<string>()) })
        o.AddPolicy(p.Item1, policy => policy.RequireRole(p.Item2.Concat(new[] { "SuperAdmin", "Admin" }).ToArray()));
    o.AddPolicy("BackOffice", p => p.RequireRole(SeedData.Roles.Where(x => x != "Member").ToArray()));
});
builder.Services.AddLocalization(o => o.ResourcesPath = "Resources");
builder.Services.AddControllersWithViews(o => { o.SuppressImplicitRequiredAttributeForNonNullableReferenceTypes = true; o.Filters.Add(new AutoValidateAntiforgeryTokenAttribute()); o.Filters.Add<BusinessExceptionFilter>(); o.Filters.Add<InputValidationFilter>(); }).AddViewLocalization().AddDataAnnotationsLocalization();
builder.Services.AddRazorPages();
builder.Services.AddAntiforgery(o => o.HeaderName = "X-CSRF-TOKEN");
builder.Services.AddHttpContextAccessor(); builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddScoped<Actor>(); builder.Services.AddScoped<AuditService>(); builder.Services.AddScoped<MemberService>();
builder.Services.AddScoped<PricingService>(); builder.Services.AddScoped<RewardService>(); builder.Services.AddScoped<InventoryService>();
builder.Services.AddScoped<CommerceService>(); builder.Services.AddScoped<PosService>(); builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddScoped<StoreQueries>(); builder.Services.AddScoped<CartService>();
builder.Services.AddScoped<SalesDashboardService>();
builder.Services.AddScoped<MemberStoreService>();
builder.Services.AddScoped<DemoDataMaintenance>();
builder.Services.AddScoped<OrderAccess>();
builder.Services.AddScoped<RedemptionConsentService>();
builder.Services.AddScoped<StockTransferService>();
builder.Services.AddScoped<MediaService>(); builder.Services.AddScoped<ConversationService>();
builder.Services.AddScoped<CreditService>();
if (builder.Environment.IsDevelopment()) builder.Services.AddScoped<IEmailSender, DemoEmailSender>();
else builder.Services.AddScoped<IEmailSender, SmtpEmailSender>();
builder.Services.AddScoped<IRecurringJobs, RecurringJobs>();
builder.Services.AddScoped<IPaymentGateway, ManualBankTransferProvider>(); builder.Services.AddScoped<IPaymentGateway, PromptPayQrProvider>(); builder.Services.AddScoped<IPaymentGateway, CardGatewayProvider>();
builder.Services.AddScoped<IShippingProvider, ManualShippingProvider>(); builder.Services.AddScoped<ILineNotifier, LineNotifier>();
builder.Services.AddScoped<IMarketplaceProvider, ShopeeProvider>(); builder.Services.AddScoped<IMarketplaceProvider, TikTokShopProvider>(); builder.Services.AddScoped<IMarketplaceProvider, LazadaProvider>();
builder.Services.AddScoped<CsvMarketplaceImportProvider>(); builder.Services.AddHostedService<CommerceWorker>();
builder.Services.AddRateLimiter(o =>
{
    o.RejectionStatusCode = 429;
    o.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context => RateLimitPartition.GetFixedWindowLimiter(
        context.User.Identity?.Name ?? context.Connection.RemoteIpAddress?.ToString() ?? "anonymous",
        _ => new FixedWindowRateLimiterOptions { PermitLimit = context.Request.Path.StartsWithSegments("/api") ? 120 : 300, Window = TimeSpan.FromMinutes(1), QueueLimit = 0 }));
});
var app = builder.Build();
if (args.Contains("--import-products"))
{
    var position = Array.IndexOf(args, "--import-products");
    if (position + 1 >= args.Length) throw new InvalidOperationException("Specify the original product attachment directory.");
    await using var scope = app.Services.CreateAsyncScope();
    await ProductArtworkImport.RunAsync(scope.ServiceProvider.GetRequiredService<AmHerbDbContext>(), scope.ServiceProvider.GetRequiredService<AuditService>(), args[position + 1]);
    Console.WriteLine("Verified 14 original images for 8 products in database. Membership artwork saved as draft."); return;
}
if (args.Contains("--import-brand"))
{
    var position=Array.IndexOf(args,"--import-brand");
    if(position+1>=args.Length)throw new InvalidOperationException("Specify the attachment directory.");
    await using var scope=app.Services.CreateAsyncScope();var db=scope.ServiceProvider.GetRequiredService<AmHerbDbContext>();
    await using var tx=await db.Database.BeginTransactionAsync();
    foreach(var slot in new[]{2,3,4})
    {
        var candidates=Directory.GetFiles(args[position+1],$"attached-{slot}.*");if(candidates.Length!=1)throw new InvalidOperationException("Missing original brand image "+slot);
        var bytes=await File.ReadAllBytesAsync(candidates[0]);var type=MediaService.Validate(bytes);
        var asset=await db.MediaAssets.SingleOrDefaultAsync(x=>x.Purpose=="brand"&&x.Slot==slot);
        if(asset==null){asset=new MediaAsset{Purpose="brand",Slot=slot};db.MediaAssets.Add(asset);}
        asset.Data=bytes;asset.ContentType=type;asset.Alt="AM HERB Balance & Restore";asset.UpdatedAt=DateTime.UtcNow;
    }
    foreach(var audience in new[]{ContentAudience.Public,ContentAudience.Members})
    {
        var title=audience==ContentAudience.Public?"ร่าง: โปรโมชั่นสำหรับบุคคลทั่วไป":"ร่าง: สิทธิพิเศษสำหรับสมาชิก";
        if(!await db.ContentPosts.AnyAsync(x=>x.Title==title))db.ContentPosts.Add(new ContentPost{Title=title,Body="ตัวอย่างประกาศ: ระบุสินค้า ข้อเสนอ ระยะเวลา และเงื่อนไขโปรโมชั่นก่อนเปิดเผยแพร่",Audience=audience,Kind=ContentKind.Advertisement,StartsAt=DateTime.UtcNow,EndsAt=DateTime.UtcNow.AddMonths(1),MediaAsset=db.MediaAssets.Local.First(x=>x.Purpose=="brand"&&x.Slot==(audience==ContentAudience.Public?2:4)),Link="/Catalog",Published=false});
    }
    scope.ServiceProvider.GetRequiredService<AuditService>().Add("Media.BrandImport","2,3,4","User-provided original brand photos and draft advertisements");
    await db.SaveChangesAsync();await tx.CommitAsync();Console.WriteLine("Brand images 2, 3 and 4 stored in database.");return;
}
if (args.Contains("--check-database"))
{ await using var scope = app.Services.CreateAsyncScope(); var ok = await scope.ServiceProvider.GetRequiredService<AmHerbDbContext>().Database.CanConnectAsync(); Console.WriteLine(ok ? "Database connection: OK" : "Database connection: FAILED"); Environment.ExitCode = ok ? 0 : 1; return; }
if (args.Contains("--migrate"))
{ await using var scope = app.Services.CreateAsyncScope(); await scope.ServiceProvider.GetRequiredService<AmHerbDbContext>().Database.MigrateAsync(); await SeedData.InitializeAsync(scope.ServiceProvider); Console.WriteLine("Migrations and role initialization complete."); return; }
if (args.Contains("--setup-admin"))
{
    await using var scope = app.Services.CreateAsyncScope();
    var users = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();
    if (await users.FindByEmailAsync(Environment.GetEnvironmentVariable("AMHERB_ADMIN_EMAIL")!) != null) throw new InvalidOperationException("This email already exists. Bootstrap requires a new email and does not elevate public accounts.");
    await SeedData.InitializeAsync(scope.ServiceProvider); Console.WriteLine("SuperAdmin created. Sign in through /Identity/Account/Login."); return;
}
if (!app.Environment.IsEnvironment("Testing"))
{ await using var scope = app.Services.CreateAsyncScope(); await SeedData.InitializeAsync(scope.ServiceProvider); }
if (!app.Environment.IsDevelopment()) { app.UseExceptionHandler("/Home/Error"); app.UseHsts(); }
app.Use(async (ctx, next) =>
{
    ctx.Response.Headers["X-Content-Type-Options"] = "nosniff";
    ctx.Response.Headers["X-Frame-Options"] = "DENY";
    ctx.Response.Headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
    ctx.Response.Headers["Content-Security-Policy"] = "default-src 'self'; img-src 'self' data: blob: https:; style-src 'self' 'unsafe-inline'; script-src 'self'; frame-ancestors 'none'; base-uri 'self'; form-action 'self'";
    await next();
});
app.UseHttpsRedirection(); app.UseStaticFiles(); app.UseRouting();
app.UseRequestLocalization(new RequestLocalizationOptions { DefaultRequestCulture = new RequestCulture("en-US", "th-TH"), SupportedCultures = [new CultureInfo("en-US")], SupportedUICultures = [new CultureInfo("th-TH"), new CultureInfo("en-US")] });
app.UseAuthentication(); app.UseAuthorization(); app.UseRateLimiter();
app.MapControllerRoute("default", "{controller=Home}/{action=Index}/{id?}"); app.MapRazorPages();
app.Run();
public partial class Program { }
