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

namespace WebServer.HttpServer
{
	public class HttpSession
	{
		readonly HttpContext context;
		internal SessionStoreUnit Entries;
		internal string Sid;

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
				return Entries[key];
			}
			set
			{
				ValidateSession();
				if (Entries.ContainsKey(key)) Entries[key] = value;
				else Entries.Add(key, value);
			}
		}

		public bool Exists(string key)
		{
			ValidateSession();
			return Entries.ContainsKey(key);
		}

		public void Remove(string key)
		{
			ValidateSession();
			Entries.Remove(key);
		}

		public int Count
		{
			get
			{
				ValidateSession();
				return Entries.Count;
			}
		}

		void ValidateSession()
		{
			if (Entries == null)
			{
				Sid = Guid.NewGuid().ToString("N");
				Entries = new SessionStoreUnit();
				if (context != null)
				{
					context.WorkingProcess.SessionStore.Add(Sid, Entries);
					context.WorkingProcess.OnSessionStartHandler(this);
				}
				
			}
		}

		public string SessionId
		{
			get { return Sid; }
		}


	}
}
