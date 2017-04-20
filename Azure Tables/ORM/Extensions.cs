using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Security.Cryptography;
using System.Globalization;
using System.Net;
using System.IO;
using System.Xml.Linq;
using OpenNETCF.Azure;

namespace OpenNETCF.ORM
{
    internal static class Extensions
    {
        public static DynamicEntity AsDynamicEntity(this AzureEntity entity, DynamicEntityDefinition definition)
        {
            var de = new DynamicEntity(definition.EntityName);
            de.Fields.Add("PartitionKey", entity.PartitionKey);
            de.Fields.Add("RowKey", entity.RowKey);

            foreach (var f in entity.Fields)
            {
                if (!definition.Fields.ContainsField(f.Name))
                {
                    continue;
                }

                if ((f.Value.ToString() == string.Empty) && (definition.Fields[f.Name].DataType != System.Data.DbType.String))
                {
                    // we store an empty string for non-string 'null' values in the Table Service
                    de.Fields.Add(f.Name, null);
                }
                else
                {
                    de.Fields.Add(f.Name, f.Value);
                }
            }

            return de;
        }
    }
}
