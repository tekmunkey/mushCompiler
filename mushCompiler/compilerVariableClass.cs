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

        public string safeValue
        {
            get
            {
                return @"objeval(%#," + this.value + @")";
            }
        }

        public string defaultValue
        {
            get
            {
                string r = string.Empty;
                if (!this.defaultSafe)
                {
                    r = this.value;
                }
                else
                {
                    r = this.safeValue;
                }
                return r;
            }
        }

        /// <summary>
        /// Indicates whether this variable defaults to wrapping in objeval()
        /// </summary>
        public bool defaultSafe;

        /// <summary>
        /// Performs addonFunc replacement operations for setq and setr addons, which require special treatment...  
        /// 
        /// Where most MUSH functions would require function(%q0,param1,param2) and etc, setq and setr require setq(0,param1) or 
        /// setr(1,param1) and then you retrieve the value thusly set by %q0.
        /// </summary>
        /// <param name="codeLine">
        /// The string to perform replacement within.
        /// </param>
        /// <param name="qOrR">
        /// If Q, performs setQ replacement.  If R, performs setR replacement.  
        /// </param>
        /// <returns>
        /// The specified string with addonFunc (setq or setr) replacements performed.
        /// </returns>
        private string replaceSETQorR(string codeLine, string qOrR)
        {
            if (!qOrR.Length.Equals(1) && !(qOrR.ToUpper().Equals(@"Q") || qOrR.ToUpper().Equals(@"R")))
            {
                throw new System.Exception(@"replaceSETQorR requires the qOrR parameter to be, obviously, either Q or R (case insensitive).  " + qOrR + " was passed instead.");
            }
            
            string r = codeLine;
            string cvarAddonFunc = @".SET" + qOrR.ToUpper();
            //
            // the actual syntax for using cvars to set qvars should be:
            //     cvar.SETQ(theNewValueForThisQVarHere)
            //   Which is equivalent to:
            //     setq(cvar,theNewValueForThisQVarHere)
            //   * If and only if the cVar is initialized as a q-register already
            //
            if (r.Contains(this.name + cvarAddonFunc.ToUpper()))
            {
                if (!(this.value.Length.Equals(3) && this.value.Substring(0, 2).ToLower().Equals(@"%q")))
                {
                    throw new System.Exception(cvarAddonFunc + @" on Compiler Variable " + this.name + @" but the value in that variable is not a Q-Variable.");
                }

                do
                {
                    int startIndex = r.IndexOf(this.name + cvarAddonFunc);
                    // remove the cvar name and the SETQ tag
                    r = r.Remove(startIndex, this.name.Length + cvarAddonFunc.Length);
                    // starting at startIndex, jump over the ( and insert the q-register NUMBER ONLY, followed by a comma
                    r = r.Insert(startIndex + 1, this.value.Substring(2, 1) + @",");
                    // re-insert the term 'setq' at startIndex in all-lowercase
                    //   * Get the substring from cvarAddonFunc signature starting at char idx 1 and traversing to string length 
                    //     This eliminates the '.' character from the addon func and injects it into the output code
                    r = r.Insert(startIndex, cvarAddonFunc.ToLower().Substring(1));
                } while (r.Contains(this.name + cvarAddonFunc));
            }

            if (this.name == "bOutput")
            {
                int bp = 0;
            }

            return r;
        }

        /// <summary>
        /// Performs addonFunc replacement operations for standard addons that require special handling, such as strlen() which takes only 1 parameter.  
        /// 
        /// Where most MUSH functions require function(%0,param1,param2) and etc, strlen() requires only strlen(%0) - the standard addon replacement function 
        /// automatically adds a comma, and making this separate callout is simpler/faster than figuring out how to test/parse for parameters since I'm actually 
        /// adding this feature to the compiler while writing MUSH code at the same time.
        /// </summary>
        /// <param name="codeLine">
        /// The string to perform replacement within.
        /// </param>
        /// <param name="funcname">
        /// The name of the function to perform replacement with.
        /// </param>
        /// <returns>
        /// The specified string with addonFunc (funcName) replacements performed.
        /// </returns>
        private string replaceAddonFunc_NOPARAM(string codeLine, string funcName)
        {
            string r = codeLine;
            string cvarAddonFunc = funcName.ToUpper();
            if (r.Contains(this.name + cvarAddonFunc))
            {
                do
                {
                    int startIndex = r.IndexOf(this.name + cvarAddonFunc);
                    // remove the cvar name and the ADDONFUNC tag
                    r = r.Remove(startIndex, this.name.Length + cvarAddonFunc.Length);
                    // starting at startIndex, jump over the ( and insert the variable-value ONLY, followed by a comma
                    r = r.Insert(startIndex + 1, this.defaultValue);
                    // re-insert the ADDONFUNC term at startIndex in all-lowercase
                    //   * Get the substring from cvarAddonFunc signature starting at char idx 1 and traversing to string length 
                    //     This eliminates the '.' character from the addon func and injects it into the output code
                    r = r.Insert(startIndex, cvarAddonFunc.ToLower().Substring(1));
                } while (r.Contains(this.name + cvarAddonFunc));
            }
            return r;
        }

        /// <summary>
        /// Performs addonFunc replacement operations for standard addons.  
        /// 
        /// Where most MUSH functions require function(%0,param1,param2) and etc
        /// </summary>
        /// <param name="codeLine">
        /// The string to perform replacement within.
        /// </param>
        /// <param name="funcname">
        /// The name of the function to perform replacement with.
        /// </param>
        /// <returns>
        /// The specified string with addonFunc (funcName) replacements performed.
        /// </returns>
        private string replaceAddonFunc(string codeLine, string funcName)
        {
            string r = codeLine;
            string cvarAddonFunc = funcName.ToUpper();
            if (r.Contains(this.name + cvarAddonFunc))
            {
                do
                {
                    int startIndex = r.IndexOf(this.name + cvarAddonFunc);
                    // remove the cvar name and the ADDONFUNC tag
                    r = r.Remove(startIndex, this.name.Length + cvarAddonFunc.Length);
                    // starting at startIndex, jump over the ( and insert the variable-value ONLY, followed by a comma
                    r = r.Insert(startIndex + 1, this.defaultValue + @",");
                    // re-insert the ADDONFUNC term at startIndex in all-lowercase
                    //   * Get the substring from cvarAddonFunc signature starting at char idx 1 and traversing to string length 
                    //     This eliminates the '.' character from the addon func and injects it into the output code
                    r = r.Insert(startIndex, cvarAddonFunc.ToLower().Substring(1));
                } while (r.Contains(this.name + cvarAddonFunc));
            }
            return r;
        }

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
                //
                // ISSUE:  Multi-line/exploded-text compile gets broken when SETR/SETQ is on a line by itself, which limits the coder's ability to clearly comment/document code while using this feature.
                // SOLUTION:  Need to de-explode/decomment code before performing variable replacements, rather than performing all tasks simultaneously
                //

                //
                // Allows adding on function specifiers to compiler variable names in codeblocks, to compile down to MUSH code operations
                //
                // SetQ/setR require special handling, replacing the cvar name with setq/r and then adding the cvar value (sans %q) into the 
                // first parameter position inside the following parentheses, followed by a comma
                //
                r = this.replaceSETQorR(r,@"Q");
                r = this.replaceSETQorR(r, @"R");
                //
                // So do the functions that only take 1 parameter, replacing the cvar name with the addonFunc name and then adding the cvar value into 
                // the only parameter position inside the following parentheses, with NO COMMA
                //
                r = this.replaceAddonFunc_NOPARAM(r, @".STRLEN");
                r = this.replaceAddonFunc_NOPARAM(r, @".ISNUM");
                //
                // Other functions with multiple parameters get pushed into a function that replaces the cvar name with the addonFunc name and then 
                // adds the cvar value into the first parameter position inside the following parentheses, followed by a comma
                //
                r = this.replaceAddonFunc(r, @".GT");
                r = this.replaceAddonFunc(r, @".LT");
                r = this.replaceAddonFunc(r, @".SUB");
                r = this.replaceAddonFunc(r, @".ADD");
                r = this.replaceAddonFunc(r, @".IDIV");
                r = this.replaceAddonFunc(r, @".ELEMENTS");
                r = this.replaceAddonFunc(r, @".STRTRUNC");

                r = r.Replace(this.name + @".UNSAFE", this.value);
                r = r.Replace(this.name + @".SAFE", @"objeval(%#," + this.value + @")");
                r = r.Replace(this.name + @".LIT", @"lit(" + this.value + @")");

                r = r.Replace(this.name,this.defaultValue);
                
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
            r.name = fromVarString.Substring(0, fromVarString.IndexOf(@"=")).Trim();
            // The var value is on the right side of the =
            r.value = fromVarString.Substring(fromVarString.IndexOf(@"=") + 1).Trim();

            return r;
        }
    }
}
