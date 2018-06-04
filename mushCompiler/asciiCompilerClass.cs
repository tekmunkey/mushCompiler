using System;
using System.Collections.Generic;
using System.IO;


namespace mushCompiler
{
    /// <summary>
    /// The asciiCompilerClass is designed for compiling ASCII art files for MUSH input.  There are several different ways to input 
    /// ASCII art.  
    /// 
    /// Compiling for single-line input means compiling ASCII art to be stored in an object attribute on the MUSH (ie: in a function 
    /// or command) for future/repeat retrieval.  
    /// 
    /// Compiling for multi-line input means compiling ASCII art to be stored in a file-on-disk (ie: on your local hard drive) to be 
    /// uploaded through your client or copy and pasted via manual @emit for future/repeat retrieval.  
    /// 
    /// If your intention is to use ASCII art in a connect.txt or other "hard" file that the MUSH reads from disk and transmits to 
    /// users directly, DO NOT COMPILE IN ANY WAY, SHAPE, OR FORM!  Compiles perform replacements and cArr which you simply don't 
    /// want in this case.
    /// </summary>
    class asciiCompilerClass
    {
        /// <summary>
        /// Processes a line of ASCII art text by simply escaping any characters traditionally needing escaping in MUSH platforms.  
        /// 
        /// Whitespace is transmuted to %b and System.Environment.Newline is transmuted to %r.
        /// </summary>
        /// <param name="codeLine">
        /// The line of text to process.
        /// </param>
        /// <returns>
        /// The processed text.
        /// </returns>
        string processASCIILine(string codeLine, int compileType)
        {
            string r = codeLine;
            r = r.Replace(@"\", @"\\"); // replace any \ characters in the ASCII art FIRST
            r = r.Replace(@"[", @"\["); // replace any parseable characters in the ASCII art with the \ escape character
            r = r.Replace(@"]", @"\]");
            r = r.Replace(@"(", @"\(");
            r = r.Replace(@")", @"\)");
            r = r.Replace(@"|", @"\|");
            r = r.Replace(@",", @".");  // commas will break anything like iter that ASCII art passes through
            r = r.Replace(@";", @":");  // semicolons will break literally anything that ASCII art passes through
            r = r.Replace(@" ", @"%b"); // replace any spaces with the %b

            if (compileType == Program.COMPILE_TYPE_ASCII_SINGLELINE)
            {
                // newlines don't translate when reading text line by line with StreamReader, so just tack %r onto every line processed
                // it's up to the consumer, after return, to tack the returned string onto a single line now
                r += @"%r"; 
            }
            // there is no else - it's up to the consumer, after return, to add the returned string into a new line
            
            return r;
        }
        /// <summary>
        /// Performs the compilation against ASCII artwork using a specified style.
        /// </summary>
        /// <param name="asciiInFile">
        /// The input file.
        /// </param>
        /// <param name="asciiOutFile">
        /// The output file.
        /// </param>
        /// <param name="compileType">
        /// The type of ASCII compile to perform.  
        /// 
        /// There are several different ways to input 
        /// ASCII art.  
        /// 
        /// Compiling for single-line input means compiling ASCII art to be stored in an object attribute on the MUSH (ie: in a function 
        /// or command) for future/repeat retrieval.  
        /// 
        /// Compiling for multi-line input means compiling ASCII art to be stored in a file-on-disk (ie: on your local hard drive) to be 
        /// uploaded through your client or copy and pasted via manual @emit for future/repeat retrieval.  
        /// 
        /// If your intention is to use ASCII art in a connect.txt or other "hard" file that the MUSH reads from disk and transmits to 
        /// users directly, DO NOT COMPILE IN ANY WAY, SHAPE, OR FORM!  Compiles perform replacements and cArr which you simply don't 
        /// want in this case.
        /// </param>
        internal void doCompile(string asciiInFile, string asciiOutFile, int compileType)
        {
            StreamReader inStream = new StreamReader(asciiInFile);
            StreamWriter outStream = new StreamWriter(asciiOutFile);

            do
            {
                string rawLine = inStream.ReadLine();
                rawLine = this.processASCIILine(rawLine, compileType);

                if (compileType == Program.COMPILE_TYPE_ASCII_SINGLELINE)
                {
                    // if single line mode, just write/append directly - there should never be a linebreak til the end of the file
                    outStream.Write(rawLine);
                }
                else if (compileType == Program.COMPILE_TYPE_ASCII_MULTILINE)
                {
                    // if multi line mode, write a single line instead
                    outStream.WriteLine(rawLine);
                }

                rawLine = null;
            } while (!inStream.EndOfStream);

            outStream.Close();
            outStream.Dispose();
            outStream = null;

            inStream.Close();
            inStream.Dispose();
            inStream = null;
            GC.Collect();
        }

        /// <summary>
        /// Constructor.  Initializes a new instance of the asciiCompilerClass.
        /// </summary>
        internal asciiCompilerClass()
        {
            
        }

        /// <summary>
        /// Destructor.  Deinitializes instance variables.
        /// </summary>
        ~asciiCompilerClass()
        {
            GC.Collect();
        }



        
    }
}
