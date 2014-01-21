﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ProtoCore.NameSpace
{
    /// <summary>
    /// Symbol class : It represents a symbol with namespace.
    /// </summary>
    public class Symbol
    {
        #region Private Members

        private string[] namespaces;
        private string symbolname;
        
        private static string[] GetNameSpaces(string name, out string symbolname)
        {
            var names = name.Split('.');
            int size = names.Length;
            symbolname = names[size-1];
            return names;
        }

        #endregion

        #region Public Methods and Constructor

        /// <summary>
        /// Constructs a FullyQualifiedSymbolName with the given fullname.
        /// </summary>
        /// <param name="fullname">fullname for the symbol including namespaces.
        /// </param>
        public Symbol(string fullname)
        {
            FullName = fullname;
            namespaces = GetNameSpaces(fullname, out symbolname);
        }

        /// <summary>
        /// Gets fully qualified symbol name.
        /// </summary>
        public string FullName { get; private set; }

        /// <summary>
        /// Gets symbol name
        /// </summary>
        public string Name { get { return symbolname; } }

        /// <summary>
        /// Gets symbol id
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Checks if all of the namespace prefixes specified in the given 
        /// partially qualified name appear in this namespace in the same order.
        /// For Example:
        /// A full namespace "Com.Autodesk.Designscript.ProtoGeometry.Point" 
        /// will match all of the following partial namespaces
        /// Com.Autodesk.Designscript.ProtoGeometry.Point
        /// Point
        /// DesignScript.Point
        /// ProtoGeometry.Point
        /// Autodesk.DesignScript.Point
        /// whereas it won't match Com.DesignScript.Autodesk.Point
        /// </summary>
        /// <param name="partialname">Partially qualified symbol name</param>
        /// <returns>returns true if partial name matches this</returns>
        public bool Matches(string partialname)
        {
            string symbol;
            var given = GetNameSpaces(partialname, out symbol);

            //Match the symbold name first
            if (!this.Name.Equals(symbol))
                return false;

            int index = 0;
            for (int i = 0; i < namespaces.Length && index < given.Length; ++i)
            {
                if (namespaces[i].Equals(given[index]))
                    ++index;
            }
            return index == given.Length;
        }

        /// <summary>
        /// Checks equality based on FullName
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public override bool Equals(object obj)
        {
            Symbol symbol = obj as Symbol;
            if (null == symbol)
                return false;

            return this.FullName.Equals(symbol.FullName);
        }

        /// <summary>
        /// Gets hascode based on FullName
        /// </summary>
        /// <returns></returns>
        public override int GetHashCode()
        {
            return this.FullName.GetHashCode();
        }

        #endregion
    }

    /// <summary>
    /// SymbolTable class
    /// </summary>
    class SymbolTable
    {
        /// <summary>
        /// Table for all symbols
        /// </summary>
        private Dictionary<string, HashSet<Symbol>> symbolTable;

        /// <summary>
        /// Constructor
        /// </summary>
        public SymbolTable()
        {
            symbolTable = new Dictionary<string, HashSet<Symbol>>();
        }

        #region Public Methods

        /// <summary>
        /// Adds the given symbol to this symbol table
        /// </summary>
        /// <param name="fullname">Fully qualified name for the symbol</param>
        /// <returns>The newly added symbol if added successfully, else null.</returns>
        public Symbol AddSymbol(string fullname)
        {
            Symbol symbol = new Symbol(fullname);
            if (AddSymbol(symbol))
                return symbol;
            return null;
        }

        /// <summary>
        /// Adds the given symbol to this symbol table
        /// </summary>
        /// <param name="qualifiedSymbol">FullyQualifiedSymbolName</param>
        /// <returns>True if symbol is added successfully, false if the symbol was 
        /// already present in the table.</returns>
        public bool AddSymbol(Symbol qualifiedSymbol)
        {
            string symbolName = qualifiedSymbol.Name;

            HashSet<Symbol> container = null;
            if (!symbolTable.TryGetValue(symbolName, out container))
            {
                container = new HashSet<Symbol>();
                symbolTable.Add(symbolName, container);
            }
            return container.Add(qualifiedSymbol);
        }

        /// <summary>
        /// Gets all matching symbols for the given partially qualified symbol.
        /// </summary>
        /// <param name="partialName">Partially qualified symbol</param>
        /// <returns>An array of all matched symbols</returns>
        /// <exception cref="System.Collections.Generic.KeyNotFoundException"></exception>
        public Symbol[] GetMatchingSymbols(string partialName)
        {
            string symbolName = partialName.Split('.').Last();
            HashSet<Symbol> symbols = GetAllSymbols(symbolName);
            if (null == symbols)
                throw new System.Collections.Generic.KeyNotFoundException(string.Format("Failed to get unique matching symbol for {0}.", partialName));

            return symbols.Where((Symbol sym) => sym.Matches(partialName)).ToArray();
        }

        /// <summary>
        /// Returns fully qualified name for the given partial name if it 
        /// resolves to a unique symbol.
        /// </summary>
        /// <param name="partialName">partial symbol name</param>
        /// <returns>Fully qualified name.</returns>
        /// <exception cref="System.Collections.Generic.KeyNotFoundException"></exception>
        public string GetFullyQualifiedName(string partialName)
        {
            var symbols = GetMatchingSymbols(partialName);
            if (symbols == null || symbols.Length != 1)
                throw new System.Collections.Generic.KeyNotFoundException(string.Format("Failed to get unique matching symbol for {0}.", partialName));

            return symbols.First().FullName;
        }

        /// <summary>
        /// Finds a symbol in this table which has exactly same as given fully
        /// qualified name.
        /// </summary>
        /// <param name="fullName">Fully qualified name for lookup</param>
        /// <param name="symbol">Matching symbol for given fullName.</param>
        /// <returns>True if exact matching symbol is found.</returns>
        public bool TryGetExactSymbol(string fullName, out Symbol symbol)
        {
            Symbol[] symbols = TryGetSymbols(fullName, (Symbol s) => s.FullName.Equals(fullName));
            if (symbols.Length == 1)
                symbol = symbols[0];
            else
                symbol = null;

            return symbol != null;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="partialName"></param>
        /// <param name="symbol"></param>
        /// <returns></returns>
        public bool TryGetUniqueSymbol(string partialName, out Symbol symbol)
        {
            Symbol[] symbols = TryGetSymbols(partialName, (Symbol s) => s.Matches(partialName));
            if (symbols.Length == 1)
                symbol = symbols[0];
            else
                symbol = null;

            return symbol != null;
        }

        /// <summary>
        /// Gets total symbol count in the table
        /// </summary>
        /// <returns>Symbol count</returns>
        public int GetSymbolCount()
        {
            int count = 0;
            foreach (var item in symbolTable)
            {
                count += item.Value.Count;
            }
            return count;
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// 
        /// </summary>
        /// <param name="name"></param>
        /// <param name="predicate"></param>
        /// <returns></returns>
        private Symbol[] TryGetSymbols(string name, Func<Symbol, bool> predicate)
        {
            string symbolName = name.Split('.').Last();
            HashSet<Symbol> symbolSet = GetAllSymbols(symbolName);
            if (null != symbolSet)
                return symbolSet.Where(predicate).ToArray();

            return new Symbol[0];
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        private HashSet<Symbol> GetAllSymbols(string symbolName)
        {
            HashSet<Symbol> symbols = null;
            symbolTable.TryGetValue(symbolName, out symbols);
            return symbols;
        }

        #endregion
    }
}
