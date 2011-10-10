using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Data.SqlServerCe;
using EntityGenerator.Dialogs;
using OpenNETCF.ORM;
using System.Xml.Linq;
using System.Reflection;
using System.IO;

namespace EntityGenerator.Entities
{
    public enum OutputLanguage
    {
        CSharp
    }

    internal class BuildOptions
    {
        private static string ElementName = "BuildOptions";
        private static string LanguageAttribute = "language";
        private static string OutputFolderAttribute = "outputFolder";
        private static string NamespaceAttribute = "namespace";
        private static string EntityModifierAttribute = "entityModifier";

        public BuildOptions()
        {
            // set defaults
            Language = OutputLanguage.CSharp;
            OutputFolder = "C:\\ORM";
            EntityNamespace = "OpenNETCF.ORM";
            EntityModifier = System.Reflection.TypeAttributes.Public;
        }

        public OutputLanguage Language { get; set; }
        public string OutputFolder { get; set; }
        public string EntityNamespace { get; set; }
        public TypeAttributes EntityModifier { get; set; }

        public static BuildOptions Load(string path)
        {
            var options = new BuildOptions();

            if (!File.Exists(path))
            {
                return options;
            }

            var doc = XDocument.Load(path);

            var element = doc.Element(ElementName);

            if (element != null)
            {
                var atttrib = element.Attribute(LanguageAttribute);
                if (atttrib != null)
                {
                    options.Language = (OutputLanguage)Enum.Parse(typeof(OutputLanguage), (string)atttrib);
                }

                atttrib = element.Attribute(OutputFolderAttribute);
                if (atttrib != null)
                {
                    options.OutputFolder = (string)atttrib;
                }

                atttrib = element.Attribute(NamespaceAttribute);
                if (atttrib != null)
                {
                    options.EntityNamespace = (string)atttrib;
                }

                atttrib = element.Attribute(EntityModifierAttribute);
                if (atttrib != null)
                {
                    options.EntityModifier = (TypeAttributes)Enum.Parse(typeof(TypeAttributes), (string)atttrib);
                }
            }

            return options;
        }

        public void Save(string path)
        {
            var doc = new XDocument(new XDeclaration("1.0", "utf-8", "yes"));
            doc.Add(
                new XElement(ElementName,
                    new XAttribute(LanguageAttribute, this.Language.ToString()),
                    new XAttribute(OutputFolderAttribute, this.OutputFolder),
                    new XAttribute(NamespaceAttribute, this.EntityNamespace),
                    new XAttribute(EntityModifierAttribute, this.EntityModifier.ToString())
                    ));

            doc.Save(path);
        }
    }
}
