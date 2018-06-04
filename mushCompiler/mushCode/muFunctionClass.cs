using System;
using System.Collections.Generic;
using System.Text;

namespace mushCompiler.mushCode
{
    /// <summary>
    /// This class represents a MUSH Softcode function, exposing methods and properties useful in compiling and decompiling and possibly bug-testing if/when 
    /// a parser is added to the compiler.
    /// </summary>
    internal class muFunctionClass
    {
        /// <summary>
        /// MU Code Functions are "force-evaluated" when the original text is encased in square brackets (for example [somefunc()].   
        /// 
        /// When parsing a function this case is tested, and forceEval is set TRUE when the function is so-bracketed.  
        /// 
        /// During output operations, when this value is TRUE, the whole output will be so-encased.
        /// </summary>
        public bool forceEval = false;

        public string name = string.Empty;

        public string[] args = null;

        internal muFunctionClass(string unparsedFunc)
        {

        }

        

        
    }
}
