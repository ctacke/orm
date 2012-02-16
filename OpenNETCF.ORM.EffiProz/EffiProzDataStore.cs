using System;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using System.Data.EffiProz;
using System.Data.Common;
using System.Data;

namespace OpenNETCF.ORM.EffiProz
{
    public class EffiProzDataStore : SQLStoreBase<SqlEntityInfo>, IDisposable
    {
        private string m_connectionString;
        private const string DefaultUserName = "sa";

        private string Password { get; set; }
        public string FileName { get; private set; }
        public string UserName { get; private set; }

        public EffiProzDataStore(string fileName)
            : this(fileName, null, null)
        {
        }

        public EffiProzDataStore(string fileName, string userName)
            : this(fileName, userName, null)
        {
        }

        public EffiProzDataStore(string fileName, string userName, string password)
        {
            FileName = fileName;
            UserName = userName ?? DefaultUserName;
            Password = password ?? string.Empty;
        }


        protected override IDbCommand GetNewCommandObject()
        {
#if WINDOWS_PHONE
            return new EfzCommandWrapper();
#else
            return new EfzCommand();
#endif
        }

        protected override IDbConnection GetNewConnectionObject()
        {
#if WINDOWS_PHONE
            return new EfzConnectionWrapper(ConnectionString);
#else
            return new EfzConnection(ConnectionString);
#endif
        }

        private string ConnectionString
        {
            get
            {
                if (m_connectionString == null)
                {
                    m_connectionString = string.Format(
                        "Connection Type=file;Initial Catalog={0};User={1};Password={2};",
                        FileName,
                        UserName,
                        Password);
                }
                return m_connectionString;
            }
        }

        protected override string AutoIncrementFieldIdentifier
        {
            get { throw new NotImplementedException(); }
        }

        public override void CreateStore()
        {
            throw new NotImplementedException();
        }

        public override void DeleteStore()
        {
            throw new NotImplementedException();
        }

        public override void EnsureCompatibility()
        {
            throw new NotImplementedException();
        }

        public override bool StoreExists
        {
            get { throw new NotImplementedException(); }
        }

        protected override string GetPrimaryKeyIndexName(string entityName)
        {
            throw new NotImplementedException();
        }

        public override void Insert(object item, bool insertReferences)
        {
            throw new NotImplementedException();
        }

        protected override object[] Select(Type objectType, IEnumerable<FilterCondition> filters, int fetchCount, int firstRowOffset, bool fillReferences)
        {
            throw new NotImplementedException();
        }

        public override void Update(object item, bool cascadeUpdates, string fieldName)
        {
            throw new NotImplementedException();
        }

        public override T[] Fetch<T>(int fetchCount, int firstRowOffset, string sortField, FieldSearchOrder sortOrder, FilterCondition filter, bool fillReferences)
        {
            throw new NotImplementedException();
        }

        public override int Count<T>(IEnumerable<FilterCondition> filters)
        {
            throw new NotImplementedException();
        }

        protected override IDataParameter CreateParameterObject(string parameterName, object parameterValue)
        {
            throw new NotImplementedException();
        }
    }
}
