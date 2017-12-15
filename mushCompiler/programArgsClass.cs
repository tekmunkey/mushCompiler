using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace mushCompiler
{
    /// <summary>
    /// The programArgsClass represents arguments that may be passed into the program from the commandline.  
    /// 
    /// A program argument may be in the form of -argName=argValue or -argName="arg value" (where the value 
    /// contains spaces) or simply -argName
    /// </summary>
    class programArgsClass
    {
        /// <summary>
        /// An instance variable that stores the name of the arg, after processing.
        /// </summary>
        internal string name = string.Empty;
        /// <summary>
        /// An instance variable that stores the value of the arg, if one exists, after processing.
        /// </summary>
        internal string value = string.Empty;

        /// <summary>
        /// Constructor.  Initializes a programArgsClass instance with default/empty fields.
        /// </summary>
        internal programArgsClass()
        {
        }

        /// <summary>
        /// Constructor.  Initializes a programArgsClass from the specified string.  
        /// 
        /// A program argument may be in the form of -argName=argValue or -argName="arg value" (where the value 
        /// contains spaces) or simply -argName
        /// </summary>
        /// <param name="argString">
        /// A program argument may be in the form of -argName=argValue or -argName="arg value" (where the value 
        /// contains spaces) or simply -argName
        /// </param>
        internal programArgsClass(string argString)
        {
            string[] initArr = programArgsClass.initArgArray(argString);
            if (initArr.Length > 0)
            {
                this.name = initArr[0];
                if (initArr.Length > 1)
                {
                    this.value = initArr[1];
                }
            }

            initArr = null;
            GC.Collect();
        }

        /// <summary>
        /// Destructor.  Deinitializes instance variables.
        /// </summary>
        ~programArgsClass()
        {
            this.name = null;
            this.value = null;
            GC.Collect();
        }

        /// <summary>
        /// Gets a string array containing a single arg name and value pair, if a value was specified, according to 
        /// this implementation's argument format specification.  
        /// 
        /// A program argument may be in the form of -argName=argValue or -argName="arg value" (where the value 
        /// contains spaces) or simply -argName  
        /// 
        /// ** This function is for initializing individual argument strings.
        /// </summary>
        /// <param name="argString">
        /// A program argument may be in the form of -argName=argValue or -argName="arg value" (where the value 
        /// contains spaces) or simply -argName
        /// </param>
        /// <returns>
        /// A string array formed by splitting at the = sign.  Empty entries are removed.  This array may be 0, 
        /// 1, or 2 elements in length.  This array will never be null.
        /// </returns>
        private static string[] initArgArray(string argString)
        {
            string[] argDelim = new string[] { @"=" };

            string[] r = argString.Split(argDelim, 2, StringSplitOptions.RemoveEmptyEntries);
            if (object.ReferenceEquals(r,null))
            {
                r = new string[0];
            }

            argDelim = null;
            GC.Collect();

            return r;
        }

        /// <summary>
        /// Gets an individual programArgsClass instance from the specified string.  
        /// 
        /// A program argument may be in the form of -argName=argValue or -argName="arg value" (where the value 
        /// contains spaces) or simply -argName
        /// </summary>
        /// <param name="argString">
        /// A program argument may be in the form of -argName=argValue or -argName="arg value" (where the value 
        /// contains spaces) or simply -argName
        /// </param>
        /// <returns>
        /// A programArgsClass instance constructed from the specified argString, if the string is in a valid format.
        /// </returns>
        internal static programArgsClass getProgramArg(string argString)
        {
            programArgsClass r = new programArgsClass();

            string[] initArr = programArgsClass.initArgArray(argString);
            if (initArr.Length > 0)
            {
                r.name = initArr[0];
                if (initArr.Length > 1)
                {
                    r.value = initArr[1];
                }
            }
            
            initArr = null;
            GC.Collect();

            return r;
        }

        /// <summary>
        /// Creates a collection of programArgsClass instances from the specified array of arguments.  
        /// 
        /// Intended for use with the args string array passed in from the command line.
        /// </summary>
        /// <param name="cmdLineArgs">
        /// The string array passed in from the command line.
        /// </param>
        /// <returns>
        /// A collection of programArgsClass instances constructed from the input array of strings.
        /// </returns>
        internal static List<programArgsClass> getProgramArgsList(string[] cmdLineArgs)
        {
            List<programArgsClass> r = new List<programArgsClass>();

            if (!object.ReferenceEquals(cmdLineArgs, null))
            {
                for (int i = 0; i < cmdLineArgs.Length; i++)
                {
                    programArgsClass pac = programArgsClass.getProgramArg(cmdLineArgs[i]);
                    r.Add(pac);
                }
            }

            return r;
        }
    }
}
