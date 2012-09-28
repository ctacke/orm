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
using System.Data;

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

                    entityClass.Members.Add(GenerateEntityCreationProxy(entity));

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

        private CodeMemberMethod GenerateEntityCreationProxy(Entities.EntityInfo entity)
        {
            var proxy = new CodeMemberMethod();
            proxy.Name = "ORM_CreateProxy";
            proxy.Attributes = MemberAttributes.Private | MemberAttributes.Static;
            proxy.ReturnType = new CodeTypeReference(entity.Entity.NameInStore);
            var fieldsParameter = new CodeParameterDeclarationExpression(typeof(FieldAttributeCollection), "fields"); 
            proxy.Parameters.Add(fieldsParameter);
            var resultsParameter = new CodeParameterDeclarationExpression(typeof(IDataReader), "results");
            proxy.Parameters.Add(resultsParameter);

            CodeStatement statement = new CodeExpressionStatement(new CodeSnippetExpression(string.Format("var item = new {0}()", entity.Entity.NameInStore)));
            proxy.Statements.Add(statement);

            statement = new CodeSnippetStatement("foreach(var field in fields){");
            proxy.Statements.Add(statement);

            statement = new CodeExpressionStatement(new CodeSnippetExpression("var value = results[field.Ordinal]"));
            proxy.Statements.Add(statement);

            statement = new CodeSnippetStatement("switch(field.FieldName){");
            proxy.Statements.Add(statement);

            foreach (var field in entity.Fields)
            {
                statement = new CodeSnippetStatement(string.Format("case \"{0}\":", field.FieldName));
                proxy.Statements.Add(statement);

                switch (field.DataType)
                {
                    case DbType.Byte:
                        statement = new CodeExpressionStatement(new CodeSnippetExpression(
                            string.Format("item.{0} = (value == DBNull.Value) ? 0 : (byte{1})value",
                            field.FieldName,
                            field.AllowsNulls ? "?" : string.Empty)));
                        break;
                    case DbType.Int16:
                        statement = new CodeExpressionStatement(new CodeSnippetExpression(
                            string.Format("item.{0} = (value == DBNull.Value) ? 0 : (short{1})value",
                            field.FieldName,
                            field.AllowsNulls ? "?" : string.Empty)));
                        break;
                    case DbType.Int32:
                        statement = new CodeExpressionStatement(new CodeSnippetExpression(
                            string.Format("item.{0} = (value == DBNull.Value) ? 0 : (int{1})value", 
                            field.FieldName,
                            field.AllowsNulls ? "?" : string.Empty)));
                        break;
                    case DbType.Int64:
                        statement = new CodeExpressionStatement(new CodeSnippetExpression(
                            string.Format("item.{0} = (value == DBNull.Value) ? 0 : (long{1})value",
                            field.FieldName,
                            field.AllowsNulls ? "?" : string.Empty)));
                        proxy.Statements.Add(new CodeCommentStatement("If this is a TimeSpan, use the commented line below"));
                        proxy.Statements.Add(new CodeCommentStatement(string.Format("item.{0} = (value == DBNull.Value) ? {1} : new TimeSpan((long)value);",
                            field.FieldName,
                            field.AllowsNulls ? "null" : "TimeSpan.MinValue;")));
                        break;
                    case DbType.Single:
                        statement = new CodeExpressionStatement(new CodeSnippetExpression(
                            string.Format("item.{0} = (value == DBNull.Value) ? 0 : (float{1})value",
                            field.FieldName,
                            field.AllowsNulls ? "?" : string.Empty)));
                        break;
                    case DbType.Double:
                        statement = new CodeExpressionStatement(new CodeSnippetExpression(
                            string.Format("item.{0} = (value == DBNull.Value) ? 0 : (double{1})value",
                            field.FieldName,
                            field.AllowsNulls ? "?" : string.Empty)));
                        break;
                    case DbType.Decimal:
                        statement = new CodeExpressionStatement(new CodeSnippetExpression(
                            string.Format("item.{0} = (value == DBNull.Value) ? 0 : (decimal{1})value",
                            field.FieldName,
                            field.AllowsNulls ? "?" : string.Empty)));
                        break;
                    case DbType.String:
                        statement = new CodeExpressionStatement(new CodeSnippetExpression(
                            string.Format("item.{0} = (value == DBNull.Value) ? null : (string)value",
                            field.FieldName)));
                        break;
                    case DbType.Guid:
                        statement = new CodeExpressionStatement(new CodeSnippetExpression(
                            string.Format("item.{0} = (value == DBNull.Value) ? null : (Guid{1})value",
                            field.FieldName,
                            field.AllowsNulls ? "?" : string.Empty)));
                        break;
                    default:
                        statement = new CodeCommentStatement(string.Format("Field '{0}' not generated: Unsupported type '{1}'", field.FieldName, field.DataType));
                        break;
                }

                proxy.Statements.Add(statement);

                statement = new CodeExpressionStatement(new CodeSnippetExpression("break"));
                proxy.Statements.Add(statement);
            }

            statement = new CodeSnippetStatement("}"); // end switch
            proxy.Statements.Add(statement);

            statement = new CodeSnippetStatement("}"); // end foreach
            proxy.Statements.Add(statement);

            var ret = new CodeExpressionStatement(new CodeSnippetExpression("return item"));
            proxy.Statements.Add(ret);

            return proxy;
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
