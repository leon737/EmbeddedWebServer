using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace EmbeddedWebServer
{
	public class HttpCookie : IEnumerable<HttpCookieEntry>
	{
		HttpContext context;
		Dictionary<string, HttpCookieEntry> cookies;

		internal HttpCookie(HttpContext context)
		{
			this.context = context;
			cookies = new Dictionary<string, HttpCookieEntry>();
		}

		public IEnumerator<HttpCookieEntry> GetEnumerator()
		{
			foreach (HttpCookieEntry e in cookies.Values)
				yield return e;
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}

		public HttpCookieEntry this[string key]
		{
			get { return cookies[key]; }
			set
			{
				if (string.IsNullOrEmpty(value.Key)) value.Key = key;
				if (cookies.ContainsKey(key)) cookies[key] = value;
				else cookies.Add(key, value);
			}
		}

		public bool Exists(string key)
		{
			return cookies.ContainsKey(key);
		}

		public void Remove(string key)
		{
			cookies.Remove(key);
		}

		public int Count
		{
			get { return cookies.Count; }
		}

		internal HttpCookieEntry[] ToArray()
		{
			HttpCookieEntry[] array = new HttpCookieEntry[cookies.Count];
			int index = 0;
			foreach (HttpCookieEntry e in cookies.Values)
				array[index++] = e;
			return array;
		}
		
	}

	public class HttpCookieEntry
	{
		string key;
		string value;
		string path;

		internal HttpCookieEntry (string key, string value, string path) 
		{
			this.key = key;
			this.value = value;
			this.path = path;
		}

		public HttpCookieEntry(string value, string path)
		{
			this.value = value;
			this.path = path;
		}

		public string Key 
		{
			get { return key;}
			internal set { key = value; }
		}

		public string Value
		{
			get { return value; }
			set { this.value = value; }
		}

		public string Path
		{
			get { return path; }
			set { path = value; }
		}

		public override string ToString()
		{
			string result = key + "=" + value;
			if (!string.IsNullOrEmpty(path))
				result += ";path=" + path;
			return result;
		}
	}
}
