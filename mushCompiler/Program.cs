using System;
using System.IO;

namespace mushCompiler
{
     class Program
     {
          static string infile = string.Empty;
          static string outfile = string.Empty;

          static bool isValidArgs(string[] args)
          {
               bool r = (args.Length == 3);
               if (!r)
               {
                    Console.WriteLine(@"Invalid arguments:  3 arguments required (" + args.Length.ToString() + @" arguments supplied)");
               }
               else
               {
                    r = (args[0].ToLower().Equals(@"/code") || args[0].ToLower().Equals(@"/ascii"));

                    if (!r)
                    {
                         Console.WriteLine(@"Invalid arguments:  First argument must be /code or /ascii");
                    }
                    else if (r && args[1].ToLower().StartsWith(@"-infile=") && args[2].ToLower().StartsWith(@"-outfile="))
                    {
                         // test if args0 after -infile= is an existing file
                         r = File.Exists(args[1].Substring(8));
                         if (!r)
                         {
                              Console.WriteLine(@"Invalid arguments:  -infile " + args[1].Substring(8) + @" is not a valid filename");
                         }
                         else
                         {
                              infile = args[1].Substring(8);
                              outfile = args[2].Substring(9); 
                         }
                    }
                    else if (r && args[2].ToLower().StartsWith(@"-infile=") && args[1].ToLower().StartsWith(@"-outfile="))
                    {
                         // test if args1 after -infile= is an existing file
                         r = File.Exists(args[3].Substring(8));
                         if (!r)
                         {
                              Console.WriteLine(@"Invalid arguments:  -infile " + args[3].Substring(8) + @" is not a valid filename");
                         }
                         else
                         {
                              infile = args[2].Substring(8);
                              outfile = args[1].Substring(9); 
                         }
                    }
                    else
                    {
                         Console.WriteLine(@"Invalid arguments:  One argument must be -infile=filename and one argument must be -outfile=filename");
                    }

                    
               }
               
               return r;
          }

          static void Main(string[] args)
          {

               if (isValidArgs(args))
               {
                    StreamReader sr = new StreamReader(infile);
                    mushCompilerClass mcc = new mushCompilerClass();

                    do
                    {
                         string rawCodeLine = sr.ReadLine();
                         if (args[0].ToLower().Equals(@"/code"))
                         {
                             try
                             {
                                 mcc.processCodeLine(rawCodeLine);
                             }
                             catch (Exception ex)
                             {
                                 Console.WriteLine(ex.Message);
                                 break;
                             }
                         }
                         else if (args[0].ToLower().Equals(@"/ascii"))
                         {
                             try
                             {
                                 mcc.processASCIILine(rawCodeLine);
                             }
                             catch (Exception ex)
                             {
                                 Console.WriteLine(ex.Message);
                                 break;
                             }
                         }
                    } while (!sr.EndOfStream);
                    sr.Close();
                    sr.Dispose();
                    sr = null;

                    string finalCode = mcc.CompiledCode;
                    if (args[0].ToLower().Equals(@"/ascii") && finalCode.EndsWith(@"%r"))
                    {
                         finalCode = finalCode.Substring(0, finalCode.Length - 2);
                    }

                    StreamWriter sw = new StreamWriter(outfile);
                    sw.Write(finalCode);

                    sw.Close();
                    sw.Dispose();
                    sw = null;
                    mcc = null;
               }
               
               GC.Collect();
               //Console.WriteLine(@"Press any key to continue");
               //Console.ReadLine();
          }
     }
}
