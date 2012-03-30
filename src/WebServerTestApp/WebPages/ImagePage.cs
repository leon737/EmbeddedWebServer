using System.Reflection;
using WebServer.WebPages;

namespace WebServerTestApp.WebPages
{
	class ImagePage : WebPageBase
	{
		protected override void ProcessPage()
		{
			string url = Request.Path;
			{
				var assembly = Assembly.GetExecutingAssembly();
				using (var stream = assembly.GetManifestResourceStream(assembly.GetName().Name +  url.Replace('/', '.')))
				{
					if (stream == null)
					{
						Response.Status = "404 Not Found";
						Response.Write("Resource Not Found");
						return;
					}

					if (url.EndsWith(".css")) Response.ContentType = "text/css";
					else if (url.EndsWith(".jpeg")) Response.ContentType = "image/jpeg";
					else if (url.EndsWith(".jpg")) Response.ContentType = "image/jpeg";
					else if (url.EndsWith(".gif")) Response.ContentType = "image/gif";
					else if (url.EndsWith(".png")) Response.ContentType = "image/png";
					else if (url.EndsWith(".js")) Response.ContentType = "text/javascript";

					var length = (int)stream.Length;
					var buffer = new byte[32768];
					for (int i = 0; i < length; i += 32768)
					{
						int blockLength = stream.Read(buffer, 0, 32768);
						Response.WriteBinary(buffer, 0, blockLength);
					}
				}
			}
		}
	}
}
