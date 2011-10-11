using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using EntityGenerator.Entities;
using OpenNETCF.ORM;
using OpenNETCF;
using System.CodeDom;
using System.CodeDom.Compiler;
using System.Reflection;
using System.IO;

namespace EntityGenerator.Services
{
    public class GeneratorService
    {
        private IDataSource[] m_sources;

        public IDataSource CurrentSourceType { get; private set; }
        private IEnumerable<EntityGenerator.Entities.EntityInfo> Structure { get; set; }

        public IDataSource[] GetAvailableSources()
        {
            if (m_sources == null)
            {
                m_sources = new IDataSource[]
                {
                    new SqlCeDataSource(),
                };
            }
            return m_sources;
        }

        public void SetCurrentSourceType(IDataSource sourceType)
        {
            Validate
                .Begin()
                .IsNotNull(sourceType)
                .Check();

            if (CurrentSourceType != sourceType)
            {
                CurrentSourceType = sourceType;
            }
        }

        public object[] GetPreviousSources()
        {
            if (CurrentSourceType == null) return null;

            // TODO: get list based on current source
            return null;
        }

        public object BrowseForSource()
        {
            if (CurrentSourceType == null) return null;

            return CurrentSourceType.BrowseForSource();
        }

        public void SetSelectedStructure(IEnumerable<EntityGenerator.Entities.EntityInfo> structure)
        {
            Structure = structure;
        }

        internal void GenerateCode(BuildOptions options)
        {
            using (var csProvider = CodeDomProvider.CreateProvider("C#"))  // TODO: add VB support
            {

                foreach (var entity in Structure)
                {
                    var ccu = new CodeCompileUnit();
                    var root = new CodeNamespace(options.EntityNamespace);

                    ccu.Namespaces.Add(root);

                    AddImports(root);

                    var entityClass = new CodeTypeDeclaration(entity.Entity.NameInStore);
                    entityClass.TypeAttributes = options.EntityModifier;

                    // TODO: determine KeyScheme
                    // TODO: set NameInStore if different than class name (user overridden)
                    var entityAttributeDeclaration = new CodeAttributeDeclaration("Entity",
                        new CodeAttributeArgument(new CodeSnippetExpression("KeyScheme." + entity.Entity.KeyScheme.ToString()))
                        // new CodeAttributeArgument(new CodeSnippetExpression("NameInStore=\"" + entity.Entity.NameInStore + "\""))
                        );

                    //new CodeAttributeArgument("KeyScheme", new CodePrimitiveExpression(KeyScheme.None))
                    entityClass.CustomAttributes.Add(entityAttributeDeclaration);

                    var fieldList = new List<CodeMemberField>();
                    var propList = new List<CodeMemberProperty>();

                    // TODO: timespans
                    // TODO: enums
                    // TODO: objects
                    foreach (var field in entity.Fields)
                    {
                        // TODO: optionally make it bindable with RaisePropertyChanged
                        var type = new CodeTypeReference(field.DataType.ToManagedType(field.AllowsNulls));
                        var backingFieldName = "m_" + field.FieldName.ToLower(); // TODO: let the use specify format
                        var backingField = new CodeMemberField(type, backingFieldName);
                        backingField.Attributes = MemberAttributes.Private;
                        fieldList.Add(backingField);

                        var prop = new CodeMemberProperty();
                        prop.Name = field.FieldName;
                        prop.Attributes = MemberAttributes.Public | MemberAttributes.Final;  // TODO: get from UI
                        prop.CustomAttributes.Add(new CodeAttributeDeclaration("Field", GenerateFieldArguments(field)));                            
                        prop.Type = type;
                        prop.GetStatements.Add(
                            new CodeMethodReturnStatement(
                                new CodeFieldReferenceExpression(
                                    new CodeThisReferenceExpression(), backingFieldName)));
                        prop.SetStatements.Add(
                            new CodeAssignStatement(
                                new CodeFieldReferenceExpression(
                                    new CodeThisReferenceExpression(), backingFieldName),
                                    new CodePropertySetValueReferenceExpression()));

                        propList.Add(prop);
                    }
                    
                    // this is outside the loop above to give some semblance of structure to the code
                    foreach (var backingField in fieldList)
                    {
                        entityClass.Members.Add(backingField);
                    }
                    foreach (var prop in propList)
                    {
                        entityClass.Members.Add(prop);
                    }

                    root.Types.Add(entityClass);

                    // Generate the C# source code for this class
                    GenerateClassFile(options, csProvider, ccu, entity.Entity.NameInStore);
                }

            }
        }

        private CodeAttributeArgument[] GenerateFieldArguments(FieldAttribute field)
        {
            var attrList = new List<CodeAttributeArgument>();

            // TODO add precision, etc.

            if (field.IsPrimaryKey)
            {
                attrList.Add(new CodeAttributeArgument(new CodeSnippetExpression("IsPrimaryKey=true")));
            }

            if (field.SearchOrder != FieldSearchOrder.NotSearchable)
            {
                attrList.Add(new CodeAttributeArgument(new CodeSnippetExpression("SearchOrder=FieldSearchOrder." + field.SearchOrder.ToString())));
            }

            return attrList.ToArray();
        }

        private void GenerateClassFile(BuildOptions buildOptions, CodeDomProvider provider, CodeCompileUnit ccu, string entityname)
        {
            var sb = new StringBuilder();
            var sw = new StringWriter(sb);

            // todo: make these settable
            var options = new CodeGeneratorOptions();
            options.BlankLinesBetweenMembers = true;
            options.BracingStyle = "C";
            options.ElseOnClosing = false;
            options.IndentString = "    ";
            options.VerbatimOrder = true;

            provider.GenerateCodeFromCompileUnit(ccu, sw, options);
            var fileName = entityname + ".cs";  // TODO: add VB support
            var path = buildOptions.OutputFolder;
            if (!Directory.Exists(path)) Directory.CreateDirectory(path);
            path = Path.Combine(path, fileName);

            // TODO: make auto-delete optional or give a warning
            if (File.Exists(path)) 
            {
                File.Delete(path);
            }

            try
            {
                using (var writer = File.CreateText(path))
                {
                    writer.Write(sb.ToString());
                    writer.Close();
                }
            }
            catch (IOException ex)
            {
                // TODO: handle this
                throw;
            }

        }

        private void AddImports(CodeNamespace root)
        {
            root.Imports.Add(new CodeNamespaceImport("System"));
            root.Imports.Add(new CodeNamespaceImport("OpenNETCF.ORM"));
        }

        private void AddReferencedAssemblies(CompilerParameters cp)
        {
            //string pathToWCFAssemblies = System.Runtime.InteropServices.RuntimeEnvironment.GetRuntimeDirectory();
            //if (pathToWCFAssemblies.EndsWith("\\")) pathToWCFAssemblies = pathToWCFAssemblies.Substring(0, pathToWCFAssemblies.Length - 1);
            //pathToWCFAssemblies = System.IO.Path.GetDirectoryName(pathToWCFAssemblies);
            //pathToWCFAssemblies = System.IO.Path.Combine(pathToWCFAssemblies, "v3.0\\Windows Communication Foundation");

            //cp.ReferencedAssemblies.Add(System.IO.Path.GetFileName(typeof(System.Uri).Assembly.CodeBase));
            //cp.ReferencedAssemblies.Add(System.IO.Path.GetFileName(typeof(System.Xml.XmlNode).Assembly.CodeBase));
            //cp.ReferencedAssemblies.Add(System.IO.Path.Combine(pathToWCFAssemblies, "System.ServiceModel.dll"));
            //cp.ReferencedAssemblies.Add("ICM.Infrastructure.Module.dll");
            //cp.ReferencedAssemblies.Add("ICM.Infrastructure.Interface.dll");
        }
    }
}
