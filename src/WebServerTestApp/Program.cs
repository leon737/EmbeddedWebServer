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


using System;
using System.Net;
using System.Configuration;
using WebServer;
using WebServer.Security;
using WebServerTestApp.WebPages;

namespace WebServerTestApp
{
	class Program
	{
		static void Main()
		{
			//WebServer.Security.CertificateManager.CreateCertificate("CN=localhost", new CertificateConfig
			//	{ FilePath=@"D:\cert.pfx", IsEnabled=true, Password = "111"});

			//return;

			var web = new Server(IPAddress.Any, int.Parse(ConfigurationManager.AppSettings["port"]));
			web.ResolveDnsNames = true;
			web.UseSsl = true;
			web.SetCertificatePath(@"d:\cert.pfx", "111");
			web.RequestRouter.Add(new RouteEntry("/scripts", typeof(ImagePage)));
			web.RequestRouter.Add(new RouteEntry("/", typeof(HtmlPage)));
			web.AuthenticationMethod = AuthenticationMethod.Digest;
			web.AuthenticationEvent += delegate(object sender, AuthenticationEventArgs e) { e.Accept = true;
			                                                                              	e.Password = "111";
			};
			web.Start();
			Console.WriteLine("Web server started at port: " + ConfigurationManager.AppSettings["port"]);
			Console.WriteLine("Press any key to stop");
			Console.ReadKey(true);
			web.Stop();
		}
	}
}
