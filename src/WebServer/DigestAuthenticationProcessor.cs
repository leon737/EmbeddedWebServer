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
using System.Text;
using System.Security.Cryptography;

namespace WebServer
{
	class DigestAuthenticationProcessor
	{
		readonly public string Username = string.Empty;
		readonly public string Realm = string.Empty;
		readonly public string Nonce = string.Empty;
		readonly public string Cnonce = string.Empty;
		readonly public string Nc = string.Empty;
		readonly public string Uri = string.Empty;
		readonly public string Response = string.Empty;
		readonly public string Opaque = string.Empty;
		readonly public string Qop = string.Empty;
		readonly string ipAddress;

		public string Method = string.Empty;

		public DigestAuthenticationProcessor(IEnumerable<string> components)
		{
			foreach (string c in components.Select(component => component.TrimEnd(',')))
			{
				if (c.StartsWith("username"))
					Username = GetValue(c);
				else if (c.StartsWith("realm"))
					Realm = GetValue(c);
				else if (c.StartsWith("nonce"))
					Nonce = GetValue(c); 
				else if (c.StartsWith("uri"))
					Uri = GetValue(c);
				else if (c.StartsWith("response"))
					Response = GetValue(c);
				else if (c.StartsWith("opaque"))
					Opaque = GetValue(c);
				else if (c.StartsWith("cnonce"))
					Cnonce = GetValue(c);
				else if (c.StartsWith("nc"))
					Nc = GetValue(c);
				else if (c.StartsWith("qop"))
					Qop = GetValue(c);
			}
		}

		public DigestAuthenticationProcessor(string remoteIpAddress, string realm)
		{
			ipAddress = remoteIpAddress;
			Realm = realm;
		}

		public bool CheckValid(string password)
		{
			string a1 = string.Format("{0}:{1}:{2}", Username, Realm, password);
			string a2 = string.Format("{0}:{1}", Method, Uri);

			MD5 md5 = new MD5CryptoServiceProvider();
			md5.Initialize();
			byte[] ha1 = md5.ComputeHash(Encoding.ASCII.GetBytes(a1));
			byte[] ha2 = md5.ComputeHash(Encoding.ASCII.GetBytes(a2));
			string r = string.Format("{0}:{1}:{2}:{3}:{4}:{5}",
				BitConverter.ToString(ha1).Replace("-", "").ToLower(), 
				Nonce,
				Nc,
				Cnonce,
				Qop,
				BitConverter.ToString(ha2).Replace("-", "").ToLower());
			byte[] hr = md5.ComputeHash(Encoding.ASCII.GetBytes(r));
			string calculatedResponse = BitConverter.ToString(hr).Replace("-", "").ToLower();
			return calculatedResponse == Response;
		}

		public string ComposeRequest()
		{
			string nonce = ipAddress + ":" + DateTime.Now.ToUniversalTime().ToString("R");
			MD5 md5 = new MD5CryptoServiceProvider();
			md5.Initialize();
			byte[] h = md5.ComputeHash(Encoding.ASCII.GetBytes(nonce));
			nonce = BitConverter.ToString(h).Replace("-", "").ToLower();
			string opaque = "nfoiwero8ur0 ofidosfuoewrf oieufo sedoif ";
			h = md5.ComputeHash(Encoding.ASCII.GetBytes(opaque));
			opaque = BitConverter.ToString(h).Replace("-", "").ToLower();
			return string.Format("Digest qop=auth, nonce=\"{0}\", realm=\"{1}\", opaque=\"{2}\"",
				nonce, Realm, opaque);
		}

		static string GetValue(string s)
		{
			return s.Split('=')[1].Trim('"');
		}


	}
}
