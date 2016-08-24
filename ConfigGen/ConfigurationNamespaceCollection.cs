using System;
using System.CodeDom;
using System.Collections;
using System.Collections.Generic;

namespace ConfigGen
{
    public class ConfigurationNamespaceCollection : IReadOnlyCollection<CodeNamespaceImport>
    {
        private ICollection<CodeNamespaceImport> _imports;

        public ConfigurationNamespaceCollection()
        {
            _imports = new List<CodeNamespaceImport>();
        }

        public int Count
        {
            get
            {
                return _imports.Count;
            }
        }

        public IEnumerator<CodeNamespaceImport> GetEnumerator()
        {
            return _imports.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return _imports.GetEnumerator();
        }

        public void Add(string ns)
        {
            _imports.Add(new CodeNamespaceImport(ns));
        } 
    }
}