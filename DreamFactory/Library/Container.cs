using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RestSharp;

namespace OpenNETCF.DreamFactory
{
    public sealed class Container
    {
        public string Name { get; private set; }
        public string Path { get; private set; }
        public DateTime? LastModified { get; private set; }
        public Container[] Containers { get; set; }

        internal Container(ContainerDescriptor descriptor)
        {
            this.Name = descriptor.name;
            this.Path = descriptor.path;
            try
            {
                if (!descriptor.last_modified.IsNullOrEmpty())
                {
                    this.LastModified = DateTime.Parse(descriptor.last_modified);
                }
            }
            catch
            {
                // default to null
            }

            if (descriptor.folder != null)
            {
                Containers = new Container[descriptor.folder.Count];
                var i = 0;

                foreach (var c in descriptor.folder)
                {
                    Containers[i] = new Container(c);
                    i++;
                }
            }
            else
            {
                Containers = new Container[0];
            }
        }

        public override string ToString()
        {
            return Name;
        }
    }
}
