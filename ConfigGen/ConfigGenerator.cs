using System;
using System.IO;
using System.Text;
using System.Xml.Linq;
using System.Linq;
using System.CodeDom;
using System.Configuration;
using Microsoft.CSharp;
using System.CodeDom.Compiler;
using System.Collections.Generic;

namespace ConfigGen
{
    /// <summary>
    /// Class that can generate code for a configuration section
    /// </summary>
    public class ConfigGenerator
    {
        #region Fields
        private readonly XDocument _configurationSectionXml;
        private CodeCompileUnit _codeDomeCompileUnit;
        private CodeNamespace _namespace;
        private List<string> _createdTypes;
        #endregion

        #region Properties
        /// <summary>
        /// Additional Namespaces to include 
        /// </summary>
        public ConfigurationNamespaceCollection Namespaces  { get; private set; }

        /// <summary>
        /// Provider to use to generate code. E.g. <see cref="CSharpCodeProvider"/>
        /// </summary>
        public CodeDomProvider Provider { get; set; }
        #endregion

        #region Constructors

        /// <summary>
        /// Initialise the generator
        /// </summary>
        /// <param name="path">Path to an XML file containing an example config</param>
        public ConfigGenerator(Uri path) { 
             init();
            _configurationSectionXml = XDocument.Load(path.OriginalString);
        }
        
        /// <summary>
        /// Initialise the generator
        /// </summary>
        /// <param name="xml">String containing the XML to generate code for</param>
        public ConfigGenerator(string xml)
        {
            init();
            using (var stream = new MemoryStream(Encoding.UTF8.GetBytes(xml)))
            {
                _configurationSectionXml = XDocument.Load(stream);
            }            
        }


        /// <summary>
        /// Initialise default values for generating files
        /// </summary>
        private void init()
        {
            _createdTypes = new List<string>();
            _codeDomeCompileUnit = new CodeCompileUnit();
            _namespace = new CodeNamespace("Config");
            Namespaces = new ConfigurationNamespaceCollection();
            Namespaces.Add("System");
            Namespaces.Add("System.Collections");
            Namespaces.Add("System.Text");
            Namespaces.Add("System.Configuration");
            _codeDomeCompileUnit.Namespaces.Add(_namespace);
            
        }
        #endregion

        /// <summary>
        /// Generate source code for the configuration
        /// </summary>
        /// <param name="destinationPath">
        /// File location to write output to.
        /// Will overwrite files if they already exist
        /// </param>
        public void GenerateConfig(Uri destinationPath)
        {
            GenerateConfig(destinationPath, OutPutType.SingleFile);
        }

        /// <summary>
        /// Generate source code for the configuration
        /// </summary>
        /// <param name="destinationPath">
        /// File location to write output to.
        /// Should be a .cs file location when using <see cref="OutPutType.SingleFile"/> otherwise should be a folder
        /// Will overwrite files if they already exist
        /// </param>
        /// <param name="type">Type of output to generate</param>
        public void GenerateConfig(Uri destinationPath, OutPutType type)
        {
            init();
            _namespace.Imports.AddRange(Namespaces.ToArray());
            var root = _configurationSectionXml.Root;
            CreateSectionElement(root);
            if (Provider == null)
            {
                Provider = new CSharpCodeProvider();
            }

            if (type == OutPutType.FilePerClass)
            {
                WriteMultipleFiles(destinationPath);
            }
            else
            {
                WriteSingleFile(destinationPath);
            }
        }

        /// <summary>
        /// Write output to several files
        /// </summary>
        /// <param name="destinationFolder">Folder to write to</param>
        private void WriteMultipleFiles(Uri destinationFolder)
        {
            if (!Directory.Exists(destinationFolder.OriginalString))
            {
                throw new ArgumentException(string.Format("Directory {0} does not exist", destinationFolder));
            }
            foreach (CodeTypeDeclaration type in _namespace.Types)
            {
                var ns = new CodeNamespace(_namespace.Name);
                ns.Types.Add(type);
                using (var writer = new StreamWriter(string.Format("{0}/{1}.cs", destinationFolder, type.Name), false))
                using (var itw = new IndentedTextWriter(writer))
                {
                    var compileUnit = new CodeCompileUnit();
                    ns.Imports.AddRange(Namespaces.ToArray());
                    compileUnit.Namespaces.Add(ns);
                    Provider.GenerateCodeFromCompileUnit(compileUnit, itw, new CodeGeneratorOptions());
                }
            }
        }

        /// <summary>
        /// Write output into single file
        /// </summary>
        /// <param name="destinationFile">File to write to</param>
        private void WriteSingleFile(Uri destinationFile)
        {
            using (StreamWriter write = new StreamWriter(destinationFile.OriginalString, false))
            {
                var itw = new IndentedTextWriter(write);
                Provider.GenerateCodeFromCompileUnit(_codeDomeCompileUnit, itw, new CodeGeneratorOptions());
                itw.Close();
            }
        }
        
        /// <summary>
        /// Creates the code for an XML element
        /// </summary>
        /// <param name="doc">The <see cref="XElement"/> to create a file for</param>
        /// <returns>The created element or null if the element already exists</returns>
        protected CodeTypeDeclaration CreateElement(XElement doc)
        {
            if (_createdTypes.Contains(doc.Name.LocalName))
            {
                return null;
            } else
            {
                _createdTypes.Add(doc.Name.LocalName);
            }

            var isCollection = doc.Elements().Count() != doc.Elements().Select(e => e.Name.LocalName).Distinct().Count();
            if(isCollection)
            {
                return CreateCollectionElement(doc);
            }
            else
            {
                return CreateRegularElement(doc);
            }
        }

        /// <summary>
        /// Creates the base <see cref="ConfigurationSection"/> class for the configuration section
        /// </summary>
        /// <param name="doc">The xml document root</param>
        protected void CreateSectionElement(XElement doc)
        {
            var postfix = "";
            if(!doc.Name.LocalName.EndsWith("Section"))
            {
                postfix = "Section";
            }
            CodeTypeDeclaration element = new CodeTypeDeclaration(string.Format("{0}{1}",UpperCaseFirst(doc.Name.LocalName),postfix));
            element.IsClass = true;
            element.TypeAttributes = System.Reflection.TypeAttributes.Public;
            element.BaseTypes.Add(new CodeTypeReference { BaseType = typeof(ConfigurationSection).FullName });
            AddAttibutes(doc, element);
            _namespace.Types.Add(element);

            foreach (var item in doc.Elements())
            {
                var sub = CreateElement(item);
                AddAttribute(item.Name.LocalName, element, new CodeTypeReference(sub.Name));
            }
        }

        /// <summary>
        /// Creates an <see cref="ConfigurationElement"/> object
        /// </summary>
        /// <param name="doc">The <see cref="XElement"/> to create an object for</param>
        /// <returns>The created object</returns>
        protected CodeTypeDeclaration CreateRegularElement(XElement doc)
        {
            CodeTypeDeclaration element = new CodeTypeDeclaration(GetRegularElementName(doc));
            element.IsClass = true;
            element.TypeAttributes = System.Reflection.TypeAttributes.Public;
            element.BaseTypes.Add(new CodeTypeReference { BaseType = typeof(ConfigurationElement).FullName });
            AddAttibutes(doc, element);
            _namespace.Types.Add(element);
            return element;
        }

        /// <summary>
        /// Get class name for a ConfigurationElement
        /// </summary>
        /// <param name="element">Xml element representing the classe</param>
        /// <returns>Name of the class to be generated</returns>
        protected string GetRegularElementName(XElement element)
        {
            return string.Format("{0}{1}",UpperCaseFirst(element.Name.LocalName), "Element");
        }

        /// <summary>
        /// Get Class name for a ConfigurationCollection
        /// </summary>
        /// <param name="element">Element to determine name for</param>
        /// <returns>Classname to use</returns>
        protected string GetCollectionElementName(XElement element)
        {
            return string.Format("{0}{1}", UpperCaseFirst(element.Name.LocalName ), "Collection");
        }

        /// <summary>
        /// Creates an <see cref="ConfigurationElementCollection"/> object and it's child element
        /// </summary>
        /// <param name="doc">The <see cref="XElement"/> object to create an object from</param>
        /// <returns>The created object</returns>
        protected CodeTypeDeclaration CreateCollectionElement(XElement doc)
        {
            CodeTypeDeclaration element = new CodeTypeDeclaration(GetCollectionElementName(doc));
            string collectionType = "";
            collectionType = GetRegularElementName(doc.Elements().First());
            element.CustomAttributes.Add(new CodeAttributeDeclaration(
                "ConfigurationCollection", 
                new CodeAttributeArgument[]
                {
                    new CodeAttributeArgument(new CodeTypeOfExpression(collectionType)),
                    new CodeAttributeArgument("AddItemName",new CodePrimitiveExpression(doc.Elements().First().Name.LocalName))
                }
            ));
            element.TypeAttributes = System.Reflection.TypeAttributes.Public;
            element.BaseTypes.Add(new CodeTypeReference { BaseType = typeof(ConfigurationElementCollection).FullName });

            var sub = CreateElement(doc.Elements().First());
            AddCollectionMethods(element, sub);

            AddAttibutes(doc, element);

            _namespace.Types.Add(element);
            return element;
        }

        /// <summary>
        /// Adds <see cref="ConfigurationElementCollection.CreateNewElement"/> and <see cref="ConfigurationElementCollection.GetElementKey(ConfigurationElement)"/>
        /// </summary>
        /// <param name="element">Element to add methods to</param>
        /// <param name="sub">The child item type of the collection</param>
        private void AddCollectionMethods(CodeTypeDeclaration element, CodeTypeDeclaration sub)
        {
            // Add "protected override ConfigurationElement CreateNewElement()"
            var createNewElement = new CodeMemberMethod
            {
                ReturnType = new CodeTypeReference(typeof(ConfigurationElement)),
                Attributes = MemberAttributes.Override | MemberAttributes.Family,
                Name = "CreateNewElement",
            };
            createNewElement.Statements.Add(new CodeMethodReturnStatement(new CodeObjectCreateExpression(sub.Name)));
            element.Members.Add(createNewElement);

            // Add "protected override object GetElementKey(ConfigurationElement element)"
            var getElementKey = new CodeMemberMethod
            {
                ReturnType = new CodeTypeReference(typeof(object)),
                Attributes = MemberAttributes.Override | MemberAttributes.Family,
                Name = "GetElementKey",
            };
            getElementKey.Parameters.Add(new CodeParameterDeclarationExpression(new CodeTypeReference(typeof(ConfigurationElement)), "element"));
            getElementKey.Statements.Add(
                new CodeMethodReturnStatement(new CodePropertyReferenceExpression(
                    new CodeCastExpression(
                        new CodeTypeReference(sub.Name),
                        new CodeVariableReferenceExpression("element")
                    )
                    ,sub.Members[0].Name
                )));
            element.Members.Add(getElementKey);
        }

        /// <summary>
        /// Add xml attributes as properties to the <see cref="ConfigurationElement"/> class
        /// </summary>
        /// <param name="doc">The <see cref="XElement"/> containing the attributes</param>
        /// <param name="element">The type to add the properties to</param>
        protected void AddAttibutes(XElement doc, CodeTypeDeclaration element)
        {
            foreach (var item in doc.Attributes())
            {
                AddAttribute(item.Name.LocalName, element, new CodeTypeReference(typeof(string)));
            }
        }

        /// <summary>
        /// Add a single property to type definition
        /// </summary>
        /// <param name="name">Name of the property</param>
        /// <param name="element">Type definition to add to</param>
        /// <param name="attributeType"><see cref="Type"/> of the attribute to add</param>
        private void AddAttribute(string name, CodeTypeDeclaration element, CodeTypeReference attributeType)
        {
            var property = new CodeMemberProperty();
            property.Name = UpperCaseFirst(name);
            property.Type = attributeType;
            property.Attributes = MemberAttributes.Public | MemberAttributes.Final;
            property.GetStatements.Add(
                new CodeMethodReturnStatement(
                    new CodeCastExpression(
                        attributeType,
                        new CodeIndexerExpression(
                            new CodeThisReferenceExpression(),
                            new CodePrimitiveExpression(name)
                        ))));

            property.SetStatements.Add(
                new CodeAssignStatement(
                    new CodeIndexerExpression(
                        new CodeThisReferenceExpression(),
                        new CodePrimitiveExpression(name)
                    ),
                    new CodePropertySetValueReferenceExpression()
                ));
            property.CustomAttributes.Add(new CodeAttributeDeclaration(new CodeTypeReference("ConfigurationProperty"), new CodeAttributeArgument[] {
                    new CodeAttributeArgument(new CodePrimitiveExpression (name))
                }));
            element.Members.Add(property);
        }

        /// <summary>
        /// Helper function to uppercase first character in a string
        /// </summary>
        /// <param name="s">String to uppercase</param>
        /// <returns></returns>
        private string UpperCaseFirst(string s)
        {
            if(string.IsNullOrEmpty(s))
            {
                return s;
            }
            return char.ToUpper(s[0]) + s.Substring(1);
        }
    }
}
