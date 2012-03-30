using System;
using System.Collections.Generic;
using System.Text;

namespace EmbeddedWebServer
{
	public class HttpApplication
	{
		HttpContext context;
		internal ApplicationStoreUnit entries;

		internal HttpApplication (HttpContext context)
		{
			this.context = context;
			entries = context.WorkingProcess.ApplicationStore;
		}

		internal HttpApplication(ApplicationStoreUnit store)
		{
			context = null;
			entries = store;
		}

		public object this[string key]
		{
			get
			{
				return entries[key];
			}
			set
			{
				if (entries.ContainsKey(key)) entries[key] = value;
				else entries.Add(key, value);
			}
		}

		public bool Exists(string key)
		{
			return entries.ContainsKey(key);
		}

		public void Remove(string key)
		{
			entries.Remove(key);
		}

		public int Count
		{
			get
			{
				return entries.Count;
			}
		}

		
	}
}
