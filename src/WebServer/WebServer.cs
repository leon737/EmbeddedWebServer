using System;
using System.Collections.Generic;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.IO;

namespace EmbeddedWebServer
{

	public delegate void ApplicationEventHandler (object sender, ApplicationEventArgs e);
	public delegate void SessionEventHandler(object sender, SessionEventArgs e);
	internal delegate void __SessionEventHandler (HttpSession session);
	public delegate void AuthenticationEventHandler (object sender, AuthenticationEventArgs e);
	internal delegate void __AuthenticationEventHandler(AuthenticationEventArgs e);


	public class WebServer
	{
		internal const string WEB_SERVER_SOFTWARE_NAME = "EmbeddedWebServer";

		public event ApplicationEventHandler ApplicationStartEvent;
		public event ApplicationEventHandler ApplicationEndEvent;
		public event SessionEventHandler SessionStartEvent;
		public event SessionEventHandler SessionEndEvent;
		public event AuthenticationEventHandler AuthenticationEvent;

		IPAddress ipaddress;
		int port;
		TcpListener listener;
		RequestRouter router;
		SessionStore sessionStore;
		ApplicationStoreUnit applicationStore;
		int sessionTimeout = 1; //minutes
		bool resolveDnsNames = false;
		AuthenticationMethod authenticationMethod = AuthenticationMethod.None;
		bool persistentConnections = true;
		int keepAliveTimeout = 15;
		int keepAliveTimeoutMax = 100;

		public WebServer(IPAddress ipaddress, int port)
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
				IPEndPoint localEP = client.Client.LocalEndPoint as IPEndPoint;
				IPEndPoint remoteEP = client.Client.RemoteEndPoint as IPEndPoint;
				ConnectionInformation ci = new ConnectionInformation(port, localEP.Address, remoteEP.Address,
					resolveDnsNames, authenticationMethod);
				WorkingProcess process = new WorkingProcess(this, stream, ci,
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
				client.Close();
			}
			catch (ObjectDisposedException) { }
			catch (InvalidOperationException) { }
		}

		public void Stop()
		{
			listener.Stop();
			string [] keys = new string[sessionStore.Keys.Count];
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
			List<string> unitsToRemove = new List<string>();
			foreach (string key in sessionStore.Keys)
				if (sessionStore[key].IsTimedOut(sessionTimeout))
					unitsToRemove.Add(key);
			foreach (string key in unitsToRemove)
			{
				OnSessionEnd(sessionStore[key], key);
				sessionStore.Remove(key);
				
			}
		}		

		void OnApplicationStart() 
		{
			if (ApplicationStartEvent != null)
				ApplicationStartEvent(this, new ApplicationEventArgs (new HttpApplication(applicationStore)));
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
				HttpSession session = new HttpSession();
				session.entries = unit;
				session.sid = sid;
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
	}

	internal class SessionStoreUnit : Dictionary<string, object>
	{
		DateTime lastAccessDate;

		public SessionStoreUnit ()
		{
			lastAccessDate = DateTime.Now;
		}

		public bool IsTimedOut(int timeOutInterval)
		{
			return (lastAccessDate.AddMinutes(timeOutInterval) < DateTime.Now) ;
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
		int port;
		IPAddress localIpAddress;
		string localName;
		IPAddress remoteIpAddress;
		string remoteName;
		string logonUser;
		string authPassword;
		AuthenticationMethod authenticationMethod;

		public ConnectionInformation(int port, IPAddress localIPAddress, IPAddress remoteIPAddress, bool resolve,
			AuthenticationMethod authenticationMethod)
		{
			this.port = port;
			this.localIpAddress = localIPAddress;
			this.remoteIpAddress = remoteIPAddress;
			if (resolve)
			{
				if (localIPAddress.Equals(new IPAddress(0)))
					this.localName = "localhost";
				else
					this.localName = Dns.GetHostEntry(localIPAddress).HostName;
				this.remoteName = Dns.GetHostEntry(remoteIPAddress).HostName;
			}
			else
			{
				this.localName = localIPAddress.ToString();
				this.remoteName = remoteIpAddress.ToString();
			}
			this.logonUser = null;
			this.authPassword = null;
			this.authenticationMethod = authenticationMethod;
		}

		public int Port
		{
			get { return port; }
		}

		public IPAddress LocalIPAddress
		{
			get { return localIpAddress; }
		}

		public IPAddress RemoteIPAddress
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

		public string LogonUser
		{
			get { return logonUser; }
			set { logonUser = value; }
		}

		public string AuthPassword
		{
			get { return authPassword; }
			set { authPassword = value; }
		}

		public AuthenticationMethod AuthenticationMethod
		{
			get { return authenticationMethod; }
		}

	}

	public class ApplicationEventArgs : EventArgs
	{
		HttpApplication application;

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
		HttpSession session;

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
			this.Login = login;
			this.Password = password;
			this.Method = method;
			this.Accept = true;
		}
	}

	public enum AuthenticationMethod 
	{
		None,
		Basic,
		Digest
	}

}
