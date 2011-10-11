using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Data.SqlServerCe;
using EntityGenerator.Dialogs;
using OpenNETCF.ORM;
using System.Data;

namespace EntityGenerator.Entities
{
    public class InvalidPasswordException : Exception
    {
    }

    internal class SqlCeDataSource : IDataSource
    {
        private const string GetTablesSQL = "SELECT TABLE_NAME FROM INFORMATION_SCHEMA.TABLES";

        private string m_storePath;
        private bool m_firstConnection = true;
        private string m_connectionString;

        public string SourceName
        {
            get { return "SQL Server Compact"; }
        }

        public object BrowseForSource()
        {
            // TODO: load last browse folder
            var ofd = new OpenFileDialog();
            ofd.Title = "Select Source Database";
            ofd.Filter = "Database Files (*.sdf)|*.sdf";
            ofd.CheckFileExists = true;
            ofd.InitialDirectory = Application.StartupPath;

            if (ofd.ShowDialog() == DialogResult.OK)
            {
                // TODO: save browse folder
                // TODO: save as a "previous source"

                m_storePath = ofd.FileName;
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

        private class IndexInfo
        {
            public string IndexName { get; set; }
            public bool PrimaryKey { get; set; }
            public string ColumnName { get; set; }
            public FieldSearchOrder SearchOrder { get; set; }
        }

        public EntityInfo[] GetEntityDefinitions()
        {
            ValidateConnection();

            var entities = new List<EntityInfo>();

            using (var connection = new SqlCeConnection(m_connectionString))
            using (var cmd = new SqlCeCommand(GetTablesSQL, connection))
            {
                connection.Open();
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        var info = new EntityInfo();
                        var indexInfo = new Dictionary<string, IndexInfo>();

                        info.Entity = new EntityAttribute();
                        info.Entity.NameInStore = reader.GetString(0);

                        using(var indexCommand = new SqlCeCommand(
                            string.Format("SELECT INDEX_NAME, PRIMARY_KEY, COLUMN_NAME, COLLATION FROM INFORMATION_SCHEMA.INDEXES WHERE TABLE_NAME = '{0}'", info.Entity.NameInStore),
                            connection))
                        using (var indexReader = indexCommand.ExecuteReader())
                        {
                            while(indexReader.Read())
                            {
                                var indexName = indexReader.GetString(0);
                                var primaryKey = indexReader.GetBoolean(1);
                                var columnName = indexReader.GetString(2);
                                var sortOrder = indexReader.GetInt16(3) == 1 ? FieldSearchOrder.Ascending : FieldSearchOrder.Descending;
                                // collation of 1 == ascending, 2 == descending (based on a quick test, this might be incorrect)

                                // TODO: handle cases where a column is in multiple indexes (ORM doesn't support that scenario for now)
                                if (!indexInfo.ContainsKey(columnName))
                                {
                                    indexInfo.Add(columnName, new IndexInfo()
                                    {
                                        ColumnName = columnName,
                                        IndexName = indexName,
                                        PrimaryKey = primaryKey,
                                        SearchOrder = sortOrder
                                    });
                                }
                            }
                        }

                        // TODO: look for primary key to set key scheme
                        info.Entity.KeyScheme = KeyScheme.None;

                        using (var fieldCommand = new SqlCeCommand(
                            string.Format("SELECT COLUMN_NAME, COLUMN_HASDEFAULT, IS_NULLABLE, DATA_TYPE, CHARACTER_MAXIMUM_LENGTH, NUMERIC_PRECISION, NUMERIC_SCALE, AUTOINC_SEED FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = '{0}'", info.Entity.NameInStore),
                            connection))
                        {
                            using (var fieldReader = fieldCommand.ExecuteReader())
                            {
                                while (fieldReader.Read())
                                {
                                    var field = new FieldAttribute();
                                    field.FieldName = fieldReader.GetString(0);
                                    field.AllowsNulls = string.Compare(fieldReader.GetString(2), "YES", true) == 0;
                                    field.DataType = fieldReader.GetString(3).ParseToDbType();
                                    object val = fieldReader[4];
                                    if(!val.Equals(DBNull.Value))
                                    {
                                        field.Length = Convert.ToInt32(val);
                                    }
                                    val = fieldReader[5];
                                    if (!val.Equals(DBNull.Value))
                                    {
                                        field.Precision = Convert.ToInt32(val);
                                    }
                                    val = fieldReader[6];
                                    if (!val.Equals(DBNull.Value))
                                    {
                                        field.Scale = Convert.ToInt32(val);
                                    }
                                    val = fieldReader[7];
                                    if (!val.Equals(DBNull.Value))
                                    {
                                        // identity field, so it must be the PK (or part of it)
                                        info.Entity.KeyScheme = KeyScheme.Identity;
                                    }

                                    // check for indexes
                                    if (indexInfo.ContainsKey(field.FieldName))
                                    {
                                        var idx = indexInfo[field.FieldName];

                                        if (idx.PrimaryKey)
                                        {
                                            field.IsPrimaryKey = true;

                                            if (field.DataType == DbType.Guid)
                                            {
                                                info.Entity.KeyScheme = KeyScheme.GUID;
                                            }
                                        }
                                        field.SearchOrder = idx.SearchOrder;
                                    }

                                    // TODO: populate the remainder of the field info
                                    info.Fields.Add(field);
                                }
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
                var connection = new SqlCeConnection(connectionString);

                // see if we need a password
                try
                {
                    connection.Open();
                }
                catch (SqlCeException ex)
                {
                    if (ex.NativeError == 25028)
                    {
                        // a password is required.
                        var dialog = new GetPasswordDialog();
                        if (dialog.ShowDialog() == DialogResult.OK)
                        {
                            connectionString += (";password=" + dialog.Password);
                            connection.ConnectionString = connectionString;
                            try
                            {
                                connection.Open();
                            }
                            catch (SqlCeException exi)
                            {
                                if (exi.NativeError == 25028)
                                {
                                    throw new InvalidPasswordException();
                                }

                                throw;
                            }
                        }
                        else
                        {
                            throw new InvalidPasswordException();
                        }
                    }
                    else
                    {
                        throw;
                    }
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
