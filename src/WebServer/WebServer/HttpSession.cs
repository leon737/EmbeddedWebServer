using System;
using System.Collections.Generic;
using System.Text;

namespace EmbeddedWebServer
{
	public class HttpSession
	{
		HttpContext context;
		internal SessionStoreUnit entries;
		internal string sid;

		internal HttpSession(HttpContext context)
		{
			this.context = context;
		}

		internal HttpSession()
		{
			context = null;
		}

		public object this[string key]
		{
			get
			{
				ValidateSession();
				return entries[key];
			}
			set
			{
				ValidateSession();
				if (entries.ContainsKey(key)) entries[key] = value;
				else entries.Add(key, value);
			}
		}

		public bool Exists(string key)
		{
			ValidateSession();
			return entries.ContainsKey(key);
		}

		public void Remove(string key)
		{
			ValidateSession();
			entries.Remove(key);
		}

		public int Count
		{
			get
			{
				ValidateSession();
				return entries.Count;
			}
		}

		void ValidateSession()
		{
			if (entries == null)
			{
				sid = Guid.NewGuid().ToString("N");
				entries = new SessionStoreUnit();
				if (context != null)
				{
					context.WorkingProcess.SessionStore.Add(sid, entries);
					context.WorkingProcess.onSessionStartHandler(this);
				}
				
			}
		}

		public string SessionId
		{
			get { return sid; }
		}


	}
}
