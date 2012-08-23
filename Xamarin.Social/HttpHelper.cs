using System;
using System.Net;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Linq;

namespace Xamarin.Social
{
	public static class HttpEx
	{
		public static string GetCookie (this CookieContainer containers, Uri domain, string name)
		{
			var c = containers
					.GetCookies (domain)
					.Cast<Cookie> ()
					.FirstOrDefault (x => x.Name == name);
			return c != null ? c.Value : "";
		}

		public static Encoding GetEncodingFromContentType (string contentType)
		{
			//
			// TODO: Parse the Content-Type
			//
			return Encoding.UTF8;
		}

		public static string GetResponseText (this WebResponse response)
		{
			var httpResponse = response as HttpWebResponse;
			
			var encoding = Encoding.UTF8;
			
			if (httpResponse != null) {
				encoding = GetEncodingFromContentType (response.ContentType);
			}
			
			using (var s = response.GetResponseStream ()) {
				using (var r = new StreamReader (s, encoding)) {
					return r.ReadToEnd ();
				}
			}
		}

		public static Task<WebResponse> GetResponseAsync (this WebRequest request)
		{
			return Task
				.Factory
				.FromAsync<WebResponse> (request.BeginGetResponse, request.EndGetResponse, null);
		}

		public static string FormEncode (this IDictionary<string, string> inputs)
		{
			var sb = new StringBuilder ();
			var head = "";
			foreach (var p in inputs) {
				sb.Append (head);
				sb.Append (Uri.EscapeDataString (p.Key));
				sb.Append ("=");
				sb.Append (Uri.EscapeDataString (p.Value));
				head = "&";
			}
			return sb.ToString ();
		}

		static char[] AmpersandChars = new char[] { '&' };
		static char[] EqualsChars = new char[] { '=' };

		public static Dictionary<string, string> FormDecode (string encodedString)
		{
			var inputs = new Dictionary<string, string> ();

			if (encodedString.StartsWith ("?")) {
				encodedString = encodedString.Substring (1);
			}

			var parts = encodedString.Split (AmpersandChars);
			foreach (var p in parts) {
				var kv = p.Split (EqualsChars);
				var k = Uri.UnescapeDataString (kv[0]);
				var v = kv.Length > 1 ? Uri.UnescapeDataString (kv[1]) : "";
				inputs[k] = v;
			}

			return inputs;
		}
	}


	public class HttpHelper
	{
		public CookieContainer Cookies { get; private set; }

		public HttpHelper ()
			: this (new CookieContainer ())
		{
		}

		public HttpHelper (CookieContainer cookies)
		{
			this.Cookies = cookies;
		}

		public HttpWebRequest CreateHttpWebRequest (string method, string url)
		{
			var req = (HttpWebRequest)WebRequest.Create (url);
			req.Method = method;
			req.CookieContainer = Cookies;
			return req;
		}

		public Task<WebResponse> GetAsync (string url)
		{
			return CreateHttpWebRequest ("GET", url).GetResponseAsync ();
		}

		public Task<WebResponse> PostUrlFormEncodedAsync (string url, IDictionary<string, string> inputs)
		{
			var req = CreateHttpWebRequest ("POST", url);

			var body = inputs.FormEncode ();
			var bodyData = System.Text.Encoding.UTF8.GetBytes (body);
			req.ContentLength = bodyData.Length;
			req.ContentType = "application/x-www-form-urlencoded";

			return Task.Factory
				.FromAsync<Stream> (req.BeginGetRequestStream, req.EndGetRequestStream, null)
				.ContinueWith (ts => {
					using (ts.Result) {
						ts.Result.Write (bodyData, 0, bodyData.Length);
					}
					return req.GetResponseAsync ().Result;
				});
		}
	}
}

