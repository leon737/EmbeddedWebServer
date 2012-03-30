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

namespace WebServer.HttpServer
{
	public class HttpApplication
	{
		//HttpContext context;
		internal ApplicationStoreUnit Entries;

		internal HttpApplication (HttpContext context)
		{
			//this.context = context;
			Entries = context.WorkingProcess.ApplicationStore;
		}

		internal HttpApplication(ApplicationStoreUnit store)
		{
			//context = null;
			Entries = store;
		}

		public object this[string key]
		{
			get
			{
				return Entries[key];
			}
			set
			{
				if (Entries.ContainsKey(key)) Entries[key] = value;
				else Entries.Add(key, value);
			}
		}

		public bool Exists(string key)
		{
			return Entries.ContainsKey(key);
		}

		public void Remove(string key)
		{
			Entries.Remove(key);
		}

		public int Count
		{
			get
			{
				return Entries.Count;
			}
		}

		
	}
}
