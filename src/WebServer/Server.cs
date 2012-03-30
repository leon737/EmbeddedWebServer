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
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Security.Authentication;
using WebServer.HttpServer;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using WebServer.Security;

namespace WebServer
{

	public delegate void ApplicationEventHandler(object sender, ApplicationEventArgs e);
	public delegate void SessionEventHandler(object sender, SessionEventArgs e);
	internal delegate void CoreSessionEventHandler(HttpSession session);
	public delegate void AuthenticationEventHandler(object sender, AuthenticationEventArgs e);
	internal delegate void CoreAuthenticationEventHandler(AuthenticationEventArgs e);


	public class Server
	{
		internal const string WebServerSoftwareName = "EmbeddedWebServer";

		public event ApplicationEventHandler ApplicationStartEvent;
		public event ApplicationEventHandler ApplicationEndEvent;
		public event SessionEventHandler SessionStartEvent;
		public event SessionEventHandler SessionEndEvent;
		public event AuthenticationEventHandler AuthenticationEvent;

		readonly IPAddress ipaddress;
		readonly int port;
		TcpListener listener;
		readonly RequestRouter router;
		readonly SessionStore sessionStore;
		readonly ApplicationStoreUnit applicationStore;
		int sessionTimeout = 1; //minutes
		bool resolveDnsNames;
		AuthenticationMethod authenticationMethod = AuthenticationMethod.None;
		bool persistentConnections = true;
		int keepAliveTimeout = 15;
		int keepAliveTimeoutMax = 100;
		private bool useSsl;
		private bool clientCertificateRequired;
		private X509Certificate certificate;

		public Server(IPAddress ipaddress, int port)
		{
			this.ipaddress = ipaddress;
			this.port = port;
			router = new RequestRouter();
			sessionStore = new SessionStore();
			applicationStore = new ApplicationStoreUnit();
		}

		public RequestRouter RequestRouter
		{
			get { return router; }
		}

		public int SessionTimeout
		{
			get { return sessionTimeout; }
			set { sessionTimeout = value; }
		}

		public bool PersistentConnections
		{
			get { return persistentConnections; }
			set { persistentConnections = value; }
		}

		public int KeepAliveTimeout
		{
			get { return keepAliveTimeout; }
			set
			{
				if (value <= 0) throw new ArgumentOutOfRangeException();
				keepAliveTimeout = value;
			}
		}

		public int KeepAliveTimeoutMax
		{
			get { return keepAliveTimeoutMax; }
			set
			{
				if (value <= 0) throw new ArgumentOutOfRangeException();
				keepAliveTimeoutMax = value;
			}
		}

		public bool ResolveDnsNames
		{
			get { return resolveDnsNames; }
			set { resolveDnsNames = value; }
		}

		public AuthenticationMethod AuthenticationMethod
		{
			get { return authenticationMethod; }
			set { authenticationMethod = value; }
		}

		public bool UseSsl { get { return useSsl; } set { useSsl = value; } }

		public void SetCertificatePath (string certificatePath, string password)
		{
			certificate = CertificateManager.Initialize(new CertificateConfig
			    {FilePath = certificatePath, IsEnabled = true, Password = password});
		}

		public void Start()
		{
			listener = new TcpListener(ipaddress, port);
			listener.Start();
			OnApplicationStart();
			listener.BeginAcceptTcpClient(OnAcceptTcpClient, null);
		}

		public void OnAcceptTcpClient(IAsyncResult result)
		{
			try
			{
				listener.BeginAcceptTcpClient(OnAcceptTcpClient, null);
				TcpClient client = listener.EndAcceptTcpClient(result);
				NetworkStream stream = client.GetStream();
				stream.ReadTimeout = keepAliveTimeout * 1000;
				var localEp = client.Client.LocalEndPoint as IPEndPoint;
				var remoteEp = client.Client.RemoteEndPoint as IPEndPoint;
				if (localEp != null && remoteEp != null)
				{
					var ci = new ConnectionInformation(port, localEp.Address, remoteEp.Address,
					                                   resolveDnsNames, authenticationMethod);
					try
					{
						var process = new WorkingProcess(this, useSsl ? GetSslStream(stream) : stream, ci,
								delegate(HttpSession session)
								{
									if (SessionStartEvent != null)
										SessionStartEvent(this, new SessionEventArgs(session));
								},
								delegate(AuthenticationEventArgs e)
								{
									if (AuthenticationEvent != null)
										AuthenticationEvent(this, e);
								}
						);
						ShutTimedSessions();
						process.Run();
					}
					catch (AuthenticationException)
					{
							
					}
				}
				client.Close();
			}
			catch (ObjectDisposedException) { }
			catch (InvalidOperationException) { }
		}


		public void Stop()
		{
			listener.Stop();
			var keys = new string[sessionStore.Keys.Count];
			sessionStore.Keys.CopyTo(keys, 0);
			foreach (string key in keys)
			{
				OnSessionEnd(sessionStore[key], key);
				sessionStore.Remove(key);
			}
			OnApplicationEnd();
		}

		void ShutTimedSessions()
		{
			var unitsToRemove = sessionStore.Keys.Where(key => sessionStore[key].IsTimedOut(sessionTimeout)).ToList();
			foreach (string key in unitsToRemove)
			{
				OnSessionEnd(sessionStore[key], key);
				sessionStore.Remove(key);

			}
		}

		void OnApplicationStart()
		{
			if (ApplicationStartEvent != null)
				ApplicationStartEvent(this, new ApplicationEventArgs(new HttpApplication(applicationStore)));
		}

		void OnApplicationEnd()
		{
			if (ApplicationEndEvent != null)
				ApplicationEndEvent(this, new ApplicationEventArgs(new HttpApplication(applicationStore)));
		}

		void OnSessionEnd(SessionStoreUnit unit, string sid)
		{
			if (SessionEndEvent != null)
			{
				var session = new HttpSession {Entries = unit, Sid = sid};
				SessionEndEvent(this, new SessionEventArgs(session));
			}
		}

		internal SessionStore SessionStore
		{
			get { return sessionStore; }
		}

		internal ApplicationStoreUnit ApplicationStore
		{
			get { return applicationStore; }
		}

		public bool ClientCertificateRequired
		{
			get { return clientCertificateRequired; }
			set { clientCertificateRequired = value; }
		}

		private Stream GetSslStream(Stream stream)
		{
			var sslStream = new SslStream(stream, false);
			sslStream.AuthenticateAsServer(certificate, false, SslProtocols.Tls, false);
			return sslStream;
		}

	}

	internal class SessionStoreUnit : Dictionary<string, object>
	{
		DateTime lastAccessDate;

		public SessionStoreUnit()
		{
			lastAccessDate = DateTime.Now;
		}

		public bool IsTimedOut(int timeOutInterval)
		{
			return (lastAccessDate.AddMinutes(timeOutInterval) < DateTime.Now);
		}

		public void UpdateLastAccess()
		{
			lastAccessDate = DateTime.Now;
		}
	}


	internal class SessionStore : Dictionary<string, SessionStoreUnit> { }

	internal class ApplicationStoreUnit : Dictionary<string, object> { }

	internal class ConnectionInformation
	{
		readonly int port;
		readonly IPAddress localIpAddress;
		readonly string localName;
		readonly IPAddress remoteIpAddress;
		readonly string remoteName;
		readonly AuthenticationMethod authenticationMethod;

		public ConnectionInformation(int port, IPAddress localIpAddress, IPAddress remoteIpAddress, bool resolve,
			AuthenticationMethod authenticationMethod)
		{
			this.port = port;
			this.localIpAddress = localIpAddress;
			this.remoteIpAddress = remoteIpAddress;
			if (resolve)
			{
				localName = IPAddress.IsLoopback(localIpAddress) ? "localhost" : Dns.GetHostEntry(localIpAddress).HostName;
				remoteName = Dns.GetHostEntry(remoteIpAddress).HostName;
			}
			else
			{
				localName = localIpAddress.ToString();
				remoteName = remoteIpAddress.ToString();
			}
			LogonUser = null;
			AuthPassword = null;
			this.authenticationMethod = authenticationMethod;
		}

		public int Port
		{
			get { return port; }
		}

		public IPAddress LocalIpAddress
		{
			get { return localIpAddress; }
		}

		public IPAddress RemoteIpAddress
		{
			get { return remoteIpAddress; }
		}

		public string LocalName
		{
			get { return localName; }
		}

		public string RemoteName
		{
			get { return remoteName; }
		}

		public string LogonUser { get; set; }

		public string AuthPassword { get; set; }

		public AuthenticationMethod AuthenticationMethod
		{
			get { return authenticationMethod; }
		}

	}

	public class ApplicationEventArgs : EventArgs
	{
		readonly HttpApplication application;

		internal ApplicationEventArgs(HttpApplication application)
		{
			this.application = application;
		}

		public HttpApplication Application
		{
			get { return application; }
		}
	}

	public class SessionEventArgs : EventArgs
	{
		readonly HttpSession session;

		internal SessionEventArgs(HttpSession session)
		{
			this.session = session;
		}

		public HttpSession Session
		{
			get { return session; }
		}

	}

	public class AuthenticationEventArgs : EventArgs
	{
		public string Login;
		public string Password;
		public bool Accept;
		public AuthenticationMethod Method;


		internal AuthenticationEventArgs(string login, string password, AuthenticationMethod method)
		{
			Login = login;
			Password = password;
			Method = method;
			Accept = true;
		}
	}

	public enum AuthenticationMethod
	{
		None,
		Basic,
		Digest
	}

}
