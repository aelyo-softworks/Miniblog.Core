using JavaScriptEngineSwitcher.Extensions.MsDependencyInjection;
using JavaScriptEngineSwitcher.V8;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Rewrite;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Miniblog.Core.Services;
using WebEssentials.AspNetCore.OutputCaching;
using WebMarkupMin.AspNetCoreLatest;
using WebMarkupMin.Core;
using WilderMinds.MetaWeblog;
using IWmmLogger = WebMarkupMin.Core.Loggers.ILogger;
using WmmNullLogger = WebMarkupMin.Core.Loggers.NullLogger;

namespace Miniblog.Core
{
    public class Startup(IConfiguration configuration)
    {
        public IConfiguration Configuration { get; } = configuration;

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(
                    webBuilder =>
                    {
                        webBuilder
                            .UseStartup<Startup>()
                            .ConfigureKestrel(options => options.AddServerHeader = false);
                    });

        public static void Main(string[] args) => CreateHostBuilder(args).Build().Run();

        /// <remarks>This method gets called by the runtime. Use this method to configure the HTTP request pipeline.</remarks>
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Shared/Error");
                app.UseHsts();
            }

            app.Use(
                (context, next) =>
                {
                    context.Response.Headers.XContentTypeOptions = "nosniff";
                    return next();
                });

            app.UseStatusCodePagesWithReExecute("/Shared/Error");
            app.UseWebOptimizer();

            app.UseStaticFilesWithCache();

            if (Configuration.GetValue<bool>("forcessl"))
            {
                app.UseHttpsRedirection();
            }

            if (Configuration.GetValue<bool>("forceWwwPrefix"))
            {
                app.UseRewriter(new RewriteOptions().AddRedirectToWwwPermanent());
            }

            app.UseMetaWeblog("/metaweblog");
            app.UseAuthentication();

            app.UseOutputCaching();
            app.UseWebMarkupMin();

            app.UseRouting();

            app.UseAuthorization();

            app.UseEndpoints(
                endpoints =>
                {
                    endpoints.MapControllerRoute("default", "{controller=Blog}/{action=Index}/{id?}");
                });
        }

        /// <remarks>This method gets called by the runtime. Use this method to add services to the container.</remarks>
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddControllersWithViews();
            services.AddRazorPages();

            services.AddSingleton<IUserServices, BlogUserServices>();
            services.AddSingleton<IBlogService, FileBlogService>();
            services.Configure<BlogSettings>(Configuration.GetSection("blog"));
            services.TryAddSingleton<IHttpContextAccessor, HttpContextAccessor>();
            services.AddMetaWeblog<Services.MetaWeblogService>();

            // Progressive Web Apps https://github.com/madskristensen/WebEssentials.AspNetCore.ServiceWorker
            services.AddProgressiveWebApp(
                new WebEssentials.AspNetCore.Pwa.PwaOptions
                {
                    OfflineRoute = "/shared/offline/"
                });

            // Output caching (https://github.com/madskristensen/WebEssentials.AspNetCore.OutputCaching)
            services.AddOutputCaching(
                options =>
                {
                    options.Profiles["default"] = new OutputCacheProfile
                    {
                        Duration = 3600
                    };
                });

            // Cookie authentication.
            services
                .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
                .AddCookie(
                    options =>
                    {
                        options.LoginPath = "/login/";
                        options.LogoutPath = "/logout/";
                    });

            // HTML minification (https://github.com/Taritsyn/WebMarkupMin)
            services
                .AddWebMarkupMin(
                    options =>
                    {
                        options.AllowMinificationInDevelopmentEnvironment = true;
                        options.DisablePoweredByHttpHeaders = true;
                    })
                .AddHtmlMinification(
                    options =>
                    {
                        options.MinificationSettings.RemoveOptionalEndTags = false;
                        options.MinificationSettings.WhitespaceMinificationMode = WhitespaceMinificationMode.Safe;
                    });
            services.AddSingleton<IWmmLogger, WmmNullLogger>(); // Used by HTML minifier

            // Bundling, minification and Sass transpiration (https://github.com/ligershark/WebOptimizer)
            services.AddJsEngineSwitcher(options =>
               options.DefaultEngineName = V8JsEngine.EngineName
           ).AddV8();
            services.AddWebOptimizer(
                pipeline =>
                {
                    pipeline.MinifyJsFiles();
                    pipeline.CompileScssFiles()
                            .InlineImages(1);
                });
        }
    }
}
