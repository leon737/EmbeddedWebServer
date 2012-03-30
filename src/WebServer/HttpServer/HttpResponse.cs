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
	public class HttpResponse
	{
		readonly HttpContext context;

		internal HttpResponse(HttpContext context)
		{
			this.context = context;
		}

		public void Write(string value)
		{
			context.WorkingProcess.OutputStream.Append(value);
		}

		public void Write(string format, object[] args)
		{
			Write(string.Format(format, args));
		}

		public void WriteBinary(byte[] buffer)
		{
			WriteBinary(buffer, 0, buffer.Length);
		}

		public void WriteBinary(byte[] buffer, int offset, int count)
		{
			context.WorkingProcess.OutputBinaryStream.Write(buffer, offset, count);
		}

		public System.IO.Stream OutputStream
		{
			get { return context.WorkingProcess.OutputBinaryStream; }
		}


		public void AddHeader(string key, string value)
		{
			context.WorkingProcess.AddHeader(key, value);
		}

		public void Clear()
		{
			context.WorkingProcess.OutputStream.Length = 0;
		}

		public string Status
		{
			get
			{
				return (context.WorkingProcess.StatusCode + " " + context.WorkingProcess.StatusDescription);
			}
			set
			{
				int num;
				string str;
				try
				{
					int index = value.IndexOf(' ');
					num = int.Parse(value.Substring(0, index));
					str = value.Substring(index + 1);
				}
				catch
				{
					throw new Exception("Invalid_status_string");
				}
				context.WorkingProcess.StatusCode = num;
				context.WorkingProcess.StatusDescription = str;
			}
		}

		public void Redirect(string url)
		{
			if (url == null)
			{
				throw new ArgumentNullException("url");
			}
			if (url.IndexOf('\n') >= 0)
			{
				throw new ArgumentException("Cannot_redirect_to_newline");
			}		

			Clear();
			Status = "302 Object moved";
			AddHeader("Location", url);
			Write("<html><head><title>Object moved</title></head><body>\r\n");
			Write("<h2>Object moved to <a href=\"" + url + "\"></a>.</h2>\r\n");
			Write("</body></html>\r\n");
		}

		public void RedirectPermanent(string url)
		{
			if (url == null)
			{
				throw new ArgumentNullException("url");
			}
			if (url.IndexOf('\n') >= 0)
			{
				throw new ArgumentException("Cannot_redirect_to_newline");
			}

			Clear();
			Status = "301 Object permanently moved";
			AddHeader("Location", url);
			Write("<html><head><title>Object permanently moved</title></head><body>\r\n");
			Write("<h2>Object permanently moved to <a href=\"" + url + "\"></a>.</h2>\r\n");
			Write("</body></html>\r\n");
		}

		public string ContentType
		{
			get { return context.WorkingProcess.ContentType; }
			set { context.WorkingProcess.ContentType = value; }
		}

		public string EncodingString
		{
			get { return context.WorkingProcess.EncodingString; }
			set { context.WorkingProcess.EncodingString = value; }
		}

	}
}
