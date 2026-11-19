using Microsoft.EntityFrameworkCore;
using CloudPCRental.Models;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

// 1. ตั้งค่า DbContext สำหรับเชื่อมต่อ MySQL (csi402db)
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<Csi402dbContext>(options =>
{
    // ตรวจจับเวอร์ชัน MySQL อัตโนมัติ (ServerVersion.AutoDetect) ป้องกัน Error เมื่อเปลี่ยนเครื่องรัน
    try
    {
        options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString));
    }
    catch
    {
        // กรณีเซิร์ฟเวอร์ MySQL ยังไม่ได้เปิด ให้ใช้เวอร์ชันสำรองเพื่อไม่ให้แอปแครชตอนเริ่มระบบ
        options.UseMySql(connectionString, new MySqlServerVersion(new Version(8, 0, 30)));
    }
});

// 2. ตั้งค่า Memory Cache และ Session Service
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(60); // เซสชันคงอยู่ 60 นาที
    options.Cookie.HttpOnly = true;                 // ป้องกันการเข้าถึงผ่าน Script (XSS)
    options.Cookie.IsEssential = true;              // บังคับใช้งาน Cookie เพื่อการทำงานที่สมบูรณ์
});

// 3. ลงทะเบียน IHttpContextAccessor สำหรับดึง Session ใน View
builder.Services.AddHttpContextAccessor();

var app = builder.Build();

// รัน Seed ข้อมูลเริ่มต้น (User เริ่มต้น และ เครื่อง Cloud PC ตาม Wireframe)
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<Csi402dbContext>();
        await DbInitializer.SeedAsync(context);
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogWarning(ex, "Database initialization warning: MySQL server might be offline.");
    }
}

// Configure the HTTP request pipeline.
app.UseForwardedHeaders(new ForwardedHeadersOptions
{
    ForwardedHeaders = Microsoft.AspNetCore.HttpOverrides.ForwardedHeaders.XForwardedFor | Microsoft.AspNetCore.HttpOverrides.ForwardedHeaders.XForwardedProto
});

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/CloudPC/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

// 4. ใช้งาน Session Middleware (ต้องวางหลัง UseRouting และก่อน UseAuthorization)
app.UseSession();

// 4.1 ตรวจสอบสถานะบัญชีผู้ใช้และ Auto-restore Session จาก Persistent Cookie
app.Use(async (context, next) =>
{
    var sessionUid = context.Session.GetInt32("UserId");
    if (sessionUid.HasValue)
    {
        // ตรวจสอบว่าบัญชีที่อยู่ใน Session ยังคงมีอยู่ในระบบจริงหรือไม่ (หากถูกแอดมินลบไปแล้ว ต้องตัดสิทธิ์ทันที)
        var user = MockDataStore.GetUserById(sessionUid.Value);
        if (user == null)
        {
            context.Session.Clear();
            context.Response.Cookies.Delete("CloudPC_UserId");
            context.Response.Cookies.Delete("CloudPC_Username");
            context.Response.Cookies.Delete("CloudPC_Role");
        }
    }
    else
    {
        if (context.Request.Cookies.TryGetValue("CloudPC_UserId", out var uidStr) && int.TryParse(uidStr, out var uid))
        {
            var u = MockDataStore.GetUserById(uid);
            if (u != null)
            {
                context.Session.SetInt32("UserId", u.UserId);
                context.Session.SetString("Username", u.Username);
                context.Session.SetString("Email", u.Email);
                context.Session.SetString("Role", u.Role);
                decimal bal = MockDataStore.GetUserBalance(u.UserId);
                context.Session.SetString("WalletBalance", bal.ToString("F2"));
            }
            else
            {
                // หากบัญชีถูกลบไปแล้ว ให้ลบคุกกี้ออกทันทีเพื่อไม่ให้ค้างอยู่ในเบราว์เซอร์
                context.Response.Cookies.Delete("CloudPC_UserId");
                context.Response.Cookies.Delete("CloudPC_Username");
                context.Response.Cookies.Delete("CloudPC_Role");
            }
        }
    }
    await next();
});

app.UseAuthorization();

// ตั้งค่า Default Route ไปที่ CloudPCController
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=CloudPC}/{action=Index}/{id?}");

app.Run();
