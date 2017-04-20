using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RestSharp;

namespace OpenNETCF.DreamFactory
{
    public sealed class Application
    {
        public string Name { get; set; }
        public string ID { get; private set; }
        public string URL { get; private set; }
        public string APIName { get; set; }
        public string Description { get; set; }

        internal Application(ApplicationDescriptor descriptor)
        {
            Name = descriptor.name;
            ID = descriptor.id;
            URL = descriptor.url;
            APIName = descriptor.api_name;
            Description = descriptor.description;
        }

        internal ApplicationDescriptor AsApplicationDescriptor()
        {
            var descriptor = new ApplicationDescriptor()
            {
                name = Name,
                id = ID,
                api_name = APIName,
                description = Description
            };

            return descriptor;
        }
    }
}
