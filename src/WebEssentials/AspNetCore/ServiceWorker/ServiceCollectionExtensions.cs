using System;

using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Razor.TagHelpers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection.Extensions;

using WebEssentials.AspNetCore.Pwa;

namespace Microsoft.Extensions.DependencyInjection
{
	/// <summary>
	/// Extension methods for the <see cref="IServiceCollection"/> type.
	/// </summary>
	public static partial class ServiceCollectionExtensions
	{
		/// <summary>
		/// Adds ServiceWorker services to the specified <see cref="IServiceCollection"/>.
		/// </summary>
		public static IServiceCollection AddServiceWorker(this IServiceCollection services)
		{
			services.TryAddSingleton<IHttpContextAccessor, HttpContextAccessor>();
			services.AddTransient<ITagHelperComponent, ServiceWorkerTagHelperComponent>();
			services.AddTransient<RetrieveCustomServiceworker>();
			services.AddTransient(svc => new PwaOptions(svc.GetRequiredService<IConfiguration>()));
			return services;
		}

		/// <summary>
		/// Adds ServiceWorker services to the specified <see cref="IServiceCollection"/>.
		/// </summary>
		public static IServiceCollection AddServiceWorker(this IServiceCollection services, PwaOptions options)
		{
			services.TryAddSingleton<IHttpContextAccessor, HttpContextAccessor>();
			services.AddTransient<ITagHelperComponent, ServiceWorkerTagHelperComponent>();
			services.AddTransient<RetrieveCustomServiceworker>();
			services.AddTransient(factory => options);
			return services;
		}

		/// <summary>
		/// Adds ServiceWorker services to the specified <see cref="IServiceCollection"/>.
		/// </summary>
		public static IServiceCollection AddServiceWorker(this IServiceCollection services, string baseRoute = "", string offlineRoute = PwaConstants.Offlineroute, ServiceWorkerStrategy strategy = ServiceWorkerStrategy.CacheFirstSafe, bool registerServiceWorker = true, bool registerWebManifest = true, string cacheId = PwaConstants.DefaultCacheId, string routesToPreCache = "", string routesToIgnore = "", string customServiceWorkerFileName = PwaConstants.CustomServiceworkerFileName)
		{
			services.TryAddSingleton<IHttpContextAccessor, HttpContextAccessor>();
			services.AddTransient<ITagHelperComponent, ServiceWorkerTagHelperComponent>();
			services.AddTransient<RetrieveCustomServiceworker>();
			services.AddTransient(factory => new PwaOptions
			{
				BaseRoute = baseRoute,
				OfflineRoute = offlineRoute,
				Strategy = strategy,
				RegisterServiceWorker = registerServiceWorker,
				RegisterWebmanifest = registerWebManifest,
				CacheId = cacheId,
				RoutesToPreCache = routesToPreCache,
				CustomServiceWorkerStrategyFileName = customServiceWorkerFileName,
				RoutesToIgnore = routesToIgnore
			});
			return services;
		}

		/// <summary>
		/// Adds Web App Manifest services to the specified <see cref="IServiceCollection"/>.
		/// </summary>
		/// <param name="services">The service collection.</param>
		/// <param name="manifestFileName">The path to the Web App Manifest file relative to the wwwroot folder.</param>
		public static IServiceCollection AddWebManifest(this IServiceCollection services, string manifestFileName = PwaConstants.WebManifestFileName)
		{
			services.AddTransient<ITagHelperComponent, WebmanifestTagHelperComponent>();
			services.AddSingleton(sp =>
			{
				var env = sp.GetRequiredService<IWebHostEnvironment>();
				return new WebManifestCache(env, manifestFileName);
			});

			services.AddScoped(sp => sp.GetRequiredService<WebManifestCache>().GetManifest() ?? throw new InvalidOperationException());
			return services;
		}

		/// <summary>
		/// Adds Web App Manifest and Service Worker to the specified <see cref="IServiceCollection"/>.
		/// </summary>
		/// <param name="services">The service collection.</param>
		/// <param name="manifestFileName">The path to the Web App Manifest file relative to the wwwroot folder.</param>
		public static IServiceCollection AddProgressiveWebApp(this IServiceCollection services, string manifestFileName = PwaConstants.WebManifestFileName) => services.AddWebManifest(manifestFileName).AddServiceWorker();

		/// <summary>
		/// Adds Web App Manifest and Service Worker to the specified <see cref="IServiceCollection"/>.
		/// </summary>
		/// <param name="services">The service collection.</param>
		/// <param name="manifestFileName">The path to the Web App Manifest file relative to the wwwroot folder.</param>
		/// <param name="options">Options for the service worker and Web App Manifest</param>
		public static IServiceCollection AddProgressiveWebApp(this IServiceCollection services, PwaOptions options, string manifestFileName = PwaConstants.WebManifestFileName) => services.AddWebManifest(manifestFileName).AddServiceWorker(options);
	}
}
