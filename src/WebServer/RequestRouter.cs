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
using System.Linq;

namespace WebServer
{
	public class RequestRouter
	{
		readonly List<RouteEntry> entries;

		public RequestRouter()
		{
			entries = new List<RouteEntry>();
		}

		public void Add(RouteEntry entry)
		{
			entries.Add(entry);
		}

		public void Remove(RouteEntry entry)
		{
			entries.Remove(entry);
		}

		public void Clear()
		{
			entries.Clear();
		}

		internal Type GetHandler(string mask)
		{
			return (from entry in entries where mask.StartsWith(entry.Path) select entry.RequestHandler).FirstOrDefault();
		}
	}

	public class RouteEntry
	{
		readonly string path;
		readonly Type requestHandler;

		public RouteEntry(string path, Type requestHandler)
		{
			this.path = path;
			if (!typeof(IWebPage).IsAssignableFrom(requestHandler)) throw new InvalidCastException();
			this.requestHandler = requestHandler;
		}

		public string Path
		{
			get { return path; }
		}

		public Type RequestHandler
		{
			get { return requestHandler; }
		}
	}
}
