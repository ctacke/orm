using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Data.SqlServerCe;
using EntityGenerator.Dialogs;
using OpenNETCF.ORM;

namespace EntityGenerator.Entities
{
    public class ReferenceInfo
    {
        public string ReferenceTable { get; set; }
        public string LocalFieldName { get; set; }
        public string RemoteFieldName { get; set; }
    }

    public class EntityInfo
    {
        public EntityInfo()
        {
            Fields = new List<FieldAttribute>();
            References = new List<ReferenceInfo>();
        }
        
        public EntityAttribute Entity { get; set; }
        public List<FieldAttribute> Fields { get; set; }
        public List<ReferenceInfo> References { get; set; }
    }
}
