using System.IO;
using Microsoft.AspNetCore.Hosting;

namespace WebEssentials.AspNetCore.Pwa
{
	/// <summary>
	/// A utility that can retrieve the contents of a CustomServiceworker strategy file
	/// </summary>
	public class RetrieveCustomServiceworker(IWebHostEnvironment env)
	{
		/// <summary>
		/// Returns a <seealso cref="string"/> containing the contents of a Custom Serviceworker javascript file
		/// </summary>
		/// <returns></returns>
		public string? GetCustomServiceworker(string fileName = "customserviceworker.js")
		{
			var file = env.WebRootFileProvider.GetFileInfo(fileName);
			var path = file?.PhysicalPath;
			if (path == null)
				return null;

			return File.ReadAllText(path);
		}
	}
}
