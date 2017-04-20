using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.IO;
using System.Xml.Linq;
using System.Globalization;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using System.Collections.Specialized;

namespace OpenNETCF.Azure
{
    public class AzureField
    {
        public string Name { get; private set; }
        public object Value { get; set; }

        private AzureField()
        {
        }

        public AzureField(string name)
            : this(name, null)
        {
        }

        public AzureField(string name, object value)
        {
            Name = name;
            Value = value;
        }

        internal XElement AsATOMProperty()
        {
            var typeName = string.Empty;
            var valueString = Value.ToString();

            Switch.On<Type>(Value.GetType())
                .Case(typeof(string), () =>  { typeName = "Edm.String"; })
                .Case(typeof(int), () => { typeName = "Edm.Int32"; })
                .Case(typeof(uint), () => { typeName = "Edm.Int32"; })
                .Case(typeof(short), () => { typeName = "Edm.Int32"; })
                .Case(typeof(ushort), () => { typeName = "Edm.Int32"; })
                .Case(typeof(byte), () => { typeName = "Edm.Int32"; })
                .Case(typeof(long), () => { typeName = "Edm.Int64"; })
                .Case(typeof(ulong), () => { typeName = "Edm.Int64"; })
                .Case(typeof(double), () => { typeName = "Edm.Double"; })
                .Case(typeof(float), () => { typeName = "Edm.Double"; })
                .Case(typeof(bool), () => 
                { 
                    typeName = "Edm.Boolean";
                    valueString = Value.ToString().ToLower();
                })
                .Case(typeof(DateTime), () => 
                { 
                    typeName = "Edm.DateTime";
                    valueString = ((DateTime)Value).ToUniversalTime().ToString("o");
                })
                .Case(typeof(byte[]), () =>
                {
                    typeName = "Edm.Binary";
                    valueString = Convert.ToBase64String((byte[])Value);
                })
                .Case(typeof(DBNull), () =>
                {
                    typeName = "Edm.String";
                    valueString = string.Empty;
                })
                .Default(() =>
                {
                    throw new NotSupportedException(string.Format("Fields of type '{0}' are not supported", Value.GetType().Name));
                });

            return new XElement(Namespaces.DataServices + Name,
                new XAttribute(Namespaces.DataServicesMeta + "type", typeName),
                valueString);
//                System.Security.SecurityElement.Escape(valueString));
        }

        internal static AzureField FromATOMFeed(XElement propertyElement)
        {
            var field = new AzureField();
            field.Name = propertyElement.Name.LocalName;

            string edmType = string.Empty;

            var typeAttribute = propertyElement.Attribute(Namespaces.DataServicesMeta + "type");
            if (typeAttribute == null)
            {
                edmType = "Edm.String";
            }
            else
            {
                edmType = typeAttribute.Value;
            }

            switch (edmType)
            {
                case "Edm.Int32":
                    field.Value = Convert.ToInt32(propertyElement.Value);
                    break;
                case "Edm.Int64":
                    field.Value = Convert.ToInt64(propertyElement.Value);
                    break;
                case "Edm.DateTime":
                    field.Value = Convert.ToDateTime(propertyElement.Value);
                    break;
                case "Edm.Double":
                    field.Value = Convert.ToDouble(propertyElement.Value);
                    break;
                case "Edm.String":
                    field.Value = propertyElement.Value;
                    break;
                case "Edm.Boolean":
                    field.Value = Convert.ToBoolean(propertyElement.Value);
                    break;
                case "Edm.Binary":
                    field.Value = Convert.FromBase64String(propertyElement.Value);
                    break;
                default:
                    throw new NotSupportedException();
            }

            return field;
        }

        public override string ToString()
        {
            return string.Format("{0}={1}", this.Name, this.Value);
        }
    }
}
