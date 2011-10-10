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
    public class EntityInfo
    {
        public EntityInfo()
        {
            Fields = new List<FieldAttribute>();
        }
        
        public EntityAttribute Entity { get; set; }
        public List<FieldAttribute> Fields { get; set; }
    }
}
