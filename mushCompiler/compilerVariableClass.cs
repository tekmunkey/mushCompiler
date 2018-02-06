using System;
using System.Collections.Generic;
using System.Text;

namespace mushCompiler
{
    /// <summary>
    /// Compiler variables provide a means of defining name/value pairs in the context of your codefile, and converting them into MUSH-meaningful code values.  
    /// 
    /// cVars and bVars are intended for use as compiler-defined functions or macros, while qVars and parameters are intended for use as named parameters into 
    /// a softcoded function or command.  cVars are scoped at the file level, bVars, qVars, and params at the block level.
    /// </summary>
    class compilerVariableClass
    {
        /// <summary>
        /// The name by which you'll know this variable.
        /// </summary>
        public string name;

        /// <summary>
        /// The value assigned to this variable.
        /// </summary>
        public string value;

        /// <summary>
        /// Indicates whether this variable defaults to wrapping in objeval()
        /// </summary>
        public bool defaultSafe;

        /// <summary>
        /// Performs replacement of all occurrances of this compilerVariableClass instance's name with this instance's value, within the specified string.
        /// </summary>
        /// <param name="codeLine">
        /// The string to perform replacement within.
        /// </param>
        /// <returns>
        /// The specified string with variable replacements performed.
        /// </returns>
        public string replaceVariable(string codeLine)
        {
            string r = codeLine;

            if (!string.IsNullOrEmpty(this.name.Trim()) && !string.IsNullOrEmpty(r))
            {
                r = r.Replace(this.name + @"UNSAFE", this.value);
                r = r.Replace(this.name + @"SAFE", @"objeval(%#," + this.value + @")");
                r = r.Replace(this.name + @"LIT", @"lit(" + this.value + @")");

                if (!this.defaultSafe)
                {
                    r = r.Replace(this.name, this.value);
                }
                else
                {
                    r = r.Replace(this.name, @"objeval(%#," + this.value + @")");
                }

                
                
            }
            return r;
        }

        /// <summary>
        /// Deconstructor.  Performs instance variable cleanups.
        /// </summary>
        ~compilerVariableClass()
        {
            this.name = null;
            this.value = null;
            GC.Collect();
        }

        /// <summary>
        /// Creates a new compilerVariableClass instance given the specified name and value.
        /// </summary>
        /// <param name="varName">
        /// The name by which you'll refer to the new variable.
        /// </param>
        /// <param name="varValue">
        /// The value the new variable references will be replaced with at compile time.
        /// </param>
        /// <returns>
        /// A new compilerVariableClass instance.
        /// </returns>
        public static compilerVariableClass getCompilerVariable(string varName, string varValue)
        {
            compilerVariableClass r = new compilerVariableClass();
            r.name = varName.Trim();
            r.value = varValue.Trim();
            return r;
        }

        /// <summary>
        /// Creates a new compilerVariableClass instance given a variable name and value in a valid varString format.  
        /// 
        /// A valid fromVarString is in the format varName=varValue or varName = varValue 
        /// </summary>
        /// <param name="fromVarString">
        /// When you write a compilerVariable or cVar or cVarString into your MUSH code, you'll write it in the form of 'cVarName=cVarValue' 
        /// or 'cVarName = cVarValue' (of course you won't write it with quotation marks).  
        /// 
        /// What gets passed into this function is that string, to be split into its name/value components for storage and further 
        /// processing throughout the file lifecycle.
        /// </param>
        /// <returns>
        /// A compilerVariableClass instance.
        /// </returns>
        public static compilerVariableClass getCompilerVariable(string fromVarString)
        {
            compilerVariableClass r = new compilerVariableClass();

            // The var name is on the left side of the =
            r.name = fromVarString.Substring(0, fromVarString.IndexOf(@"=") - 1).Trim();
            // The var value is on the right side of the =
            r.value = fromVarString.Substring(fromVarString.IndexOf(@"=") + 1).Trim();

            return r;
        }
    }
}
