using System;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using Community.CsharpSqlite;
using System.IO;
using System.Diagnostics;
using System.Text;
using System.Linq;
using System.Data;

namespace OpenNETCF.ORM.SQLite
{
    public class SQLiteDataStore : DataStore<SQLiteEntityInfo>, IDisposable
    {
        private Sqlite3.sqlite3 m_store;
        private string m_storeName;

        public int DefaultStringFieldSize { get; set; }
        public int DefaultNumericFieldPrecision { get; set; }

        public SQLiteDataStore(string databaseName)
        {
            if (string.IsNullOrEmpty(databaseName))
            {
                throw new ArgumentException();
            }

            m_storeName = databaseName;
            DefaultStringFieldSize = 200;
            DefaultNumericFieldPrecision = 16;

            OpenStore();
        }
         
        public override void CreateStore()
        {
            foreach (var entity in this.Entities)
            {
                CreateTable(entity);
            }
        }

        public override void DeleteStore()
        {
            if (m_store != null)
            {
                CloseStore();
            }

            if (StoreExists)
            {
                File.Delete(m_storeName);
            }
        }

        public override bool StoreExists
        {
            get { return File.Exists(m_storeName); }
        }

        public override void Insert(object item, bool insertReferences)
        {
            throw new NotImplementedException();
        }

        public override T[] Select<T>()
        {
            throw new NotImplementedException();
        }

        public override T Select<T>(object primaryKey)
        {
            throw new NotImplementedException();
        }

        public override T[] Select<T>(string searchFieldName, object matchValue)
        {
            throw new NotImplementedException();
        }

        public override void Update(object item)
        {
            throw new NotImplementedException();
        }

        public override void Delete(object item)
        {
            throw new NotImplementedException();
        }

        public override void Delete<T>(object primaryKey)
        {
            throw new NotImplementedException();
        }

        public override void FillReferences(object instance)
        {
            throw new NotImplementedException();
        }

        public override T[] Fetch<T>(int fetchCount)
        {
            throw new NotImplementedException();
        }

        public override T[] Fetch<T>(int fetchCount, int firstRowOffset)
        {
            throw new NotImplementedException();
        }

        public override T[] Fetch<T>(string searchFieldName, int fetchCount, int firstRowOffset)
        {
            throw new NotImplementedException();
        }

        public override int Count<T>()
        {
            throw new NotImplementedException();
        }

        public override void Delete<T>()
        {
            throw new NotImplementedException();
        }

        public override void Delete<T>(string fieldName, object matchValue)
        {
            throw new NotImplementedException();
        }

        public override void Update(object item, bool cascadeUpdates)
        {
            throw new NotImplementedException();
        }

        public override bool Contains(object item)
        {
            throw new NotImplementedException();
        }

        public void Dispose()
        {
            if (m_store != null)
            {
                CloseStore();
            }
        }

        private void OpenStore()
        {
            m_store = new Sqlite3.sqlite3();

            var result = Sqlite3.sqlite3_open(m_storeName, ref m_store);

            if (result != Sqlite3.SQLITE_OK)
            {
                throw new SQLiteException(Sqlite3.sqlite3_errmsg(m_store));
            }
        }

        private void CloseStore()
        {
            var result = Sqlite3.sqlite3_close(m_store);
            if (result != Sqlite3.SQLITE_OK)
            {
                throw new SQLiteException(Sqlite3.sqlite3_errmsg(m_store));
            }
            m_store = null;
        }

        private void CreateTable(EntityInfo entity)
        {
            StringBuilder sql = new StringBuilder();

            if (ReservedWords.Contains(entity.EntityName, StringComparer.InvariantCultureIgnoreCase))
            {
                throw new ReservedWordException(entity.EntityName);
            }

            sql.AppendFormat("CREATE TABLE {0} (", entity.EntityName);

            int count = entity.Fields.Count;

            foreach (var field in entity.Fields)
            {
                if (ReservedWords.Contains(field.FieldName, StringComparer.InvariantCultureIgnoreCase))
                {
                    throw new ReservedWordException(field.FieldName);
                }

                sql.AppendFormat("[{0}] {1} {2}",
                    field.FieldName,
                    GetFieldDataTypeString(entity.EntityName, field),
                    GetFieldCreationAttributes(entity.EntityAttribute, field));

                if (--count > 0) sql.Append(", ");
            }

            sql.Append(")");

            Debug.WriteLine(sql);
            string error = string.Empty;
            var result = Sqlite3.sqlite3_exec(m_store, sql.ToString(), CommandCallback, null, ref error);
            if (result != Sqlite3.SQLITE_OK)
            {
                throw new SQLiteException(Sqlite3.sqlite3_errmsg(m_store));
            }
        }

        private int CommandCallback(object pArg, long nArg, object azArgs, object azCols)
        {
            return 0;
        }

        private string GetFieldDataTypeString(string entityName, FieldAttribute field)
        {
            // the SQL RowVersion is a special case
            if (field.IsRowVersion)
            {
                switch (field.DataType)
                {
                    case DbType.UInt64:
                    case DbType.Int64:
                        // no error
                        break;
                    default:
                        throw new FieldDefinitionException(entityName, field.FieldName, "rowversion fields must be an 8-byte data type (In64 or UInt64)");
                }

                return "rowversion";
            }

            return field.DataType.ToSqlTypeString();
        }

        private string GetFieldCreationAttributes(EntityAttribute attribute, FieldAttribute field)
        {
            StringBuilder sb = new StringBuilder();

            switch (field.DataType)
            {
                case DbType.String:
                    if (field.Length > 0)
                    {
                        sb.AppendFormat("({0}) ", field.Length);
                    }
                    else
                    {
                        sb.AppendFormat("({0}) ", DefaultStringFieldSize);
                    }
                    break;
                case DbType.Decimal:
                    int p = field.Precision == 0 ? DefaultNumericFieldPrecision : field.Precision;
                    sb.AppendFormat("({0},{1}) ", p, field.Scale);
                    break;
            }

            if (field.IsPrimaryKey)
            {
                sb.Append("PRIMARY KEY ");

                if (attribute.KeyScheme == KeyScheme.Identity)
                {
                    switch (field.DataType)
                    {
                        case DbType.Int32:
                        case DbType.UInt32:
                            sb.Append("IDENTITY ");
                            break;
                        case DbType.Guid:
                            sb.Append("ROWGUIDCOL ");
                            break;
                        default:
                            throw new FieldDefinitionException(attribute.NameInStore, field.FieldName,
                                string.Format("Data Type '{0}' cannot be marked as an Identity field", field.DataType));
                    }
                }
            }

            if (!field.AllowsNulls)
            {
                sb.Append("NOT NULL ");
            }

            if (field.RequireUniqueValue)
            {
                sb.Append("UNIQUE ");
            }

            return sb.ToString();
        }

        static string[] ReservedWords = new string[]
        {
            // TODO: add SQLite reserved words here
        };

        public override void EnsureCompatibility()
        {
            throw new NotImplementedException();
        }

        public override T[] Select<T>(System.Collections.Generic.IEnumerable<FilterCondition> filters)
        {
            throw new NotImplementedException();
        }

        public override object[] Select(Type entityType)
        {
            throw new NotImplementedException();
        }
    }
}
