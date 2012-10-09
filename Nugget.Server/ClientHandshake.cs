using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;

namespace Nugget.Server
{
    public class ClientHandshake
    {
        private Uri _uri;

        public Uri ResourceName { get {return _uri;} }
        public string Host { get; set; }
        public string Origin { get; set; }
        public string Version { get; set; }
        public string Key { get; set; }
        public HttpCookieCollection Cookies { get; private set; }
        public string SubProtocol { get; set; }
        public Dictionary<string, string> AdditionalFields { get; private set; }

        public ClientHandshake(string handshakeString)
        {
            Cookies = new HttpCookieCollection();
            AdditionalFields = new Dictionary<string, string>();

            // the "grammar" of the handshake
            var pattern = @"^(?<connect>[^\s]+)\s(?<path>[^\s]+)\sHTTP\/1\.1\r\n" +  // request line
                          @"((?<field_name>[^:\r\n]+):\s(?<field_value>[^\r\n]+)\r\n)+"; // unordered set of fields (name-chars colon space any-chars cr lf)

            // match the handshake against the "grammar"
            var regex = new Regex(pattern, RegexOptions.IgnoreCase);
            var match = regex.Match(handshakeString);
            var fields = match.Groups;

            // save the request path
            Uri.TryCreate(fields["path"].Value, UriKind.RelativeOrAbsolute, out _uri);
            
                // run through every match and save them in the handshake object
            for (int i = 0; i < fields["field_name"].Captures.Count; i++)
            {
                var name = fields["field_name"].Captures[i].ToString();
                var value = fields["field_value"].Captures[i].ToString();

                if (string.IsNullOrEmpty(value)) //discussion:244004
                {
                    continue;
                }

                switch (name.ToLowerInvariant())
                {
                    case "sec-websocket-key":
                        Key = value;
                        break;
                    case "sec-websocket-version":
                        Version = value;
                        break;
                    case "sec-websocket-protocol":
                        SubProtocol = value;
                        break;
                    case "origin":
                        Origin = value.ToLowerInvariant(); // to lower as per the protocol
                        break;
                    case "host":
                        Host = value;
                        break;
                    case "sec-websocket-extensions":
                        // TODO
                        break;
                    case "cookie":
                        // create and fill a cookie collection from the data in the handshake
                        var cookies = value.Split(';');
                        foreach (var item in cookies)
                        {
                            // the name if before the '=' char
                            var c_name = item.Remove(item.IndexOf('='));
                            // the value is after
                            var c_value = item.Substring(item.IndexOf('=') + 1);
                            // put the cookie in the collection (this also parses the sub-values and such)
                            Cookies.Add(new HttpCookie(c_name.TrimStart(), c_value));
                        }
                        break;
                    default:
                        // some field that we don't know about
                        AdditionalFields[name] = value;
                        break;
                }
            }


        }



        public bool IsValid(string origin = null, string location = null)
        {
            var valid = ResourceName != null &&
                !string.IsNullOrEmpty(Host) &&
                AdditionalFields["Upgrade"] == "websocket" &&
                AdditionalFields["Connection"].Contains("Upgrade") &&
                !string.IsNullOrEmpty(Version) && Version == "13";

            return valid;
        }
    }
}
