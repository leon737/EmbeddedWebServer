using System;
using System.Collections.Generic;
using System.Text;

namespace EmbeddedWebServer
{
	public class HttpServer
	{
		HttpContext context;

		internal HttpServer(HttpContext context)
		{
			this.context = context;
		}

	}
}
