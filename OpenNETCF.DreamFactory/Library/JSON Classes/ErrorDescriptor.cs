using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RestSharp.Serializers;
using RestSharp.Deserializers;

namespace OpenNETCF.DreamFactory
{
    [SerializeAs(Name = "error")]
//    [DeserializeAs(Name = "error")]
    public class ErrorDescriptor
    {
        public ErrorDescriptor()
        {
        }

        public string message { get; set; }
        public int code { get; set; }

        // {"error":[{"message":"Decimal scale '2' is out of valid range.","code":400}]}
    }

    internal class ErrorDescriptorList
    {
        public List<ErrorDescriptor> error { get; set; }
    }
}
