using System;

namespace OpenNETCF.DreamFactory
{
    internal sealed class UriFactory
    {
        private Version m_version;

        public string SchemaRoot { get; private set; }

        public UriFactory(Version dspVersion)
        {
            m_version = dspVersion;

            bool legacySchema = false;

            if(m_version.Major <= 1)
            {
                if(m_version.Minor < 7)
                {
                    legacySchema = true;
                }
                else if(m_version.Minor == 7)
                {
                    if(m_version.Build < 6)
                    {
                        legacySchema = true;
                    }
                }
            }

            if (legacySchema)
            {
                SchemaRoot = "/rest/schema";
            }
            else
            {
                SchemaRoot = "/rest/db/_schema";
            }
        }

        public string GetTableSchema(string tableName)
        {
            return string.Format("{0}/{1}", SchemaRoot, tableName);
        }
    }
}
