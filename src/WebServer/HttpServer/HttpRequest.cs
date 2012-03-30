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

using System.Collections.Generic;
using System.Text;

namespace WebServer.HttpServer
{
	public class HttpRequest
	{
		readonly HttpContext context;
		readonly HttpQueryString queryString;
		readonly HttpForm form;
		readonly HttpServerVariables serverVariables;
		public string ContentType { get; private set; }

		internal HttpRequest(HttpContext context)
		{
			this.context = context;			
			queryString = new HttpQueryString(context.WorkingProcess.QueryString);
			serverVariables = new HttpServerVariables(context.WorkingProcess);
			ContentType = serverVariables["CONTENT_TYPE"];
			form = new HttpForm(context.WorkingProcess.RequestBody, context.WorkingProcess.BinaryRequest, ContentType);			
		}
		
		public HttpQueryString QueryString
		{
			get
			{
				return queryString;
			}
		}

		public HttpForm Form
		{
			get
			{
				return form;
			}
		}


		public HttpServerVariables ServerVariables
		{
			get
			{
				return serverVariables;
			}
		}

		public string Path
		{
			get { return context.WorkingProcess.Path; }
		}

		public string HttpMethod
		{
			get { return context.WorkingProcess.HttpMethod; }
		}

	}

	public class HttpQueryString : Dictionary<string, string> 
	{
		readonly string qs;

		internal HttpQueryString(string qs)
		{
			this.qs = qs;
			string[] pairs = qs.Split('&');
			foreach (string pair in pairs)
			{
				string[] keyvalue = pair.Split('=');
				Add(keyvalue[0], keyvalue.Length > 1 ? keyvalue[1] : string.Empty);
			}
		}

		public override string ToString()
		{
			return qs;
		}
	}

	public class HttpForm :Dictionary<string, string>
	{
		readonly string form;
		public byte[] Binary { get; private set; }
		public string Boundary { get; private set; }


		internal HttpForm(string form, byte[] binary, string contentType)
		{			
			this.form = form;
			if (contentType == "application/x-www-form-urlencoded")
			{
				string[] pairs = form.Split('&');
				foreach (string pair in pairs)
				{
					string[] keyvalue = pair.Split('=');
					Add(keyvalue[0], keyvalue.Length > 1 ? keyvalue[1] : string.Empty);
				}
			}
			else
			{
				Binary = binary;
				if (!string.IsNullOrEmpty(contentType))
					Boundary = GetMultipartBoundary(contentType);
			}
		}

		public override string ToString()
		{
			return form;
		}

		private static string GetMultipartBoundary(string contentType)
		{
			int index = contentType.IndexOf("boundary=");
			if (index == -1)
				return string.Empty;
			return contentType.Substring(index + 9).TrimEnd('"').Trim('-');
		}
	}

	public class HttpServerVariables : Dictionary<string, string>
	{
		internal HttpServerVariables(WorkingProcess process)
		{
			bool firstLine = true;
			var allRaw = new StringBuilder();
			var allHttp = new StringBuilder();
			foreach (string requestHeaderLine in process.RequestHeaders)
			{
				if (firstLine)
				{
					firstLine = false;
					continue;
				}
				if (!string.IsNullOrEmpty(requestHeaderLine))
				{
					allRaw.AppendLine(requestHeaderLine);
					string[] keyvalue = requestHeaderLine.Split(':');
					if (!string.IsNullOrEmpty(keyvalue[0]))
					{
						string key = "HTTP_" + keyvalue[0].ToUpper().Replace('-', '_').Trim();
						string value = keyvalue.Length > 1 ? keyvalue[1].Trim() : string.Empty;
						Add(key, value);
						allHttp.AppendLine(key + ":" + value);
					}
				}
			}
			Add("ALL_RAW", allRaw.ToString());
			Add("AUTH_TYPE", process.ConnectionInformation.AuthenticationMethod.ToString());
			Add("AUTH_USER", process.ConnectionInformation.LogonUser);
			Add("AUTH_PASSWORD", process.ConnectionInformation.AuthPassword);
			Add("CONTENT_LENGTH", process.GetHeaderValue("Content-Length"));
			Add("CONTENT_TYPE", process.GetHeaderValue("Content-Type"));
			Add("HTTPS", "off");
			Add("LOCAL_ADDR", process.ConnectionInformation.LocalIpAddress.ToString());
			Add("LOGON_USER", process.ConnectionInformation.LogonUser);
			Add("PATH_INFO", process.Path);
			Add("QUERY_STRING", process.QueryString);
			Add("REMOTE_ADDR", process.ConnectionInformation.RemoteIpAddress.ToString());
			Add("REMOTE_HOST", process.ConnectionInformation.RemoteName);
			Add("REQUEST_METHOD", process.HttpMethod.ToUpper());
			Add("SCRIPT_NAME", process.Path);
			Add("SERVER_NAME", process.ConnectionInformation.LocalName);
			Add("SERVER_PORT", process.ConnectionInformation.Port.ToString());
			Add("SERVER_PROTOCOL", "HTTP/1.1");
			Add("SERVER_SOFTWARE", Server.WebServerSoftwareName);
			Add("URL", process.Path);
			Add("ALL_HTTP", allHttp.ToString());
		}

	}
}
