using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Net.Http.Headers;

namespace WebEssentials.AspNetCore.Pwa
{
    /// <summary>
    /// A controller for manifest.webmanifest, serviceworker.js and offline.html
    /// </summary>
    /// <remarks>
    /// Creates an instance of the controller.
    /// </remarks>
    public class PwaController(PwaOptions options, RetrieveCustomServiceworker customServiceworker) : Controller
    {
        /// <summary>
        /// Serves a service worker based on the provided settings.
        /// </summary>
        [Route(PwaConstants.ServiceworkerRoute)]
        [HttpGet]
        public async Task<IActionResult> ServiceWorkerAsync()
        {
            Response.ContentType = "application/javascript; charset=utf-8";
            Response.Headers[HeaderNames.CacheControl] = $"max-age={options.ServiceWorkerCacheControlMaxAge}";

            if (options.Strategy == ServiceWorkerStrategy.CustomStrategy)
            {
                var cjs = customServiceworker.GetCustomServiceworker(options.CustomServiceWorkerStrategyFileName);
                if (cjs == null)
                    return NotFound();

                return Content(InsertStrategyOptions(cjs));
            }

            var fileName = options.Strategy + ".js";
            var resourceStream = GetManifestResourceStream($"ServiceWorker.Files.{fileName}");
            using var reader = new StreamReader(resourceStream!);
            var js = await reader.ReadToEndAsync();
            return Content(InsertStrategyOptions(js));
        }

        private string InsertStrategyOptions(string javascriptString)
        {
            return javascriptString
                .Replace("{version}", options.CacheId + "::" + options.Strategy)
                // *WARNING* note this line presumes the embedded .js files contains exactly the "{ routes }" string while this is subject to js formatting!
                .Replace("{ routes }", string.Join(",", options.RoutesToPreCache.Split(',', StringSplitOptions.RemoveEmptyEntries).Select(r => "'" + r.Trim() + "'")))
                .Replace("{offlineRoute}", options.BaseRoute + options.OfflineRoute)
                .Replace("{ignoreRoutes}", string.Join(",", options.RoutesToIgnore.Split(',', StringSplitOptions.RemoveEmptyEntries).Select(r => "'" + r.Trim() + "'")));
        }

        /// <summary>
        /// Serves the offline.html file
        /// </summary>
        [Route(PwaConstants.Offlineroute)]
        [HttpGet]
        public async Task<IActionResult> OfflineAsync()
        {
            Response.ContentType = "text/html";

            var resourceStream = GetManifestResourceStream("ServiceWorker.Files.offline.html")!;
            using var reader = new StreamReader(resourceStream);
            return Content(await reader.ReadToEndAsync());
        }

        /// <summary>
        /// Serves the manifest.json file
        /// </summary>
        [Route(PwaConstants.WebManifestRoute)]
        [HttpGet]
        public IActionResult WebManifest([FromServices] WebManifest wm)
        {
            if (wm == null)
                return NotFound();

            Response.ContentType = "application/manifest+json; charset=utf-8";
            Response.Headers[HeaderNames.CacheControl] = $"max-age={options.WebManifestCacheControlMaxAge}";
            if (wm.RawJson == null)
                return NotFound();

            return Content(wm.RawJson);
        }

        private static Stream GetManifestResourceStream(string name)
        {
            var assembly = typeof(PwaController).Assembly;
            var streamName = assembly.GetManifestResourceNames().First(n => n.EndsWith(name));
            return assembly.GetManifestResourceStream(streamName)!;
        }
    }
}
