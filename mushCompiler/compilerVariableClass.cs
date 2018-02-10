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
                //
                // the actual syntax for using cvars to set qvars should be:
                //   cvarSETQ(theNewValueForThisQVarHere)
                //   * If and only if the cVar is initialized as a q-register already
                //
                if (r.Contains(this.name + @"SETQ"))
                {
                    if (!(this.value.Length.Equals(3) && this.value.Substring(0, 2).ToLower().Equals(@"%q")))
                    {
                        throw new System.Exception(@"SETQ on Compiler Variable " + this.name + @" but the value in that variable is not a Q-Variable.");
                    }

                    do
                    {
                        int startIndex = r.IndexOf(this.name + @"SETQ");
                        // remove the cvar name and the SETQ tag
                        r = r.Remove(startIndex, this.name.Length + @"SETQ".Length);
                        // starting at startIndex, jump over the ( and insert the q-register NUMBER ONLY, followed by a comma
                        r = r.Insert(startIndex + 1, this.value.Substring(2, 1) + @",");
                        // re-insert the term 'setq' at startIndex in all-lowercase
                        r = r.Insert(startIndex, @"setq");
                    } while (r.Contains(this.name + @"SETQ"));
                }

                if (r.Contains(this.name + @"SETR"))
                {
                    if (!(this.value.Length.Equals(3) && this.value.Substring(0, 2).ToLower().Equals(@"%q")))
                    {
                        throw new System.Exception(@"SETR on Compiler Variable " + this.name + @" but the value in that variable is not a Q-Variable.");
                    }

                    do
                    {
                        int startIndex = r.IndexOf(this.name + @"SETR");
                        // remove the cvar name and the SETQ tag
                        r = r.Remove(startIndex, this.name.Length + @"SETR".Length);
                        // starting at startIndex, jump over the ( and insert the q-register NUMBER ONLY, followed by a comma
                        r = r.Insert(startIndex + 1, this.value.Substring(2, 1) + @",");
                        // re-insert the term 'setq' at startIndex in all-lowercase
                        r = r.Insert(startIndex, @"setr");
                    } while (r.Contains(this.name + @"SETR"));
                }

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
