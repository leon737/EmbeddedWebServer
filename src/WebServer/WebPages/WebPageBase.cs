/* 
    Embedded Web Server App
    Copyright (C) 2012 Leonid Gordo

    This program is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    This program is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with this program.  If not, see <http://www.gnu.org/licenses/>.
 */

using WebServer.HttpServer;

namespace WebServer.WebPages
{
	public abstract class WebPageBase : IWebPage
	{
		protected IWebServer Server;

		protected HttpResponse Response
		{
			get { return Server.Context.Response; }
		}

		protected HttpRequest Request
		{
			get { return Server.Context.Request; }
		}

		protected HttpCookie Cookie
		{
			get { return Server.Context.Cookie; }
		}

		public HttpSession Session
		{
			get { return Server.Context.Session; }
		}

		public HttpApplication Application
		{
			get { return Server.Context.Application; }
		}

		public void ProcessRequest(IWebServer server)
		{
			Server = server;
			ProcessPage();
		}

		protected abstract void ProcessPage();
		
	}


}
