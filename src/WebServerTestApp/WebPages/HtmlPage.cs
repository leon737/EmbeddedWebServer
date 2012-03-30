using System.Collections;
using System.Reflection;
using System.Linq;
using WebServer.WebPages;
using System.IO;
using NVelocity.App;
using Commons.Collections;
using NVelocity;
using System.Diagnostics;

namespace WebServerTestApp.WebPages
{
	class HtmlPage : WebPageBase
	{
		static readonly VelocityEngine Engine;
		private static string template;

		static HtmlPage()
		{
			

			Engine = new VelocityEngine();
			//Engine = NVelocityTemplateEngine.NVelocityEngineFactory.CreateNVelocityAssemblyEngine(Assembly.GetExecutingAssembly().GetName().Name, false);
			//Engine = NVelocityTemplateEngine.NVelocityEngineFactory.CreateNVelocityMemoryEngine(true);
			var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("WebServerTestApp.Views.default.aspx.htm");
			var sr = new StreamReader(stream);
			template = sr.ReadToEnd();
			sr.Dispose();
			ExtendedProperties props = new ExtendedProperties();
			Engine.Init(props);

		}

		protected override void ProcessPage()
		{
			//if (Request.Path == "/upload.aspx")
			//{
			//   Upload();
			//}



			var context =
				new VelocityContext(new Hashtable
				   {
				      {"sv", Request.ServerVariables.Select(p => new {key = p.Key, value = p.Value ?? ""}).ToList()}
				   });

			string result = "";
			using (var writer = new StringWriter())
			{
				Engine.Evaluate(context, writer, "", template);
				result = writer.GetStringBuilder().ToString();
			}
			Response.Write(result);
		}
	}
}
