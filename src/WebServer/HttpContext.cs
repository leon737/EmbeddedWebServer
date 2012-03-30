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

namespace WebServer
{
	public class HttpContext
	{
		readonly WorkingProcess workingProcess;
		readonly HttpRequest request;
		readonly HttpResponse response;
		readonly HttpServer.HttpServer server;
		readonly HttpCookie cookie;
		readonly HttpSession session;
		readonly HttpApplication application;

		internal HttpContext(WorkingProcess workingProcess)
		{
			this.workingProcess = workingProcess;
			request = new HttpRequest(this);
			response = new HttpResponse(this);
			server = new HttpServer.HttpServer(this);
			cookie = new HttpCookie(this);
			session = new HttpSession(this);
			application = new HttpApplication(this);
		}

		internal WorkingProcess WorkingProcess
		{
			get { return workingProcess; }
		}

		public HttpRequest Request
		{
			get
			{
				return request;
			}
		}

		public HttpResponse Response
		{
			get
			{
				return response;
			}
		}

		public HttpServer.HttpServer Server
		{
			get 
			{ 
				return server; 
			}
		}

		public HttpCookie Cookie
		{
			get
			{
				return cookie;
			}
		}

		public HttpSession Session
		{
			get
			{
				return session;
			}
		}

		public HttpApplication Application
		{
			get
			{
				return application;
			}
		}

	}
}
