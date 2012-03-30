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

using System.Collections;
using System.Collections.Generic;

namespace WebServer.HttpServer
{
	public class HttpCookie : IEnumerable<HttpCookieEntry>
	{
		//HttpContext context;
		readonly Dictionary<string, HttpCookieEntry> cookies;

		internal HttpCookie(HttpContext context)
		{
			//this.context = context;
			cookies = new Dictionary<string, HttpCookieEntry>();
		}

		public IEnumerator<HttpCookieEntry> GetEnumerator()
		{
			return ((IEnumerable<HttpCookieEntry>) cookies.Values).GetEnumerator();
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
			var array = new HttpCookieEntry[cookies.Count];
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
