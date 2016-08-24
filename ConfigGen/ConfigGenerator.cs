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
    /// Class that can generate code for configuration sections
    /// </summary>
    public class ConfigGenerator
    {
        #region Fields
        private readonly XDocument ConfigurationSectionXml;
        private CodeCompileUnit CodeDomeCompileUnit;
        private CodeNamespace Namespace;
        private List<string> CreatedTypes = new List<string>();
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
        public ConfigGenerator(Uri path)
        {
            init();
            ConfigurationSectionXml = XDocument.Load(path.OriginalString);
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
                ConfigurationSectionXml = XDocument.Load(stream);
            }            
        }


        private void init()
        {
            CodeDomeCompileUnit = new CodeCompileUnit();
            Namespace = new CodeNamespace("Config");
            Namespaces = new ConfigurationNamespaceCollection();
            Namespaces.Add("System");
            Namespaces.Add("System.Collections");
            Namespaces.Add("System.Text");
            Namespaces.Add("System.Configuration");
            CodeDomeCompileUnit.Namespaces.Add(Namespace);
            
        }
        #endregion

        /// <summary>
        /// Generate source code for the configuration
        /// </summary>
        /// <param name="destinationFile">File location to write output to. Will overwrite the file if it already exists</param>
        public void GenerateConfig(Uri destinationFile)
        {
            init();
            Namespace.Imports.AddRange(Namespaces.ToArray());
            var root = ConfigurationSectionXml.Root;
            CreateSectionElement(root);
            if(Provider == null)
            {
                Provider = new CSharpCodeProvider();
            }
            

            using (StreamWriter write = new StreamWriter(destinationFile.OriginalString, false))
            {
                var itw = new IndentedTextWriter(write);
                Provider.GenerateCodeFromCompileUnit(CodeDomeCompileUnit, itw, new System.CodeDom.Compiler.CodeGeneratorOptions());
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
            if (CreatedTypes.Contains(doc.Name.LocalName))
            {
                return null;
            } else
            {
                CreatedTypes.Add(doc.Name.LocalName);
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
            CodeTypeDeclaration element = new CodeTypeDeclaration(UpperCaseFirst(doc.Name.LocalName));
            element.IsClass = true;
            element.TypeAttributes = System.Reflection.TypeAttributes.Public;
            element.BaseTypes.Add(new CodeTypeReference { BaseType = typeof(ConfigurationSection).FullName });
            AddAttibutes(doc, element);
            Namespace.Types.Add(element);

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
            CodeTypeDeclaration element = new CodeTypeDeclaration(UpperCaseFirst(doc.Name.LocalName));
            element.IsClass = true;
            element.TypeAttributes = System.Reflection.TypeAttributes.Public;
            element.BaseTypes.Add(new CodeTypeReference { BaseType = typeof(ConfigurationElement).FullName });
            AddAttibutes(doc, element);
            Namespace.Types.Add(element);
            return element;
        }


        /// <summary>
        /// Creates an <see cref="ConfigurationElementCollection"/> object and it's child element
        /// </summary>
        /// <param name="doc">The <see cref="XElement"/> object to create an object from</param>
        /// <returns>The created object</returns>
        protected CodeTypeDeclaration CreateCollectionElement(XElement doc)
        {
            CodeTypeDeclaration element = new CodeTypeDeclaration(UpperCaseFirst(doc.Name.LocalName + "Collection"));
            string collectionType = "";
            collectionType = UpperCaseFirst(doc.Elements().First().Name.LocalName);
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

            Namespace.Types.Add(element);
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
                            new CodePrimitiveExpression(name.ToLower())
                        ))));

            property.SetStatements.Add(
                new CodeAssignStatement(
                    new CodeIndexerExpression(
                        new CodeThisReferenceExpression(),
                        new CodePrimitiveExpression(name.ToLower())
                    ),
                    new CodePropertySetValueReferenceExpression()
                ));
            property.CustomAttributes.Add(new CodeAttributeDeclaration(new CodeTypeReference("ConfigurationProperty"), new CodeAttributeArgument[] {
                    new CodeAttributeArgument(new CodePrimitiveExpression (name.ToLower()))
                }));
            element.Members.Add(property);
        }

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
