using System;
using System.Collections.Generic;
using System.Text;

namespace EmbeddedWebServer
{
	public class HttpResponse
	{
		HttpContext context;

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
			this.Write(string.Format(format, args));
		}

		public void WriteBinary(byte[] buffer)
		{
			this.WriteBinary(buffer, 0, buffer.Length);
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
				return (context.WorkingProcess.StatusCode.ToString() + " " + context.WorkingProcess.StatusDescription);
			}
			set
			{
				int num = 200;
				string str = "OK";
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

			this.Clear();
			this.Status = "302 Object moved";
			this.AddHeader("Location", url);
			this.Write("<html><head><title>Object moved</title></head><body>\r\n");
			this.Write("<h2>Object moved to <a href=\"" + url + "\"></a>.</h2>\r\n");
			this.Write("</body></html>\r\n");
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

			this.Clear();
			this.Status = "301 Object permanently moved";
			this.AddHeader("Location", url);
			this.Write("<html><head><title>Object permanently moved</title></head><body>\r\n");
			this.Write("<h2>Object permanently moved to <a href=\"" + url + "\"></a>.</h2>\r\n");
			this.Write("</body></html>\r\n");
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
