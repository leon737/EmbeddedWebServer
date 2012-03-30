using System;
using System.Collections.Generic;
using System.Text;
using System.Globalization;

namespace EmbeddedWebServer
{
	public class HttpRequest
	{
		HttpContext context;
		HttpQueryString queryString;
		HttpForm form;
		HttpServerVariables serverVariables;
		public string ContentType { get; private set; }

		internal HttpRequest(HttpContext context)
		{
			this.context = context;			
			this.queryString = new HttpQueryString(context.WorkingProcess.QueryString);
			this.serverVariables = new HttpServerVariables(context.WorkingProcess);
			ContentType = serverVariables["CONTENT_TYPE"];
			this.form = new HttpForm(context.WorkingProcess.RequestBody, context.WorkingProcess.BinaryRequest, ContentType);			
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
		string qs;

		internal HttpQueryString(string qs)
		{
			this.qs = qs;
			string[] pairs = qs.Split('&');
			foreach (string pair in pairs)
			{
				string[] keyvalue = pair.Split('=');
				this.Add(keyvalue[0], keyvalue.Length > 1 ? keyvalue[1] : string.Empty);
			}
		}

		public override string ToString()
		{
			return qs;
		}
	}

	public class HttpForm :Dictionary<string, string>
	{
		string form;
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
					this.Add(keyvalue[0], keyvalue.Length > 1 ? keyvalue[1] : string.Empty);
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

		private string GetMultipartBoundary(string contentType)
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
			StringBuilder allRaw = new StringBuilder();
			foreach (string requestHeaderLine in process.RequestHeaders)
			{
				if (firstLine)
				{
					firstLine = false;
					continue;
				}
				if (!string.IsNullOrEmpty(requestHeaderLine))
					allRaw.AppendLine (requestHeaderLine);
			}
			Add("ALL_RAW", allRaw.ToString());
			Add("AUTH_TYPE", process.ConnectionInformation.AuthenticationMethod.ToString());
			Add("AUTH_USER", process.ConnectionInformation.LogonUser);
			Add("AUTH_PASSWORD", process.ConnectionInformation.AuthPassword);
			Add("CONTENT_LENGTH", process.GetHeaderValue("Content-Length"));
			Add("CONTENT_TYPE", process.GetHeaderValue("Content-Type"));
			Add("HTTPS", "off");
			Add("LOCAL_ADDR", process.ConnectionInformation.LocalIPAddress.ToString());
			Add("LOGON_USER", process.ConnectionInformation.LogonUser);
			Add("PATH_INFO", process.Path);
			Add("QUERY_STRING", process.QueryString);
			Add("REMOTE_ADDR", process.ConnectionInformation.RemoteIPAddress.ToString());
			Add("REMOTE_HOST", process.ConnectionInformation.RemoteName);
			Add("REQUEST_METHOD", process.HttpMethod.ToUpper());
			Add("SCRIPT_NAME", process.Path);
			Add("SERVER_NAME", process.ConnectionInformation.LocalName);
			Add("SERVER_PORT", process.ConnectionInformation.Port.ToString());
			Add("SERVER_PROTOCOL", "HTTP/1.1");
			Add("SERVER_SOFTWARE", WebServer.WEB_SERVER_SOFTWARE_NAME);
			Add("URL", process.Path);
			StringBuilder allHttp = new StringBuilder();
			firstLine = true;
			foreach (string requestHeaderLine in process.RequestHeaders)
			{
				if (firstLine)
				{
					firstLine = false;
					continue;
				}
				string[] keyvalue = requestHeaderLine.Split(':');
				if (!string.IsNullOrEmpty(keyvalue[0]))
				{
					string key = "HTTP_" + keyvalue[0].ToUpper().Replace('-', '_').Trim();
					string value = keyvalue.Length > 1 ? keyvalue[1].Trim() : string.Empty;
					Add(key, value);
					allHttp.AppendLine(key + ":" + value);
				}
			}
			Add("ALL_HTTP", allHttp.ToString());
		}

	}
}
