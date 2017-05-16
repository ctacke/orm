using System.Collections.Generic;

namespace OpenNETCF.DreamFactory
{
    internal class ContainerDescriptorList
    {
        public List<ContainerDescriptor> container { get; set; }
    }

    internal class ContainerDescriptor
    {
        public string name { get; set; }
        public string path { get; set; }
        public string last_modified { get; set; }

        public List<ContainerDescriptor> folder { get; set; }
    }

    internal class FolderDescriptor
    {
        public string name { get; set; }
        public string path { get; set; }
        public string last_modified { get; set; }
    }
}
