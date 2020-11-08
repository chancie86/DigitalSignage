using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net;
using System.Runtime.Serialization.Json;
using System.Text;

namespace Display
{
    public class RestClient
    {
        private const string HttpGetMethod = "GET";
        private const string HttpPostMethod = "POST";
        public const string TextType = "text/xml";
        public const string JsonType = "application/json";

        public RestClient(string contentType = null, string acceptType = null)
        {
            ContentType = contentType;
            AcceptHeader = acceptType;
            Method = HttpMethod.Get;
        }

        public string ContentType { get; set; }
        public HttpMethod Method { get; set; }
        public string AcceptHeader { get; private set; }

        /// <summary>
        /// Makes a REST call.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="address">The url to make the request on</param>
        /// <param name="method">The REST method to call</param>
        /// <param name="content">The body of the request</param>
        /// <param name="parameters">Query string parameters to build into the address</param>
        /// <param name="headers">Headers to add to the http request</param>
        /// <returns></returns>
        public T MakeRequest<T>(string address, string method, string content, IDictionary<string, string> parameters, IDictionary<string, string> headers = null)
            where T : class
        {
            var requestAddress = BuildRequestAddress(address, method, parameters);
            return MakeRequest<T>(requestAddress, content, headers);
        }

        /// <summary>
        /// Makes a REST call.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="requestAddress">The full request address including REST method and query parameters</param>
        /// <param name="content">The body of the request</param>
        /// <param name="headers">Headers to add to the http request</param>
        /// <returns></returns>
        public T MakeRequest<T>(string requestAddress, string content,
            IDictionary<string, string> headers = null)
            where T : class
        {
            var webRequest = WebRequest.Create(requestAddress);
            webRequest.ContentLength = 0;
            webRequest.ContentType = ContentType;

            switch (Method)
            {
                case HttpMethod.Post:
                    webRequest.Method = HttpPostMethod;
                    break;
                default:
                    webRequest.Method = HttpGetMethod;
                    break;
            }

            if (!string.IsNullOrWhiteSpace(AcceptHeader))
            {
                ((HttpWebRequest) webRequest).Accept = AcceptHeader;
            }

            if (headers != null)
            {
                foreach (var header in headers)
                {
                    webRequest.Headers.Add(header.Key, header.Value);
                }
            }

            webRequest.ContentLength = 0;
            webRequest.ContentType = ContentType;

            if (!string.IsNullOrEmpty(content))
            {
                var encoding = new ASCIIEncoding();
                var contentData = encoding.GetBytes(content);
                webRequest.ContentLength = contentData.Length;
                using (var contentStream = webRequest.GetRequestStream())
                {
                    contentStream.Write(contentData, 0, contentData.Length);
                }

                Log.TraceMsg("Message content: {0}", content);
            }

            using (var webResponse = (HttpWebResponse)webRequest.GetResponse())
            {
                if (webResponse.StatusCode != HttpStatusCode.OK)
                {
                    throw new Exception(string.Format(CultureInfo.InvariantCulture,
                        "HTTP request failed. Request: {0}, Status Code: {1}, Status Description: {2}",
                        requestAddress,
                        webResponse.StatusCode,
                        webResponse.StatusDescription));
                }

                using (var responseStream = webResponse.GetResponseStream())
                {
                    if (responseStream == null)
                        throw new Exception("Invalid response");

                    var jsonSerializer = new DataContractJsonSerializer(typeof(T));
                    var objResponse = jsonSerializer.ReadObject(responseStream);
                    return objResponse as T;
                }
            }
        }

        public static string BuildRequestAddress(string address, string method, IDictionary<string, string> parameters)
        {
            var requestAddressBuilder = new StringBuilder();
            requestAddressBuilder.Append(address.TrimEnd('/'));
            requestAddressBuilder.Append('/'); // Ensure there is only a single /

            if (!string.IsNullOrWhiteSpace(method))
                requestAddressBuilder.Append(method.TrimStart('/'));

            if (parameters != null
                && parameters.Count != 0)
            {
                var first = true;

                foreach (var kvp in parameters)
                {
                    if (first)
                    {
                        requestAddressBuilder.Append('?');
                        first = false;
                    }
                    else
                    {
                        requestAddressBuilder.Append('&');
                    }

                    requestAddressBuilder.Append(kvp.Key);
                    requestAddressBuilder.Append('=');

                    if (kvp.Value != null)
                        requestAddressBuilder.Append(kvp.Value);
                }
            }

            return requestAddressBuilder.ToString();
        }
    }
}
