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
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Net.Sockets;
using WebServer.HttpServer;
using System.Linq;

namespace WebServer
{
	class WorkingProcess
	{
		const string SessionCookieKey = "WEBSERVERSESSIONUID";

		readonly Server webServer;
		readonly Stream stream;
		readonly ConnectionInformation ci;
		MemoryStream ms;
		StringBuilder outputStream;
		internal CoreSessionEventHandler OnSessionStartHandler;
		internal CoreAuthenticationEventHandler OnAuthenticateHandler;
		string httpMethod;
		string path;
		string queryString;
		readonly Dictionary<string, string> requestHeadersDictionary = new Dictionary<string, string>();
		string[] requestHeaders;
		string requestBody = string.Empty;
		string contentType = "text/html";
		string encodingString = "UTF-8";
		byte[] binaryRequest;

		List<string> extraHeaders;
		int statusCode = 200;
		string statusDescription = "OK";

		public WorkingProcess(Server webServer, Stream stream, ConnectionInformation ci, 
			CoreSessionEventHandler onSessionStartHandler, 
			CoreAuthenticationEventHandler onAuthenticateHandler)
		{
			this.webServer = webServer;
			this.stream = stream;
			this.ci = ci;
			OnSessionStartHandler = onSessionStartHandler;
			OnAuthenticateHandler = onAuthenticateHandler;
			ms = new MemoryStream();
			outputStream = new StringBuilder();
		}

		internal StringBuilder OutputStream
		{
			get { return outputStream; }
		}

		internal Stream OutputBinaryStream
		{
			get { return ms; }
		}

		public void Run()
		{
			bool persistentConnection;
			var reader = new StreamReader(stream);
			var writer = new StreamWriter(stream);
			do
			{
				var inbox = new StringBuilder();
				outputStream = new StringBuilder();
				ms = new MemoryStream();
				bool firstLine = true;
				try
				{
					string line;
					while (!string.IsNullOrEmpty((line = reader.ReadLine())))
					{
						if (firstLine)
						{
							ParseUrl(line);
							firstLine = false;
						}
						inbox.AppendLine(line);
					}
				}
				catch (IOException)
				{
					writer.Dispose();
					return;
				}
				catch (IndexOutOfRangeException)
				{
					Write400Error(writer, null);
					return;
				}

				
				requestHeaders = ParseHeaders(inbox.ToString());
				persistentConnection = IsPersistent();

				if (httpMethod != "GET" && httpMethod != "POST")
				{
					Write405Error(writer, null);
					return;
				}

				if (httpMethod == "POST")
				{
					try
					{
						int inboxContentLength;
						int.TryParse(GetHeaderValue("Content-Length"), out inboxContentLength);
						if (inboxContentLength > 0)
						{
							if (GetHeaderValue("Content-Type").StartsWith("multipart/form-data"))
							{
								binaryRequest = new byte[inboxContentLength];
								reader.BaseStream.Read(binaryRequest, 0, inboxContentLength);
							}
							else
							{
								var buffer = new char[inboxContentLength];
								reader.Read(buffer, 0, inboxContentLength);
								requestBody = new string(buffer);
							}
						}
					}
					catch (IOException)
					{
						writer.Dispose();
						return;
					}
				}

				if (string.IsNullOrEmpty(inbox.ToString()))
				{
					Write400Error(writer, null);
					return;
				}


				GetAuthorization();
				if (ci.AuthenticationMethod != AuthenticationMethod.None && string.IsNullOrEmpty(ci.LogonUser))
				{
					Write401Error(writer, null);
					return;
				}


				var context = new HttpContext(this);
				GetCookies(context);

				Type handlerType = Router.GetHandler(path);
				if (handlerType == null)
				{
					Write404Error(writer, null);
					return;
				}
				var page = HandlerActivator.Instance.GetHandler(handlerType);
				try
				{
					contentType = "text/html";
					encodingString = "utf-8";
					page.ProcessRequest(new HttpApplicationRuntime(context));
				}
				catch (Exception)
				{
					Write500Error(writer, null);
					return;
				}

				string response = outputStream.ToString();

				WriteOutput(writer, response, persistentConnection, context, null);
				
			} while (persistentConnection);
			writer.Dispose();
		}

		void WriteOutput(StreamWriter writer, string response, bool persistentConnection, HttpContext context,
			 Action<StreamWriter> action)
		{
			bool binaryOutput = ms != null && ms.Length > 0;
			writer.WriteLine("HTTP/1.1 " + statusCode + " " + statusDescription);
			writer.WriteLine("Date: " + DateFormatter.FormatDateTimeGmt(DateTime.UtcNow));
			writer.WriteLine("Expires: Thu, 01 Jan 1970 00:00:01 GMT");
			writer.WriteLine("Server: " + Server.WebServerSoftwareName);
			writer.WriteLine("Cache-Control: no-cache, no-store, must-revalidate");
			writer.WriteLine("Pragma: no-cache");
			if (binaryOutput)
				writer.WriteLine("Content-Length: " + ms.Length);
			else
				writer.WriteLine("Content-Length: " + Encoding.UTF8.GetByteCount(response));
			writer.WriteLine("Connection: " + (persistentConnection ? "keep-alive" : "close"));
			writer.WriteLine("Content-Type: " + contentType + (contentType.Contains("text/") ? "; charset=" + encodingString : string.Empty));
			if (context != null)
			{
				if (context.Cookie.Count > 0)
					foreach (HttpCookieEntry cookie in context.Cookie)
						writer.WriteLine(string.Format("Set-Cookie: {0}", cookie));
				if (context.Session.Entries != null)
					writer.WriteLine("Set-Cookie: " + SessionCookieKey + "=" + context.Session.Sid);
			}
			if (extraHeaders != null && extraHeaders.Count > 0)
				foreach (string extraHeader in extraHeaders)
					writer.WriteLine(extraHeader);
			if (action != null) action(writer);
			writer.WriteLine();
			if (binaryOutput)
			{
				writer.Flush();
				ms.Position = 0;
				ms.WriteTo(writer.BaseStream);
			}
			else
			{
				writer.WriteLine(response);
				writer.WriteLine();
			}
			writer.Flush();
		}

		void WriteError(StreamWriter writer, int code, string status, string html, HttpContext context, Action<StreamWriter> action)
		{
			try
			{
				ms.Close();
				ms = null;
				if (extraHeaders != null)
					extraHeaders.Clear();
				ContentType = "text/html";
				EncodingString = "UTF-8";
				StatusCode = code;
				StatusDescription = status;
				string content = string.Format("<html><head><title>{0} - {1}</title></head><body>{2}</body></html>",
					code, status, html);
				WriteOutput(writer, content, false, context, action);
			}
			catch (IOException) { }
			writer.Dispose();
		}

		void Write500Error(StreamWriter writer, HttpContext context)
		{
			WriteError(writer, 500, "Internal Server Error", "<h1>Sorry, server is unavailable now, try later...</h1>", context, null);
		}

		void Write401Error(StreamWriter writer, HttpContext context)
		{
			WriteError(writer, 401, "Unauthorised", "<h1>Unauthorised</h1>", context, 
				delegate
				{
					switch (ci.AuthenticationMethod)
					{
						case AuthenticationMethod.Basic:
							writer.WriteLine("WWW-Authenticate: Basic realm=\"" + GetHeaderValue("Host") + "\"");
							break;
						case AuthenticationMethod.Digest:
							writer.WriteLine("WWW-Authenticate: " + ComposeDigestAuthenticationRequest());
							break;
					}
				});			
		}

		void Write400Error(StreamWriter writer, HttpContext context)
		{
			WriteError(writer, 400, "Bad Request", "<h1>Bad request</h1>", context, null);
		}

		void Write404Error(StreamWriter writer, HttpContext context)
		{
			WriteError(writer, 404, "Not found", "<h1>Resource not found</h1>", context, null);
		}

		void Write405Error(StreamWriter writer, HttpContext context)
		{
			WriteError(writer, 405, "Method Not Allowed", "<h1>Method not allowed</h1>", context, null);
		}

		void ParseUrl(string line)
		{
			string[] parts = line.Split(' ');
			httpMethod = parts[0];
			path = parts[1];
			string[] pathParts = path.Split('?');
			path = pathParts[0];
			queryString = pathParts.Length > 1 ? pathParts[1] : string.Empty;
		}

		public string Path
		{
			get { return path; }
		}

		public string QueryString
		{
			get { return queryString; }
		}

		public string RequestBody
		{
			get { return requestBody; }
		}

		public byte[] BinaryRequest
		{
			get { return binaryRequest; }
		}

		public string HttpMethod
		{
			get { return httpMethod; }
		}

		public void AddHeader(string key, string value)
		{
			if (extraHeaders == null) extraHeaders = new List<string>();
			extraHeaders.Add(string.Format("{0}: {1}", key, value));
		}

		public int StatusCode
		{
			get { return statusCode; }
			set { statusCode = value; }
		}

		public string StatusDescription
		{
			get { return statusDescription; }
			set { statusDescription = value; }
		}

		public string ContentType
		{
			get { return contentType; }
			set { contentType = value; }
		}

		public string EncodingString
		{
			get { return encodingString; }
			set { encodingString = value; }
		}

		static string[] ParseHeaders(string headers)
		{
			return headers.Replace("\r", string.Empty).Split('\n');
		}

		public string GetHeaderValue(string key)
		{
			string value;
			if (requestHeadersDictionary.TryGetValue(key, out value))
				return value;
			foreach (string line in requestHeaders) 
			{
				if (line.StartsWith(key)) 
				{
					string[] keyvalue = line.Split(':');
					if (keyvalue[0].Trim() == key)
						if (keyvalue.Length > 1)
						{
							value = keyvalue[1].Trim();
							requestHeadersDictionary.Add(key, value);
							return value;
						}
				}
			}
			return null;
		}

		void GetCookies(HttpContext context)
		{
			HttpCookie cookies = context.Cookie;
			foreach (string headerLine in requestHeaders)
			{
				if (headerLine.StartsWith("Cookie: "))
				{
					string[] scookies = headerLine.Substring(8).Split(';');
					foreach (string scookie in scookies)
					{
						string[] keyvaluepair = scookie.Split('=');
						if (keyvaluepair[0].Trim() == SessionCookieKey)
							InitializeSession(context, keyvaluepair[1]);
						else
							cookies[keyvaluepair[0].Trim()] = new HttpCookieEntry(
								keyvaluepair.Length > 0 ? keyvaluepair[1].Trim() : string.Empty, string.Empty);
					}
				}
			}
		}

		string ComposeDigestAuthenticationRequest()
		{
			var digest = new DigestAuthenticationProcessor(ci.RemoteIpAddress.ToString(), GetHeaderValue("Host"));
			return digest.ComposeRequest();
		}

		void GetAuthorization()
		{
			if (ConnectionInformation.AuthenticationMethod == AuthenticationMethod.None) return;
			string authorizationLine = GetHeaderValue("Authorization");
			if (string.IsNullOrEmpty(authorizationLine)) return;
			string[] split = authorizationLine.Split(' ');
			if (split.Length < 2) return;
			AuthenticationEventArgs e;
			switch (split[0])
			{
				case "Basic":
					byte[] buffer = Convert.FromBase64String(split[1]);
					string[] userpass = Encoding.ASCII.GetString(buffer).Split(':');
					e = new AuthenticationEventArgs(userpass[0], string.Empty, AuthenticationMethod.Basic);
					OnAuthenticateHandler(e);
					if (e.Accept && e.Password == userpass[1])
					{
						ci.LogonUser = e.Login;
						ci.AuthPassword = e.Password;
					}
					break;
				case "Digest":
					if (split.Length < 3)
					{
						split = split[1].Split(',').Select(s => s.Trim()).ToArray();
					}
					var digest = new DigestAuthenticationProcessor(split);
					digest.Method = httpMethod;
					e = new AuthenticationEventArgs(digest.Username, string.Empty, AuthenticationMethod.Digest);
					OnAuthenticateHandler(e);
					if (e.Accept && digest.CheckValid(e.Password))
					{
						ci.LogonUser = e.Login;
						ci.AuthPassword = e.Password;
					}
					break;
			}

		}

		void InitializeSession(HttpContext context, string id)
		{
			if (!SessionStore.ContainsKey(id))
				return;
			context.Session.Entries = SessionStore[id];
			context.Session.Sid = id;
			context.Session.Entries.UpdateLastAccess();
		}

		public SessionStore SessionStore
		{
			get { return webServer.SessionStore; }
		}

		public ApplicationStoreUnit ApplicationStore
		{
			get { return webServer.ApplicationStore; }			
		}

		public string[] RequestHeaders
		{
			get { return requestHeaders; }
		}

		public ConnectionInformation ConnectionInformation
		{
			get { return ci; }
		}

		RequestRouter Router
		{
			get { return webServer.RequestRouter; }
		}

		bool IsPersistent()
		{
			string connection = GetHeaderValue("Connection");
			return (!string.IsNullOrEmpty(connection) && connection.ToLower() == "keep-alive" &&
				webServer.PersistentConnections);
		}
		
	}
}
