using System;
using System.Collections.Generic;
using System.IO;

namespace mushCompiler
{
    class Program
    {
        internal const int COMPILE_TYPE_CODE  =            0x00000000;
        internal const int COMPILE_TYPE_DECOMPILE_CODE =   0x00000001;
        internal const int COMPILE_TYPE_ASCII_SINGLELINE = 0x00010000;
        internal const int COMPILE_TYPE_ASCII_MULTILINE =  0x00020000;

        

        /// <summary>
        /// Converts an integer value such as COMPILE_TYPE_CODE to a string value suitable for output.
        /// </summary>
        /// <param name="compileTypeConst">
        /// One of the const/enumerated COMPILE_TYPE_ type values.
        /// </param>
        /// <returns>
        /// A string value suitable for human-readable output.
        /// </returns>
        internal static string getCompileTypeString(int compileTypeConst)
        {
            string r = "Invalid Type Specifier";
            if (compileTypeConst == COMPILE_TYPE_CODE)
            {
                r = @"Compile MUSH Code";
            }
            else if (compileTypeConst == COMPILE_TYPE_DECOMPILE_CODE)
            {
                r = @"Decompile MUSH Code";
            }
            else if (compileTypeConst == COMPILE_TYPE_ASCII_SINGLELINE)
            {
                r = @"ASCII Single Line";
            }
            else if (compileTypeConst == COMPILE_TYPE_ASCII_MULTILINE)
            {
                r = @"ASCII Multi Line";
            }
            
            return r;
        }

        /// <summary>
        /// Contains a collection of command line arguments, after processing into our own variety.
        /// </summary>
        static List<programArgsClass> cmdLineArgs = null;

        /// <summary>
        /// Contains a default or user-defined compile type.
        /// </summary>
        static int compileType = COMPILE_TYPE_CODE;

        /// <summary>
        /// Contains the user-defined input file for this compile operation.
        /// </summary>
        static string infile = string.Empty;
        /// <summary>
        /// Contains the user-defined output file for this compile operation.
        /// </summary>
        static string outfile = string.Empty;

        /// <summary>
        /// Contains a value indicating how many spaces compiler output values should indent when sub-values provide output (tree branch 
        /// output desigation).
        /// </summary>
        static int outputIndent = 4;

        static bool isValidArgs(string[] args)
        {
            bool r = (cmdLineArgs.Count >= 2);
            if (!r)
            {
                writeOutput(@"Invalid arguments:  at least 2 arguments required (" + cmdLineArgs.Count.ToString() + @" arguments supplied)", 1);
            }
            else
            {
                for (int i = 0; i < cmdLineArgs.Count; i++)
                {
                    // a boolean indicating whether the commandline argument is valid
                    programArgsClass arg = cmdLineArgs[i];
                    string argName = arg.name.ToLower();
                    string argValue = arg.value;

                    if (argName.Equals(@"-compiletype") || argName.Equals(@"--compiletype") || argName.Equals(@"/compiletype"))
                    {
                        argValue = argValue.ToLower();
                        bool typeFound = false;
                        
                        if (argValue.Equals(@"code"))
                        {
                            compileType = COMPILE_TYPE_CODE;
                            typeFound = true;
                        }
                        else if (argValue.Equals(@"decompile") || argValue.Equals(@"decomp") || argValue.Equals(@"decompcode"))
                        {
                            compileType = COMPILE_TYPE_DECOMPILE_CODE;
                            typeFound = true;
                        }
                        else if (argValue.Equals(@"ascii") || argValue.Equals(@"ascii-single") || argValue.Equals(@"ascii-singleline"))
                        {
                            compileType = COMPILE_TYPE_ASCII_SINGLELINE;
                            typeFound = true;
                        }
                        else if (argValue.Equals(@"ascii-multi") || argValue.Equals(@"ascii-multiline"))
                        {
                            compileType = COMPILE_TYPE_ASCII_MULTILINE;
                            typeFound = true;
                        }
                        

                        if (!typeFound)
                        {
                            writeOutput(@"Invalid -compiletype argument:  " + arg.value, 1);
                            r = false;
                            break; // from commandline argument processing
                        }
                        
                        continue; // skip to next iteration if we haven't broken
                    }
                    else if (argName.Equals(@"-infile") || argName.Equals(@"--infile") || argName.Equals(@"/infile"))
                    {
                        r = File.Exists(arg.value);
                        if (!r)
                        {
                            writeOutput(@"Invalid infile argument:  " + arg.value + @" is not a valid filename",1);
                            break; // from commandline argument processing
                        }
                        else
                        {
                            infile = Path.GetFullPath(arg.value);
                            continue; // skip to next iteration if we haven't broken
                        }
                    }
                    else if (argName.Equals(@"-outfile") || argName.Equals(@"--outfile") || argName.Equals(@"/outfile"))
                    {

                        System.IO.FileInfo fi = null;
                        try
                        {
                            fi = new System.IO.FileInfo(arg.value);
                        }
                        catch (Exception) 
                        {
                        }
                        
                        if (object.ReferenceEquals(fi, null))
                        {
                            // file name is not valid
                            writeOutput(@"Invalid outfile argument:  " + arg.value + @" is not a good filename", 1);
                            break; // from commandline argument processing
                        }
                        else
                        {
                            // file name is valid... May check for existence by calling fi.Exists.
                            outfile = Path.GetFullPath(arg.value);
                            fi = null;
                            continue; // skip to next iteration if we haven't broken
                        }
                    }
                    else
                    {
                        r = false;
                        writeOutput(@"Invalid commandline argument:  " + arg.name, 1);
                        break; // from commandline argument processing
                    }

                    if (!r) { break; }
                }
            }
            return r;
        }

        static int Main(string[] args)
        {
            int r = int.MaxValue;

            cmdLineArgs = programArgsClass.getProgramArgsList(args);

            if (isValidArgs(args) && !string.IsNullOrEmpty(infile) && !string.IsNullOrEmpty(outfile))
            {
                r = 0;
                writeOutput(@"Beginning compile on:  " + infile, 0);

                if (compileType == COMPILE_TYPE_CODE)
                {
                    StreamReader sr = new StreamReader(infile);
                    mushCompilerClass_OLD mcc = new mushCompilerClass_OLD(infile);

                    do
                    {
                        string rawCodeLine = sr.ReadLine();
                        try
                        {
                            mcc.processCodeLine(rawCodeLine);
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine(ex.Message);
                            Console.WriteLine(ex.StackTrace);
                            r = 1;
                            break;
                        }

                    } while (!sr.EndOfStream);
                    sr.Close();
                    sr.Dispose();
                    sr = null;

                    string finalCode = mcc.CompiledCode;

                    StreamWriter sw = new StreamWriter(outfile);
                    try
                    {
                        sw.Write(finalCode);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex.Message);
                        Console.WriteLine(ex.StackTrace);
                        r = 1;
                    }
                    sw.Close();
                    sw.Dispose();
                    sw = null;

                    mcc = null;
                }
                else if (compileType == COMPILE_TYPE_DECOMPILE_CODE)
                {
                    StreamReader sr = new StreamReader(infile);
                    mushDecompilerClass mdc = new mushDecompilerClass(infile);

                    do
                    {
                        string rawCodeLine = sr.ReadLine();
                        try
                        {
                            mdc.processCodeLine(rawCodeLine);
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine(ex.Message);
                            Console.WriteLine(ex.StackTrace);
                            r = 1;
                            break;
                        }

                    } while (!sr.EndOfStream);
                    sr.Close();
                    sr.Dispose();
                    sr = null;

                    string finalCode = mdc.DecompiledCode;

                    StreamWriter sw = new StreamWriter(outfile);
                    try
                    {
                        sw.Write(finalCode);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex.Message);
                        Console.WriteLine(ex.StackTrace);
                        r = 1;
                    }
                    sw.Close();
                    sw.Dispose();
                    sw = null;

                    mdc = null;
                }
                else if (compileType > 0x0000ffff) // The low order 2-bytes are code options - everything greater is ASCII-something-something
                {
                    asciiCompilerClass acc = new asciiCompilerClass();
                    acc.doCompile(infile, outfile, compileType);
                    acc = null;
                }

                writeOutput(@"Finished compile on:   " + infile,0);
            }
            else
            {
                writeOutput(@"Invalid arguments supplied by user", 1);
            }

            GC.Collect();

            if (r != 0)
            {
                Console.WriteLine();
                Console.WriteLine(@"    Errors detected in compile.  Press any key to continue.");
                Console.ReadKey();
            }
            
            return r;
        }

        /// <summary>
        /// Provides a way to write output to the console in a standardized format.
        /// </summary>
        /// <param name="output">
        /// The output to print to the terminal or log.
        /// </param>
        /// <param name="indentdepth">
        /// Indicates how deep the caller is into the calling tree before calling writeOutput.  Determines how much indentation is 
        /// applied to the output.
        /// </param>
        internal static void writeOutput(string output, int indentdepth)
        {
            string finalOutput = new string(' ', (indentdepth * outputIndent));
            finalOutput += output;
            Console.WriteLine(finalOutput);
            finalOutput = null;
            GC.Collect();
        }
    }
}
