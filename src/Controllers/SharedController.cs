using Microsoft.AspNetCore.Mvc;

namespace Miniblog.Core.Controllers
{
	public class SharedController : Controller
	{
		public IActionResult Error() => View(Response.StatusCode);

		/// <summary>
		/// This is for use in wwwroot/serviceworker.js to support offline scenarios
		/// </summary>
		public IActionResult Offline() => View();
	}
}
