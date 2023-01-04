// Copyright (c) Nathan Ford. All rights reserved. ConfigModel.cs

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace Leaf2Google.Models.Generic
{
    public class Nissan
    {
        public EU EU { get; set; } = new EU();
        public string api_version { get; set; }
        public string srp_key { get; set; }
    }

    public class EU
    {
        public string client_id { get; set; }
        public string client_secret { get; set; }
        public string scope { get; set; }
        public string auth_base_url { get; set; }
        public string realm { get; set; }
        public string redirect_uri { get; set; }
        public string car_adapter_base_url { get; set; }
        public string user_adapter_base_url { get; set; }
        public string user_base_url { get; set; }
    }

    public class Fido2
    {
        public string serverDomain { get; set; }
        public string[] origins { get; set; }
        public int timestampDriftTolerance { get; set; }
    }


    public class ConfigModel : BaseModel
    {
        public Nissan Nissan { get; set; } = new Nissan();
        public Fido2 fido2 { get; set; } = new Fido2();
    }
}
