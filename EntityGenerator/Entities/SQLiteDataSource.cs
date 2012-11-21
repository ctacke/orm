using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Data.SqlServerCe;
using EntityGenerator.Dialogs;
using OpenNETCF.ORM;
using System.Data;
using System.Data.SQLite;
using System.Diagnostics;
using System.IO;

namespace EntityGenerator.Entities
{
    internal class SQLiteDataSource : IDataSource
    {
        private const string GetTablesSQL = "SELECT name FROM sqlite_master WHERE type = 'table'";

        private string m_storePath;
        private bool m_firstConnection = true;
        private string m_connectionString;

        public string SourceName
        {
            get { return "SQLite"; }
        }

        public object BrowseForSource(BuildOptions options)
        {
            // TODO: load last browse folder
            var ofd = new OpenFileDialog();
            ofd.Title = "Select Source Database";
            ofd.Filter = "Database Files (*.db)|*.db";
            ofd.CheckFileExists = true;
            if (string.IsNullOrEmpty(options.SourceFolder))
            {
                ofd.InitialDirectory = Application.StartupPath;
            }
            else
            {
                ofd.InitialDirectory = options.SourceFolder;
            }

            if (ofd.ShowDialog() == DialogResult.OK)
            {
                // TODO: save browse folder
                // TODO: save as a "previous source"

                m_storePath = ofd.FileName;
                options.SourceFolder = Path.GetDirectoryName(m_storePath);
                m_firstConnection = true;
                return ofd.FileName;
            }

            return null;
        }

        public object[] GetPreviousSources(IDataSource sourceType)
        {
            // TODO:
            return null;
        }

        public void ClearPreviousSources()
        {
            // TODO:
        }

        public override string ToString()
        {
            return SourceName;
        }

        public EntityInfo[] GetEntityDefinitions()
        {
            ValidateConnection();

            var entities = new List<EntityInfo>();

            // PRAGMA table_info(Clienti)
            // PRAGMA index_list(Clienti)

            using (var connection = new SQLiteConnection(m_connectionString))
            using (var cmd = new SQLiteCommand(GetTablesSQL, connection))
            {
                connection.Open();
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        var info = new EntityInfo();

                        info.Entity = new EntityAttribute();
                        info.Entity.NameInStore = reader.GetString(0);

                        if (string.Compare(info.Entity.NameInStore, "sqlite_sequence", true) == 0)
                        {
                            // this is the auto-increment meta table, skip it
                            continue;
                        }

                        using (var ticmd = new SQLiteCommand(string.Format("PRAGMA table_info({0})", info.Entity.NameInStore), connection))
                        using (var tireader = ticmd.ExecuteReader())
                        {
                            while (tireader.Read())
                            {
                                var cid = tireader["cid"];
                                var name = tireader["name"];
                                var type = tireader["type"];
                                var notnull = tireader["notnull"];
                                var dflt_value = tireader["dflt_value"];
                                var pk = tireader["pk"];

                                var field = new FieldAttribute();
                                field.FieldName = (string)name;
                                field.AllowsNulls = !Convert.ToBoolean(notnull);
                                field.IsPrimaryKey = Convert.ToBoolean(pk);

                                field.DataType = ((string)type).ParseToDbType(true);


                                // TODO: handle default values
                                // TODO: determine if we have auto-increment

                                info.Fields.Add(field);

                            }

                        }

                        // check for indexes (for sort order)
                        using (var idxcmd = new SQLiteCommand(string.Format("SELECT sql FROM sqlite_master WHERE type = 'index' AND tbl_name = '{0}'", info.Entity.NameInStore), connection))
                        using (var idxreader = idxcmd.ExecuteReader())
                        {
                            while (idxreader.Read())
                            {
                                if (idxreader[0] == DBNull.Value)
                                {
                                    // PK or UNIQUE index
                                    continue;
                                }
                                var sql = idxreader.GetString(0);
                                var indexInfo = sql.ParseToIndexInfo();

                                if (indexInfo.IsComposite)
                                {
                                    Debug.WriteLine("Composite indexes not currently supported!");
                                    continue;
                                }

                                var indexedField = (from f in info.Fields
                                                    where string.Compare(f.FieldName, indexInfo.Fields[0], true) == 0
                                                    select f).FirstOrDefault();

                                if (indexedField != null)
                                {
                                    indexedField.SearchOrder = indexInfo.SearchOrder;
                                }
                            }
                        }


                        // check for references
                        using (var fkcmd = new SQLiteCommand(String.Format("PRAGMA foreign_key_list({0})", info.Entity.NameInStore), connection))
                        using (var fkreader = fkcmd.ExecuteReader())
                        {
                            while (fkreader.Read())
                            {
                                var reference = new ReferenceInfo();
                                reference.ReferenceTable = (string)fkreader["table"];
                                reference.LocalFieldName = (string)fkreader["from"];
                                reference.RemoteFieldName = (string)fkreader["to"];
                                info.References.Add(reference);
                            }
                        }

                        entities.Add(info);
                    }
                }
            }

            return entities.ToArray();
        }

        private void ValidateConnection()
        {
            var connectionString = string.Format("Data Source={0}", m_storePath);

            if (m_firstConnection)
            {
                var connection = new SQLiteConnection(connectionString);

                try
                {
                    connection.Open();
                }
                finally
                {
                    if (connection != null)
                    {
                        connection.Dispose();
                    }
                }

                m_connectionString = connectionString;
                m_firstConnection = false;
            }
        }
    }
}
