using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Web;

namespace Nugget.Server
{
    public class ServerHandshake
    {
        public string Origin { get; set; }
        public string Location { get; set; }
        public string SubProtocol { get; set; }
        public string Accept { get; set; }
    }
}
