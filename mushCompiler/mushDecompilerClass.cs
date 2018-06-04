using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace mushCompiler
{
    public class mushDecompilerClass
    {
        /// <summary>
        /// Contains the filename currently being decompiled.
        /// </summary>
        string filePath = string.Empty;

        /// <summary>
        /// This field is where "decompiled" code is constructed at runtime.
        /// </summary>
        private string deCompiledCodeString = string.Empty;

        public string DecompiledCode
        {
            get
            {
                return deCompiledCodeString;
            }
        }

        

         //
        // Standard comments in compiled code need to be converted to documentation comments per compile operation
        //     ie:  @@ -> @@@@
        //     Also, these should have single-spacing between lines preserved, while others should always be pushed apart by 
        //     additional carriage returns.
        //
        // Group 1 is any leading whitespace
        // Group 2 is comment header - must be at least two contiguous @ characters but may be more
        // Group 3 is any whitespace following comment header
        // Group 4 is any actual commentation value
        //
        private string regexCommentLine = @"^(\s*)?(@{2,})(\s*)?(.*)?$";

        /// <summary>
        /// If the specified codeline is in fact a commentation header, converts it to a compiler-recognized Documentation Comment
        /// </summary>
        /// <param name="codeline"></param>
        /// <returns></returns>
        private bool decompComment(string codeline)
        {
            bool r = Regex.IsMatch(codeline, regexCommentLine);

            // Do nothing if codeline is not a regex match against the commentation pattern
            if (r)
            {
                // When there is a match
                Match rxm = Regex.Match(codeline, regexCommentLine, RegexOptions.None);
                // Ensure that output has the extra two @@ chars at the front of it, which would be stripped by subsequent 
                // compile operations
                this.deCompiledCodeString += rxm.Groups[1] + @"@@" + rxm.Groups[2].Value;

                //
                // Ensure that the comment header has at least one whitespace when the input text has none
                //   * Sometimes a coder will use @@ alone (a blank comment) just as a visual/readability offsetter, so only 
                //     bloat the line with extra whitespace where there's an actual commentation
                //
                if (!string.IsNullOrEmpty(rxm.Groups[4].Value))
                {
                    // Where group 4 is legit, append group 3 or append at least 1 whitespace
                    this.deCompiledCodeString += (!string.IsNullOrEmpty(rxm.Groups[3].Value) ? rxm.Groups[3].Value : @" ");

                    // Finally add the commentation itself
                    this.deCompiledCodeString += rxm.Groups[4].Value;
                }

                this.deCompiledCodeString += System.Environment.NewLine;

                rxm = null;
            }

            return r;
        }

        //
        // Custom command definition lines in attributes typically start with &
        //     ie:  &someattrib someobj=$commandDef arg0 arg1:somedata
        //
        // Group 1 is any leading whitespace
        // Group 2 is attribute-setting name (ie:  &attribName)
        // Group 3 is any object-name or dbref
        // Group 5 is any command-specification (command name and arguments)
        // Group 5 is attribute-value contents
        //
        private string regexCommandLine = @"^(\s*)?(\&\S+)?\s+?(.+)?=(\$.+)?\:(.*)$";

        private bool decompCommand(string codeline)
        {
            bool r = Regex.IsMatch(codeline, regexCommandLine);

            // Do nothing if codeline is not a regex match against the command definition pattern
            if (r)
            {
                // When there is a match
                Match rxm = Regex.Match(codeline, regexCommandLine, RegexOptions.None);

                int indentFactor = 0; // number of indent operations to perform on next line of code
                char indentChar = (char)0x20; // use single standard whitespace as per-indent-width char
                int indentWidth = 4; // use this many indentChar occurrences in a single indent operation

                // Add the attribute-setter to output
                this.deCompiledCodeString += rxm.Groups[2].Value + @" " + rxm.Groups[3].Value + @" = ";
                this.deCompiledCodeString += System.Environment.NewLine;
                indentFactor++;
                // Add indentation
                this.deCompiledCodeString += new string(indentChar, (indentWidth * indentFactor));
                // Add command-spec
                this.deCompiledCodeString += rxm.Groups[4] + @":";
                this.deCompiledCodeString += System.Environment.NewLine;
                indentFactor++;

                //
                // The next regex match group contains softcode to some extent or other
                //
                // First cut all non-escaped semicolon characters to newlines (these are internal command-call 
                // delimiters when found in custom-command contents)
                //   * This requires some specialized handling due to DotNET not supporting "split at X but not yX"
                //
                // Initializing internal-calls-array to contain the total string value worked with at position 0...
                // this can be useful later on in debug output
                string[] icArr = new string[] { rxm.Groups[5].Value };
                bool escNext = false;
                for (int i = 0; i < icArr[0].Length; i++)
                {
                    // Get the character at index i of the string at icArr[0] (where icArr[0] is the full string)
                    char c = icArr[0][i];

                    if (!escNext)
                    {
                        //
                        // When we're not planning to escape the next character to produce a literal
                        //
                        // if current char is escape char, plan to escape next character
                        escNext = mushCode.characters.isCharIn( mushCode.characters.escChars, c);

                        if (mushCode.characters.isCharIn( mushCode.characters.breakBeforeChars, c ) && !(mushCode.characters.isCharIn(mushCode.characters.lineTerms, this.deCompiledCodeString[deCompiledCodeString.Length - 1])))
                        {
                            // Only when c is a designated line-break char
                            this.deCompiledCodeString += System.Environment.NewLine;
                        }

                        if (mushCode.characters.isCharIn(mushCode.characters.dedentTriggers,c)) 
                        { 
                            indentFactor--; 
                        }

                        // Only when the previous character is a linebreak (CR or LF)
                        if (mushCode.characters.isCharIn(mushCode.characters.lineTerms, this.deCompiledCodeString[deCompiledCodeString.Length - 1]))
                        {
                            // Add line-indent
                            this.deCompiledCodeString += new string(indentChar, (indentWidth * indentFactor));
                        }

                        if (mushCode.characters.isCharIn(mushCode.characters.indentTriggers, c)) 
                        {
                            indentFactor++;
                        }
                        
                        
                        // Add current char to output
                        this.deCompiledCodeString += c;

                        if (mushCode.characters.isCharIn(mushCode.characters.breakAfterChars, c))
                        {
                            // Only when c is a designated line-break char
                            this.deCompiledCodeString += System.Environment.NewLine;
                        }
                    }
                    else
                    {
                        // When we are planning to escape the next character to produce a literal
                        //
                        // Whether current char is escape or not, print it and toggle escapenext off
                        escNext = false;
                        // Add the char to output without additional processing
                        this.deCompiledCodeString += c;
                    }

                    GC.Collect();
                }

                //this.deCompiledCodeString += new string(indentChar, (indentWidth * indentFactor));
                //this.deCompiledCodeString += rxm.Groups[5];
                this.deCompiledCodeString += System.Environment.NewLine + System.Environment.NewLine;
            }

            return r;
        }

        /// <summary>
        /// MUSH Attributes may contain straight data or function-processing code to return data or custom commands.
        /// </summary>
        /// <param name="codeline"></param>
        /// <returns></returns>
        private bool decompAttribute(string codeline)
        {
            bool r = false;

            return r;
        }

        public string processCodeLine(string codeLine)
        {
            string r = string.Empty;

            // empty/blank text files caused program crashes.  problem solved.
            if (object.ReferenceEquals(codeLine, null)) { codeLine = string.Empty; }

            
            
            
            
            
            //string teststr = @"This is \a \t\\\est of isCharEscap\ed";
            //uint testi = mushCode.characters.countUnescaped(teststr, 'e');





            if (decompComment(codeLine))
            {
                // Comments are processed inside the decompComment function
                // Add any secondary functions here
            }
            else if (decompCommand(codeLine))
            {
                // Commands are processed inside the decompCommand function
                // Add any secondary functions here
            }
            else
            {
                this.deCompiledCodeString += codeLine;
                // Non-comments must be doublespaced per compiler input specs
                this.deCompiledCodeString += System.Environment.NewLine + System.Environment.NewLine;
            }


            return r;
        }





        internal mushDecompilerClass(string fileIn)
        {
        }
    }
}
