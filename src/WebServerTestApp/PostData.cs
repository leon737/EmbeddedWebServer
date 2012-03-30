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

namespace WebServerTestApp
{
	public class PostData
	{

		private List<PostDataParam> m_Params;

		public List<PostDataParam> Params
		{
			get { return m_Params; }
			set { m_Params = value; }
		}

		public PostData()
		{
			m_Params = new List<PostDataParam>();

			// Add sample param
			m_Params.Add(new PostDataParam("email", "MyEmail", PostDataParamType.Field));
		}


		/// <summary>
		/// Returns the parameters array formatted for multi-part/form data
		/// </summary>
		/// <returns></returns>
		public string GetPostData(string boundary)
		{
			StringBuilder sb = new StringBuilder();
			foreach (PostDataParam p in m_Params)
			{
				sb.AppendLine(boundary);

				if (p.Type == PostDataParamType.File)
				{
					sb.AppendLine(string.Format("Content-Disposition: file; name=\"{0}\"; filename=\"{1}\"", p.Name, p.FileName));
					sb.AppendLine("Content-Type: text/plain");
					sb.AppendLine();
					sb.AppendLine(p.Value);
				}
				else
				{
					sb.AppendLine(string.Format("Content-Disposition: form-data; name=\"{0}\"", p.Name));
					sb.AppendLine();
					sb.AppendLine(p.Value);
				}
			}

			sb.AppendLine(boundary);

			return sb.ToString();
		}
	}

	public enum PostDataParamType
	{
		Field,
		File
	}

	public class PostDataParam
	{
				public PostDataParam(string name, string value, PostDataParamType type)
		{
			Name = name;
			Value = value;
			Type = type;
		}

		public string Name;
		public string FileName;
		public string Value;
		public PostDataParamType Type;
	}

}
