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
    public interface IDataSource
    {
        string SourceName { get; }
        object BrowseForSource();
        object[] GetPreviousSources(IDataSource sourceType);
        void ClearPreviousSources();
        EntityInfo[] GetEntityDefinitions();
    }
}
