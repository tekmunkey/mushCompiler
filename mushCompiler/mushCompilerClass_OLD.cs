﻿using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.IO;

namespace mushCompiler
{
    /// <summary>
    /// Not really a compiler, more like a reformatter.  Allows programming and permanent storage of MUSH/MUX softcode in "format A" 
    /// which is an exploded format with plenty of linebreaks and tabstop/indents for sanity's sake and then when you're ready to 
    /// "compile" and install that in your game, you pass it through your MUSH Code Compiler which produces that godawful, mind-bending 
    /// spaghetti string we all know and love.  
    /// 
    /// The compiler removes line terminators and leading and trailing whitespace from individual lines, recognizing blocks based on the 
    /// format of the input file, and outputs one-line conglomerations suitable for MUSH platform consumers.
    /// </summary>
    public class mushCompilerClass_OLD
    {
        /// <summary>
        /// Contains the filename currently being compiled.
        /// </summary>
        string filePath = string.Empty;

        /// <summary>
        /// This field is where "compiled" code is constructed at runtime.
        /// </summary>
        private string compiledCodeString = string.Empty;

        public string CompiledCode
          {
               get
               {
                    return compiledCodeString;
               }
          }


        /// <summary>
        /// This list contains a collection of cVars defined throughout your code file.  You may define as many cVars as you like.
        /// </summary>
        List<compilerVariableClass> cvarsList = new List<compilerVariableClass>();

        /// <summary>
        /// This list contains a collection of bVars defined throughout a currently processing code block.  You may define as many bVars as you like.
        /// </summary>
        List<compilerVariableClass> bvarsList = new List<compilerVariableClass>();

        /// <summary>
        /// This list contains a collection of qVars defined throughout a currently processing code block.  You may define as many qVars as you like, 
        /// limited by your MUSH platform and generally configurable.
        /// </summary>
        List<compilerVariableClass> qvarsList = new List<compilerVariableClass>();

        /// <summary>
        /// This list contains a collection of params defined throughout a currently processing code block.  You may define as many params as you like, 
        /// limited by your MUSH platform and generally configurable.
        /// </summary>
        List<compilerVariableClass> paramsList = new List<compilerVariableClass>();

        /// <summary>
        /// This toggle value is only changed when the compiler encounters the BEGIN BLOCK or END BLOCK compiler directives, which manually dictate certain compiler behaviors with regard to 
        /// input file processing.
        /// </summary>
        private bool isInBlock = false;

        /// <summary>
        /// The standard MUSH comment format is @@ Remarks so I went ahead and built on that for my pre-processor commands.  Any compiler directives, 
        /// compiler-recognized comments and the like begin with @@.
        /// </summary>
        string commentOrDirectivePattern = @"^\s*?@@(.*)?$";

        /// <summary>
        /// This is the REGEX pattern used to identify compiler/preprocessor directives at runtime.  
        /// 
        /// @@ CDIR (directive)  
        /// 
        /// The @@ CDIR portion is always case-sensitive - @@ cdir will not be treated as a compiler directive by this implementation.  
        /// 
        /// There must always be a space preceding and following the term CDIR which enforces readability.
        /// </summary>
        private string compilerDirectivePattern = @"^\s*@@\s+CDIR\s+(.*)$";

        /// <summary>
        /// Includes are a single line of code in any file that will be replaced by the entire content of a specified filepath at compile time.  
        /// 
        /// @@ CDIR INC .\relative\path\to\file  
        /// @@ CDIR INCLUDE C:\explicit\path\to\file  
        /// 
        /// INC is provided simply as a shorthand for INCLUDE.  
        /// 
        /// The \ provided in examples is just whatever your platform's directory separator char is, if for example you could get this to work 
        /// on linux with the Mono framework.  
        /// 
        /// INC or INCLUDE is not case-sensitive.  Whether filepaths are case-sensitive or not depends on your particlar operating system.  
        /// 
        /// Leading and trailing spaces will be trimmed out of the path, so be careful when naming your include files.
        /// </summary>
        string includeDirectivePattern = @"^(?:INC|INCLUDE)\s+(.*)$";

        /// <summary>
        /// cVars are variables set into code files for replacement by the compiler.  cVar replacements are scoped at the entire file level.  
        /// 
        /// @@ CDIR CVAR varName=varValue  
        /// @@ CDIR CVAR varName=varValue  
        /// @@ CDIR cvar varName = varValue  
        /// @@ CDIR cvar varName=varValue  
        /// @@ CDIR cvar varName = varValue  
        /// 
        /// varName will be replaced with varValue throughout the variable's scope.  
        /// 
        /// Within the directive, the term CVAR is not case-sensitive.  Any name you give your variable will be, so if you name a cVar 'foo' then 'foo' will be matched but 'Foo' will not.  
        /// 
        /// Leading and trailing spaces will always be trimmed out of varName and varValue, so varName=varValue is exactly the same as varName = varValue
        /// </summary>
        string cvarsDirectivePattern = @"^CVAR\s+(.+)\s*=\s*(.+)\s*$";

        /// <summary>
        /// bVars are variables set into code files for replacement by the compiler.  bVar replacements are reset after processing each codeblock.  
        /// 
        /// @@ CDIR BVAR varName=varValue  
        /// @@ CDIR BVAR varName=varValue  
        /// @@ CDIR bvar varName = varValue  
        /// @@ CDIR bvar varName=varValue  
        /// @@ CDIR bvar varName = varValue  
        /// 
        /// varName will be replaced with varValue throughout the variable's scope.  
        /// 
        /// Within the directive, the term BVAR is not case-sensitive.  Any name you give your variable will be, so if you name a bVar 'bar' then 'bar' will be matched but 'Bar' will not.  
        /// 
        /// Leading and trailing spaces will always be trimmed out of varName and varValue, so varName=varValue is exactly the same as varName = varValue
        /// </summary>
        string bvarsDirectivePattern = @"^BVAR\s+(.+)\s*=\s*(.+)\s*$";

        /// <summary>
        /// Parameters are variables passed into functions or commands by user input.  Parameter replacements are reset after processing each codeblock.  
        /// 
        /// Parameter names found in code blocks are replaced with their numeric position in the parameter list by the compiler.  
        /// 
        /// @@ CDIR PARAMETERS(thisparam, thatparam, anotherparam) -> %0, %1, %2
        /// @@ CDIR parameters (thisparam, thatparam, anotherparam) -> %0, %1, %2
        /// @@ CDIR params(foo, bar, baz) -> %0, %1, %2
        /// @@ CDIR params (foo, bar, baz) -> %0, %1, %2
        /// @@ CDIR params () ->
        /// @@ CDIR safeparameters ( someparam, otherparam ) -> objeval( %#, %0, %1 )
        /// @@ CDIR SAFEPARAMS ( foo, bar, baz ) -> objeval( %#, %0, %1, %2 )
        /// 
        /// The content between the parentheses is returned as regex group 2 if it exists.  You aren't required to declare params and if you declare it with no parameters 
        /// there's no harm done.
        /// 
        /// The term params shall not be considered case-sensitive but the individual param IDs (inside parentheses) shall be
        /// 
        /// Within the directive, the term PARAMS is shorthand for PARAMETERS and neither is case-sensitive.  Any name you give your variables for replacement will be, so if you 
        /// name a param 'baz' then 'baz' will be matched but 'Baz' will not.  
        /// 
        /// Leading and trailing spaces will always be trimmed out so params (foo,bar,baz) is exactly the same as params(foo, bar, baz) and params ( foo , bar , baz )
        /// </summary>
        string paramsDirectivePattern = @"^(PARAMETERS|PARAMS|SAFEPARAMETERS|SAFEPARAMS)\s*\((.*?)\)$";

        /// <summary>
        /// qVars are variables set in MUSH code by setq() and setr() calls.  qVar replacements are reset after processing each codeblock.  
        /// 
        /// qVar names found in code blocks are replaced with their numeric position in the parameter list by the compiler.  
        /// 
        /// While declaring qVars for replacements makes your life easier, you still have to remember to actually make the calls to setq/setr in 
        /// your code, and when you do you have to get the numbers right!
        /// 
        /// @@ CDIR QVARS(thisQ,thatQ,anotherQ) -> %q0, %q1, %q2
        /// @@ CDIR QVARS (thisQ, thatQ, anotherQ) -> %q0, %q1, %q2
        /// @@ CDIR qvars (thisQ, thatQ, anotherQ) -> %0, %1, %2
        /// @@ CDIR qvars(foo, bar, baz) -> %q0, %q1, %q2
        /// @@ CDIR qvars (foo, bar, baz) -> %q0, %q1, %q2
        /// @@ CDIR qvars ( ,,,,,,,,,,baz) -> ,,,,,,,,,,%qA
        /// @@ CDIR qvars ( ,,,foo,,,,,,,,,,,,,,baz) -> ,,,%q3,,,,,,,,,,,,,,%qH
        /// @@ CDIR qvars () ->
        /// @@
        /// @@   * in your code you still need to: [setq(0,gleep)][setq(1,glomp)][setq(2,glerp)][setr(A,womp)][setq(H,wakka)]
        /// 
        /// The content between the parentheses is returned as regex group 1 if it exists.  You aren't required to declare qVars even if you use setq/setr and if you declare 
        /// it with no parameters there's no harm done.
        /// 
        /// The term qvars shall not be considered case-sensitive but the individual qVar IDs (inside parentheses) shall be.  
        /// 
        /// Within the directive, the term QVARS is not case-sensitive.  Any name you give your variables for replacement will be, so if you 
        /// name a qVar 'thisQ' then 'thisQ' will be matched but 'Thisq' will not.  
        /// 
        /// Leading and trailing spaces will always be trimmed out so qvars (foo,bar,baz) is exactly the same as qvars(foo, bar, baz) and qvars ( foo , bar , baz )
        /// </summary>
        string qvarsDirectivePattern = @"^QVARS\s*\((.*?)\)$";

        /// <summary>
        /// The important concept to grasp about a code block is that from the BEGINNING OF BLOCK to the END OF BLOCK, the compiler will remove all linebreaks and trim all leading and 
        /// trailing whitespace from lines not explicitly excepted in the needsTrailingWhitespace definition.  When you define a code block in your input file, you're advising the compiler 
        /// that all the individual codelines from the BEGIN BLOCK directive until the END BLOCK directive should be concatenated into a single cohesive line of MUSH output code.  
        /// 
        /// @@ CDIR BEGIN BLOCK  
        /// @@ CDIR BEGINBLOCK
        /// 
        /// The BEGIN BLOCK or BEGINBLOCK directive is case sensitive.  
        /// 
        /// Use of BEGIN BLOCK is totally optional.  There is no need to tell the compiler where a codeblock begins, as long as the coder is careful to ensure that at some point where they 
        /// intend for the previous block to end, they actually leave an empty line with nothing but a line terminator (just hit enter a couple of times).  If BEGIN BLOCK is ever actually 
        /// used, however, the use of END BLOCK is absolutely necessary.  Subsequent use of BEGIN BLOCK won't help you.  The new code will just keep being concatenated onto the last block 
        /// during compilation.  
        /// 
        /// ** Whether you use BEGIN BLOCK directives or not, you may optionally employ an END BLOCK directive where you would otherwise just hit enter a few times, to improve readability 
        ///    in your source files and also to ensure that you don't inadvertently leave empty whitespace that results in a bad compile.  
        /// 
        /// There are 2 types of codelines in play:  A line of code-to-compile (exploded input) and a line of compiled-code (concatenated output compiled from multiple lines of input).  An 
        /// individual input line may or may not represent a block or part of a block, code or comment, directive or whatever.  So the compiler relies on a certain amount of strict adherence 
        /// to programmer attention to detail in their input files:  
        /// 
        /// With no prompting or compiler directives at all:  
        /// 1:  An input line with any text in it at all (including any kind of whitespace other than line terminators) is treated as a codeline and processed/tested for code type.  
        /// 2:  If there was a line containing text (including empty whitespace) before it, it is a CONTINUATION OF BLOCK.  If not, it is a BEGINNING OF BLOCK.  
        ///     * In neither case is the compiler's explicit isInBlock value affected.  
        /// 3:  An empty line containing nothing but a line terminator is considered to be END OF BLOCK.  
        /// 
        /// When the compiler encounters a BEGIN BLOCK directive:  
        /// 1:  The compiler's explicit isInBlock value is toggled to true.  
        /// 2:  While the compiler's explicit isInBlock value continues to be true, empty lines containing nothing but line terminators are no longer considered to be END OF BLOCK - they will 
        ///     be treated as CONTINUATION OF BLOCK until an explicit compiler directive END BLOCK directive is encountered.  
        /// 3:  Only the explicit END BLOCK directive toggles the compiler's isInBlock value to false, thus ending concatenation of codelines within the file at that particular time.  
        /// </summary>
        string beginBlockDirectivePattern = @"^BEGIN\s*BLOCK\s*$";

        /// <summary>
        /// The important concept to grasp about a code block is that from the BEGINNING OF BLOCK to the END OF BLOCK, the compiler will remove all linebreaks and trim all leading and 
        /// trailing whitespace from lines not explicitly excepted in the needsTrailingWhitespace definition.  When you define a code block in your input file, you're advising the compiler 
        /// that all the individual codelines from the BEGIN BLOCK directive until the END BLOCK directive should be concatenated into a single cohesive line of MUSH output code.  
        /// 
        /// @@ CDIR END BLOCK  
        /// @@ CDIR ENDBLOCK
        /// 
        /// The END BLOCK or ENDBLOCK directive is case sensitive.  
        /// 
        /// Use of BEGIN BLOCK is totally optional.  There is no need to tell the compiler where a codeblock begins, as long as the coder is careful to ensure that at some point where they 
        /// intend for the previous block to end, they actually leave an empty line with nothing but a line terminator (just hit enter a couple of times).  If BEGIN BLOCK is ever actually 
        /// used, however, the use of END BLOCK is absolutely necessary.  Subsequent use of BEGIN BLOCK won't help you.  The new code will just keep being concatenated onto the last block 
        /// during compilation.  
        /// 
        /// ** Whether you use BEGIN BLOCK directives or not, you may optionally employ an END BLOCK directive where you would otherwise just hit enter a few times, to improve readability 
        ///    in your source files and also to ensure that you don't inadvertently leave empty whitespace that results in a bad compile.  
        /// 
        /// There are 2 types of codelines in play:  A line of code-to-compile (exploded input) and a line of compiled-code (concatenated output compiled from multiple lines of input).  An 
        /// individual input line may or may not represent a block or part of a block, code or comment, directive or whatever.  So the compiler relies on a certain amount of strict adherence 
        /// to programmer attention to detail in their input files:  
        /// 
        /// With no prompting or compiler directives at all:  
        /// 1:  An input line with any text in it at all (including any kind of whitespace other than line terminators) is treated as a codeline and processed/tested for code type.  
        /// 2:  If there was a line containing text (including empty whitespace) before it, it is a CONTINUATION OF BLOCK.  If not, it is a BEGINNING OF BLOCK.  
        ///     * In neither case is the compiler's explicit isInBlock value affected.  
        /// 3:  An empty line containing nothing but a line terminator is considered to be END OF BLOCK.  
        /// 
        /// When the compiler encounters a BEGIN BLOCK directive:  
        /// 1:  The compiler's explicit isInBlock value is toggled to true.  
        /// 2:  While the compiler's explicit isInBlock value continues to be true, empty lines containing nothing but line terminators are no longer considered to be END OF BLOCK - they will 
        ///     be treated as CONTINUATION OF BLOCK until an explicit compiler directive END BLOCK directive is encountered.  
        /// 3:  Only the explicit END BLOCK directive toggles the compiler's isInBlock value to false, thus ending concatenation of codelines within the file at that particular time.  
        /// </summary>
        string endBlockDirectivePattern = @"^END\s*BLOCK\s*$";

        /// <summary>
        /// Comment Directives pass through the compiler to the output file.  
        /// 
        /// Anything following this particular compiler directive pattern is passed directly into the output string in the format of a standard MUSH comment string.  
        /// 
        /// @@@@ Some remarks here -> @@ Some remarks here
        /// @@ @@ Some remarks here -> @@ Some remarks here
        /// @@ CMT Some remarks here -> @@ Some remarks here
        /// @@ CMTS Some remarks here -> @@ Some remarks here
        /// @@ COMMENT Some remarks here -> @@ Some remarks here
        /// @@ COMMENTS Some remarks here -> @@ Some remarks here
        /// @@ cmt Some remarks here ->
        /// @@ comments Some remarks here ->
        /// @@ Some remarks here -> 
        /// 
        /// If you choose to use one of the word-based directives then it is case-sensitive.  
        /// 
        /// This is a compiler/preprocessor directive which processes the supplied comment out to a straight MUSH-style comment.  The purpose is to allow the MUSH coder to 
        /// insert some comments into MUSH code as line-items which the compiler actually removes at compile-time and some other comments which the compiler reduces to comments 
        /// in a distributable codefile.  
        /// </summary>
        string commentDirectivePattern = @"^\s*(@@|CMT|CMTS|COMMENT|COMMENTS)\s*(\S+.*)?$";

        /// <summary>
        /// Null Directives are transmuted into literal @@ characters into the output file with all whitespace around them trimmed out and with no leading or trailing linebreaks.  
        /// 
        /// This compiler/preprocessor directive was added to support TinyMUX' "Null Delimiter" (which you would pass into iter() for example if you wanted output to be run together, 
        /// with no spaces or pipes or whatever), which is in fact @@.  Without the ability to inject this @@ into the output file without additional formatting, TinyMUX softcoders 
        /// would not be able to make use of this handy-dandy feature!
        /// </summary>
        string nullDirectivePattern = @"^NULL\s*?$";

        string leadingSpacePattern = @"^\s+(.*?)$";

        /// <summary>
        /// Returns a boolean value indicating whether the provided text string is in fact a compiler directive.
        /// </summary>
        /// <param name="codeLine">
        /// A single line of exploded MUSH code read from a text file.
        /// </param>
        /// <returns>
        /// A boolean value.  True if the string matches the compilerDirectivePattern through case-sensitive System.RegularExpressions matching.  Otherwise False.
        /// </returns>
        bool isCompilerDirective(string codeLine)
        {
            // With default options selected, this is a case-sensitive match determining 
            return Regex.IsMatch(codeLine, compilerDirectivePattern);
        }

        /// <summary>
        /// Processes include directives codelines.
        /// </summary>
        /// <param name="codeLine">
        /// A single line of exploded MUSH code read from a text file.
        /// </param>
        /// <returns>
        /// A boolean value.  True if the string was processed as an Include Directive.  Otherwise False.
        /// </returns>
        bool processIncludeDirective(string codeLine)
        {
            bool r = false;
            // Testing if this is even a compiler directive - yes this is redundant after processCompilerDirective - no, I don't care - the function may be called directly by some other code at some other time
            if (isCompilerDirective(codeLine))
            {
                // We have found a compiler directive, but what kind? The next line of code returns the portion representing the directive itself (after the CDIR)
                string compilerDirective = Regex.Match(codeLine, compilerDirectivePattern, RegexOptions.IgnoreCase).Groups[1].Value;
                // If this is in fact an include directive, then the next line sets our return value to true (or remains false)
                r = Regex.IsMatch(compilerDirective, includeDirectivePattern, RegexOptions.IgnoreCase);
                if (r)
                {
                    // We have found an include directive, so the next line of code extracts the filename to include from the directive
                    string incFile = Regex.Match(compilerDirective, includeDirectivePattern, RegexOptions.IgnoreCase).Groups[1].Value.Trim();
                    //
                    // Allow the use of cVars in include file paths
                    //
                    incFile = this.replaceCVars(incFile);

                    //
                    // Issue:     neither DotNET or mushCompiler automagically track "working directory" into subdirectory changes during include operations (for the 
                    //            organizationally compulsive among us, which is every coder of any merit at all).
                    // Solution:  Store the filename on each mushCompiler class, and if/when another include operation is called with a non-rooted Path (which must be 
                    //            relative to the currently processing file path), convert that relative path by prepending the current instance's directory path, 
                    //            concatenating the new directory path, then concatenating the new filename
                    //
                    if (!Path.IsPathRooted(incFile))
                    {
                        // And of course there's a platform-specific directory separator characters - screw DotNET and Windows and Linux - allow \ or / regardless
                        char[] dirSC = new char[] { (char)0x2f, (char)0x5c };

                        // The parent filepath is the directory name of the current mushCompilerClass instance, if any
                        string parFP = Path.GetDirectoryName(this.filePath);
                        // System.IO.Path doesn't provide any functions that do this AT ALL so we must account for usage of ./ and ../ ourselves
                        string[] parFPArr = parFP.Split(dirSC, StringSplitOptions.RemoveEmptyEntries);

                        // The include filepath is the relative path from the parent filepath, if any
                        string incFP = Path.GetDirectoryName(incFile);
                        string[] incFPArr = incFP.Split(dirSC, StringSplitOptions.RemoveEmptyEntries);

                        // Working FORWARD through incFP and BACKWARD through parFP
                        //   * This represents the format of ../../filename.msh
                        //     + Where the first .. means step backward 1 directory, the second .. means step backward another directory
                        for (int i = 0; i < incFPArr.Length; i++)
                        {
                            if (parFPArr.Length > 0)
                            {
                                if (incFPArr[i].Equals(@".."))
                                {
                                    //
                                    // The include filepath segment here is '..'
                                    //
                                    // If there are any items left in parFPArr at all, simply drop the last element from parFPArr
                                    Array.Resize<string>(ref parFPArr, parFPArr.Length - 1);
                                }
                                else if (incFPArr[i].Equals(@"."))
                                {
                                    // 
                                    // The include filepath segment here is '.'
                                    //   * Simply ignore this - the '.' means THIS DIRECTORY RIGHT HERE
                                    //
                                }
                                else
                                {
                                    //
                                    // Anything else at all is either '.' or is a directory name - it wouldn't be a file name because of the calls to Path.GetDirectoryName
                                    //
                                    Array.Resize<string>(ref parFPArr, parFPArr.Length + 1);
                                    parFPArr[parFPArr.Length - 1] = incFPArr[i];
                                }
                            }
                        }

                        string newFQP = string.Empty;
                        // This loop should never error because parFPArr would never be null, but may be of length 0, in which case this loop would simply 
                        // do nothing
                        for (int i = 0; i < parFPArr.Length; i++)
                        {
                            newFQP += parFPArr[i] + Path.DirectorySeparatorChar;
                        }
                        newFQP += Path.GetFileName(incFile);

                        incFile = newFQP;

                        newFQP = null;

                        incFPArr = null;
                        incFP = null;

                        parFPArr = null;
                        parFP = null;
                        
                        dirSC = null;
                    }

                    if (!System.IO.File.Exists(incFile))
                    {
                        throw new System.IO.FileNotFoundException(@"    Invalid include file: " + incFile);
                    }
                    
                    // Reading in the file means compiling it independently and then copying its compiled/finished product 
                    // into ours
                    System.IO.StreamReader sr = new System.IO.StreamReader(incFile);
                    mushCompilerClass_OLD mcc = new mushCompilerClass_OLD(incFile, this.cvarsList);
                    do
                    {
                        string rawCodeLine = sr.ReadLine();
                        mcc.processCodeLine(rawCodeLine);
                    } while (!sr.EndOfStream);
                    sr.Close();
                    sr.Dispose();
                    sr = null;
                    // add an extra carriage return to compiledCodeString - otherwise if there are 2 includes in a row there's trouble!
                    compiledCodeString += mcc.CompiledCode + System.Environment.NewLine;
                    mcc = null;
               }
            }

            GC.Collect();
            return r;
        }

        /// <summary>
        /// Processes cVar directives codelines.
        /// </summary>
        /// <param name="codeLine">
        /// A single line of exploded MUSH code read from a text file.
        /// </param>
        /// <returns>
        /// A boolean value.  True if the string was processed as a cVar Directive.  Otherwise False.
        /// </returns>
        /// <remarks>
        /// The difference between cVars/bVars and parameters/qVars is that cVars/bVars provide a convention for creating real variables while parameters/qVars provide a convention for 
        /// MUSH coders to name MUSH-native code registers.  
        /// 
        /// params for example allow for naming %0-%9, qVars for naming %q0-%qZ - you simply define a qVar for %q1 named "myMnemonicDeviceFor%q1" and then anywhere you plug 
        /// "myMnemonicDeviceFor%q1" into your codefile and the compiler replaces that with %q1.  You define the qVar just before you begin writing an attribute definition, and the compiler 
        /// automatically clears it at the first linebreak with no empty whitespace or other characters on that line (or @@ CDIR END BLOCK).
        /// 
        /// By contrast, you can define a cVar or bVar completely unrelated to user input or MUSH variables but perhaps injecting a function call or dbref you find yourself commonly looking up 
        /// or cut and pasting from some other source.
        /// </remarks>
        bool processCvarsDirective(string codeLine)
        {
            bool r = false;
            // Testing if this is even a compiler directive - yes this is redundant after processCompilerDirective - no, I don't care - the function may be called directly by some other code at some other time
            if (isCompilerDirective(codeLine))
            {
                // We have found a compiler directive, but what kind? The next line of code returns the portion representing the directive itself (after the CDIR)
                string compilerDirective = Regex.Match(codeLine, compilerDirectivePattern, RegexOptions.None).Groups[1].Value;
                // If this is in fact a cVar directive, then the next line sets our return value to True.  Otherwise it is set to false (or remains false)
                r = Regex.IsMatch(compilerDirective, cvarsDirectivePattern, RegexOptions.IgnoreCase);
                if (r)
                {
                    if (object.ReferenceEquals(this.cvarsList, null))
                    {
                        this.cvarsList = new List<compilerVariableClass>();
                    }
                    // pass through the existing cvars to ensure we're not doubling up an existing cvar
                    compilerVariableClass cv = compilerVariableClass.getCompilerVariable(compilerDirective.Substring(5));
                    for (int i = 0; i < this.cvarsList.Count; i++)
                    {
                        // testing if named var already exists, and removing it if so
                        if (cvarsList[i].name.Equals(cv.name)) { cvarsList.RemoveAt(i); }
                    }
                    // don't even try an if/else addition in the loop above - it gets too complex with multiple possible cVar declarations per file
                    this.cvarsList.Add(cv);
                    //
                    // ISSUE:
                    //        When processing cVar/bVar directives, we sometimes need to include other c/b variable values into a macro-style definition.
                    // SOLUTION:
                    //        cVars must evaluate all other (previously existing) cVars, and bVars must evaluate all previously existing cVars and also 
                    //        bVars
                    cv.value = replaceCVars(cv.value);
                    //
                    // ISSUE:  
                    //        When a user declares similar variable names such as cvText and cvTextAlign, with the shorter version declared earlier, 
                    //        replacement overwrites part of cvTextAlign.  ie:  if the value of cvText is %0 then occurrences of cvTextAlign become 
                    //        %0Align before cvTextAlign replacement can occur
                    // SOLUTION:
                    //        Sort items by name length so that the longest items process/replace FIRST, forcing the "most complete match" to replace 
                    //        ahead of any partial matches.
                    //  
                    this.cvarsList.Sort((x, y) => x.name.Length.CompareTo(y.name.Length));
                    this.cvarsList.Reverse();
                }
            }
            return r;
        }

        /// <summary>
        /// Processes bVar directives codelines.
        /// </summary>
        /// <param name="codeLine">
        /// A single line of exploded MUSH code read from a text file.
        /// </param>
        /// <returns>
        /// A boolean value.  True if the string was processed as a bVar Directive.  Otherwise False.
        /// </returns>
        /// <remarks>
        /// The difference between cVars/bVars and parameters/qVars is that cVars/bVars provide a convention for creating real variables while parameters/qVars provide a convention for 
        /// MUSH coders to name MUSH-native code registers.  
        /// 
        /// params for example allow for naming %0-%9, qVars for naming %q0-%qZ - you simply define a qVar for %q1 named "myMnemonicDeviceFor%q1" and then anywhere you plug 
        /// "myMnemonicDeviceFor%q1" into your codefile and the compiler replaces that with %q1.  You define the qVar just before you begin writing an attribute definition, and the compiler 
        /// automatically clears it at the first linebreak with no empty whitespace or other characters on that line (or @@ CDIR END BLOCK).
        /// 
        /// By contrast, you can define a cVar or bVar completely unrelated to user input or MUSH variables but perhaps injecting a function call or dbref you find yourself commonly looking up 
        /// or cut and pasting from some other source.
        /// </remarks>
        bool processBvarsDirective(string codeLine)
        {
            bool r = false;
            // Testing if this is even a compiler directive - yes this is redundant after processCompilerDirective - no, I don't care - the function may be called directly by some other code at some other time
            if (isCompilerDirective(codeLine))
            {
                // We have found a compiler directive, but what kind? The next line of code returns the portion representing the directive itself (after the CDIR)
                string compilerDirective = Regex.Match(codeLine, compilerDirectivePattern, RegexOptions.None).Groups[1].Value;
                // If this is in fact a bVar directive, then the next line sets our return value to True.  Otherwise it is set to false (or remains false)
                r = Regex.IsMatch(compilerDirective, bvarsDirectivePattern, RegexOptions.IgnoreCase);
                if (r)
                {
                    if (object.ReferenceEquals(this.bvarsList, null))
                    {
                        this.bvarsList = new List<compilerVariableClass>();
                    }
                    // pass through the existing bvars to ensure we're not doubling up an existing bvar
                    compilerVariableClass bv = compilerVariableClass.getCompilerVariable(compilerDirective.Substring(5));
                    for (int i = 0; i < this.bvarsList.Count; i++)
                    {
                        // testing if named var already exists, and removing it if so
                        if (bvarsList[i].name.Equals(bv.name)) { bvarsList.RemoveAt(i); }
                    }
                    // don't even try an if/else addition in the loop above - it gets too complex with multiple possible bVar declarations per codeblock
                    this.bvarsList.Add(bv);
                    //
                    // ISSUE:
                    //        When processing cVar/bVar directives, we sometimes need to include other c/b variable values into a macro-style definition.
                    // SOLUTION:
                    //        cVars must evaluate all other (previously existing) cVars, and bVars must evaluate all previously existing cVars and also 
                    //        bVars
                    bv.value = replaceCVars(bv.value);
                    bv.value = replaceParams(bv.value);
                    bv.value = replaceQVars(bv.value);
                    bv.value = replaceBVars(bv.value);
                    //
                    // ISSUE:  
                    //        When a user declares similar variable names such as bvText and bvTextAlign, with the shorter version declared earlier, 
                    //        replacement overwrites part of bvTextAlign.  ie:  if the value of bvText is %0 then occurrences of bvTextAlign become 
                    //        %0Align before bvTextAlign replacement can occur
                    // SOLUTION:
                    //        Sort items by name length so that the longest items process/replace FIRST, forcing the "most complete match" to replace 
                    //        ahead of any partial matches.
                    //  
                    this.bvarsList.Sort((x, y) => x.name.Length.CompareTo(y.name.Length));
                    this.bvarsList.Reverse();
                }
            }
            return r;
        }

        /// <summary>
        /// Processes Parameters directives codelines.
        /// </summary>
        /// <param name="codeLine">
        /// A single line of exploded MUSH code read from a text file.
        /// </param>
        /// <returns>
        /// A boolean value.  True if the string was processed as a Parameters Directive.  Otherwise False.
        /// </returns>
        /// <remarks>
        /// The difference between cVars/bVars and parameters/qVars is that cVars/bVars provide a convention for creating real variables while parameters/qVars provide a convention for 
        /// MUSH coders to name MUSH-native code registers.  
        /// 
        /// params for example allow for naming %0-%9, qVars for naming %q0-%qZ - you simply define a qVar for %q1 named "myMnemonicDeviceFor%q1" and then anywhere you plug 
        /// "myMnemonicDeviceFor%q1" into your codefile and the compiler replaces that with %q1.  You define the qVar just before you begin writing an attribute definition, and the compiler 
        /// automatically clears it at the first linebreak with no empty whitespace or other characters on that line (or @@ CDIR END BLOCK).
        /// 
        /// By contrast, you can define a cVar or bVar completely unrelated to user input or MUSH variables but perhaps injecting a function call or dbref you find yourself commonly looking up 
        /// or cut and pasting from some other source.
        /// </remarks>
        bool processParamsDirective(string codeLine)
        {
            bool r = false;
            // Testing if this is even a compiler directive - yes this is redundant after processCompilerDirective - no, I don't care - the function may be called directly by some other code at some other time
            if (isCompilerDirective(codeLine))
            {
                // We have found a compiler directive, but what kind? The next line of code returns the portion representing the directive itself (after the CDIR)
                string compilerDirective = Regex.Match(codeLine, compilerDirectivePattern, RegexOptions.None).Groups[1].Value.Trim();
                // If this is in fact a params directive, then the next line sets our return value to True.  Otherwise it is set to false (or remains false)
                r = Regex.IsMatch(compilerDirective, paramsDirectivePattern, RegexOptions.IgnoreCase);
                if (r)
                {
                    // paramsType will be relevant in a little moment
                    string paramsType = Regex.Match(compilerDirective, paramsDirectivePattern, RegexOptions.IgnoreCase).Groups[1].Value.Trim();

                    if (object.ReferenceEquals(this.paramsList, null))
                    {
                        this.paramsList = new List<compilerVariableClass>();
                    }
                    // no else - just clear paramsList in all events - this way if a softcoder has a brainfart and double-declares params we don't let them shoot their own foot
                    this.paramsList.Clear();
                    // Since we matched a paramsDirective regex pattern, setting up the paramsArray is really easy
                    // The next line of code simply rips the comma-delimited parameter names apart into an array
                    string[] paramsArray = Regex.Match(compilerDirective, paramsDirectivePattern, RegexOptions.IgnoreCase).Groups[2].Value.Trim().Split(new string[] { @"," }, StringSplitOptions.RemoveEmptyEntries);
                    // If the user didn't try to trick us by passing an empty parameters field, tidy up the whitespace around each parameter name
                    if (!object.ReferenceEquals(paramsArray, null) && (paramsArray.Length > 0))
                    {
                        for (int i = 0; i < paramsArray.Length; i++)
                        {
                            this.paramsList.Add(compilerVariableClass.getCompilerVariable(paramsArray[i].Trim(),@"%" + i.ToString()));
                            if (!string.IsNullOrEmpty(paramsType) && (paramsType.ToUpper().Equals(@"SAFEPARAMETERS") || paramsType.ToUpper().Equals(@"SAFEPARAMS")))
                            {
                                this.paramsList[this.paramsList.Count - 1].defaultSafe = true;
                            }
                        }
                        //
                        // ISSUE:  
                        //        When a user declares similar variable names such as bvText and bvTextAlign, with the shorter version declared earlier, 
                        //        replacement overwrites part of bvTextAlign.  ie:  if the value of bvText is %0 then occurrences of bvTextAlign become 
                        //        %0Align before bvTextAlign replacement can occur
                        // SOLUTION:
                        //        Sort items by name length so that the longest items process/replace FIRST, forcing the "most complete match" to replace 
                        //        ahead of any partial matches.
                        //  
                        this.paramsList.Sort((x, y) => x.name.Length.CompareTo(y.name.Length));
                        this.paramsList.Reverse();
                    }

                    paramsType = null;
                    GC.Collect();

                    // Finally we inject a params comment into the compiled codeline, following the MUSH standard @@ 
                    compiledCodeString += @"@@ " + compilerDirective + System.Environment.NewLine;
                }
            }
            return r;
        }

        /// <summary>
        /// Processes qVars directives codelines.
        /// </summary>
        /// <param name="codeLine">
        /// A single line of exploded MUSH code read from a text file.
        /// </param>
        /// <returns>
        /// A boolean value.  True if the string was processed as a qVars Directive.  Otherwise False.
        /// </returns>
        /// <remarks>
        /// The difference between cVars/bVars and parameters/qVars is that cVars/bVars provide a convention for creating real variables while parameters/qVars provide a convention for 
        /// MUSH coders to name MUSH-native code registers.  
        /// 
        /// params for example allow for naming %0-%9, qVars for naming %q0-%qZ - you simply define a qVar for %q1 named "myMnemonicDeviceFor%q1" and then anywhere you plug 
        /// "myMnemonicDeviceFor%q1" into your codefile and the compiler replaces that with %q1.  You define the qVar just before you begin writing an attribute definition, and the compiler 
        /// automatically clears it at the first linebreak with no empty whitespace or other characters on that line (or @@ CDIR END BLOCK).
        /// 
        /// By contrast, you can define a cVar or bVar completely unrelated to user input or MUSH variables but perhaps injecting a function call or dbref you find yourself commonly looking up 
        /// or cut and pasting from some other source.
        /// </remarks>
        bool processQVarDirective(string codeLine)
        {
            bool r = false;
            // Testing if this is even a compiler directive - yes this is redundant after processCompilerDirective - no, I don't care - the function may be called directly by some other code at some other time
            if (isCompilerDirective(codeLine))
            {
                // We have found a compiler directive, but what kind? The next line of code returns the portion representing the directive itself (after the CDIR)
                string compilerDirective = Regex.Match(codeLine, compilerDirectivePattern, RegexOptions.None).Groups[1].Value.Trim();
                // If this is in fact a params directive, then the next line sets our return value to True.  Otherwise it is set to false (or remains false)
                r = Regex.IsMatch(compilerDirective, qvarsDirectivePattern, RegexOptions.IgnoreCase);
                if (r)
                {
                    if (object.ReferenceEquals(this.qvarsList, null))
                    {
                        this.qvarsList = new List<compilerVariableClass>();
                    }
                    //
                    // no else - never clear qvarsList except at End of Block - this way if a softcoder wants to redeclare qVars or add declarations repeatedly, they can
                    //
                    // Since we matched a qVarsDirective regex pattern, setting up the qVarsArray is really easy
                    // The next line of code simply rips the comma-delimited parameter names apart into an array
                    // Do not use StringSplitOptions.RemoveEmptyEntries - this eliminates the ability to use %q9 without using %q0-%q8
                    string[] qvarsArray = Regex.Match(compilerDirective, qvarsDirectivePattern, RegexOptions.IgnoreCase).Groups[1].Value.Trim().Split(new string[] { @"," }, StringSplitOptions.None);
                    // If the user didn't try to trick us by passing an empty qVars field, tidy up the whitespace around each qVar name
                    if (!object.ReferenceEquals(qvarsArray, null) && (qvarsArray.Length > 0))
                    {
                        for (int i = 0; i < qvarsArray.Length; i++)
                        {
                            string qStr = i.ToString();
                            // if i > 9 then convert to A-Z by ascii morphic
                            if (i > 9) { qStr = ((char)(i + 55)).ToString(); };
                            qStr = qStr.Trim();
                            // must test each member of the existing qVars list for a pre-existing %qreg
                            for (int i2 = 0; i2 < qvarsList.Count; i2++)
                            {
                                // testing if the new %qreg number/letter already exists in the list, and remove it if so
                                if (qvarsList[i2].value.Equals(qStr)) { qvarsList.RemoveAt(i2); }
                            }
                            // don't even try an if/else addition in the loop above - it gets too complex with multiple possible qVar declarations per codeblock
                            qvarsList.Add(compilerVariableClass.getCompilerVariable(qvarsArray[i].Trim(), @"%q" + qStr.Trim()));
                        }
                        //
                        // ISSUE:  
                        //        When a user declares similar variable names such as qvText and qvTextAlign, with the shorter version declared earlier, 
                        //        replacement overwrites part of qvTextAlign.  ie:  if the value of qvText is %q0 then occurrences of qvTextAlign become 
                        //        %q0Align before qvTextAlign replacement can occur
                        // SOLUTION:
                        //        Sort items by name length so that the longest items process/replace FIRST, forcing the "most complete match" to replace 
                        //        ahead of any partial matches.
                        //  
                        this.qvarsList.Sort((x, y) => x.name.Length.CompareTo(y.name.Length));
                        this.qvarsList.Reverse();
                    }
                }
            }
            return r;
        }

        /// <summary>
        /// Processes BEGIN BLOCK directive codelines.
        /// </summary>
        /// <param name="codeLine">
        /// A single line of exploded MUSH code read from a text file.
        /// </param>
        /// <returns>
        /// A boolean value.  True if the string was processed as a BEGIN BLOCK Directive.  Otherwise False.
        /// </returns>
        bool processBeginBlockDirective(string codeLine)
        {
            bool r = false;
            // Testing if this is even a compiler directive - yes this is redundant after processCompilerDirective - no, I don't care - the function may be called directly by some other code at some other time
            if (isCompilerDirective(codeLine))
            {
                // We have found a compiler directive, but what kind? The next line of code returns the portion representing the directive itself (after the CDIR)
                string compilerDirective = Regex.Match(codeLine, compilerDirectivePattern, RegexOptions.None).Groups[1].Value.Trim();
                // If this is in fact a BEGIN BLOCK directive, then the next line sets our return value to True.  Otherwise it is set to false (or remains false)
                r = Regex.IsMatch(compilerDirective, beginBlockDirectivePattern);
                if (r)
                {
                    this.isInBlock = true;
                }
            }

            return r;
        }
        
        /// <summary>
        /// Processes END BLOCK directive codelines.
        /// </summary>
        /// <param name="codeLine">
        /// A single line of exploded MUSH code read from a text file.
        /// </param>
        /// <returns>
        /// A boolean value.  True if the string was processed as a END BLOCK Directive.  Otherwise False.
        /// </returns>
        bool processEndBlockDirective(string codeLine)
        {
            bool r = false;
            // Testing if this is even a compiler directive - yes this is redundant after processCompilerDirective - no, I don't care - the function may be called directly by some other code at some other time
            if (isCompilerDirective(codeLine))
            {
                // We have found a compiler directive, but what kind? The next line of code returns the portion representing the directive itself (after the CDIR)
                string compilerDirective = Regex.Match(codeLine, compilerDirectivePattern, RegexOptions.None).Groups[1].Value.Trim();
                // If this is in fact an END BLOCK directive, then the next line sets our return value to True.  Otherwise it is set to false (or remains false)
                r = Regex.IsMatch(compilerDirective, endBlockDirectivePattern);
                if (r)
                {
                    this.exitBlock();
                }
            }

            return r;
        }

        /// <summary>
        /// Processes COMMENT directive codelines.
        /// </summary>
        /// <param name="codeLine">
        /// A single line of exploded MUSH code read from a text file.
        /// </param>
        /// <returns>
        /// A boolean value.  True if the string was processed as a COMMENT Directive.  Otherwise False.
        /// </returns>
        bool processCommentDirective(string codeLine)
        {
            bool r = false;
            // Testing if this is a compiler directive or comment - a proper comment may be either one
            if (isCompilerDirective(codeLine) || isCommentOrDirective(codeLine))
            {
                // We have found a compiler directive or possibly a comment, but what kind? The next line of code determines exactly what we're dealing with regardless
                string compilerDirective = (isCommentOrDirective(codeLine) ? Regex.Match(codeLine, commentOrDirectivePattern, RegexOptions.None).Groups[1].Value.Trim() : Regex.Match(codeLine, compilerDirectivePattern, RegexOptions.None).Groups[1].Value.Trim());
                // If this is in fact a COMMENT BLOCK directive, then the next line sets our return value to True.  Otherwise it is set to false (or remains false)
                r = Regex.IsMatch(compilerDirective, commentDirectivePattern);
                if (r)
                {
                    // We are in fact dealing with a comment so the next line extracts the comment itself from the codeline
                    string comment = Regex.Match(compilerDirective, commentDirectivePattern, RegexOptions.None).Groups[2].Value.Trim();
                    // Finally we inject the comment into the compiled codeline, following the MUSH standard @@ 
                    compiledCodeString += @"@@" + ( !string.IsNullOrEmpty(comment) ? @" " + comment : string.Empty ) + System.Environment.NewLine;
                }
            }

            return r;
        }

        /// <summary>
        /// Processes NULL directive codelines.
        /// </summary>
        /// <param name="codeLine">
        /// A single line of exploded MUSH code read from a text file.
        /// </param>
        /// <returns>
        /// A boolean value.  True if the string was processed as a NULL Directive.  Otherwise False.
        /// </returns>
        bool processNullDirective(string codeLine)
        {
            bool r = false;
            // Testing if this is a compiler directive or comment - a proper comment may be either one
            if (isCompilerDirective(codeLine) || isCommentOrDirective(codeLine))
            {
                // We have found a compiler directive or possibly a comment, but what kind? The next line of code determines exactly what we're dealing with regardless
                string compilerDirective = Regex.Match(codeLine, compilerDirectivePattern, RegexOptions.None).Groups[1].Value.Trim();
                // If this is in fact a NULL BLOCK directive, then the next line sets our return value to True.  Otherwise it is set to false (or remains false)
                r = Regex.IsMatch(compilerDirective, nullDirectivePattern, RegexOptions.IgnoreCase);
                if (r)
                {
                    // We are in fact dealing with a null
                    // Finally we inject the comment into the compiled codeline, following the MUX standard @@ 
                    compiledCodeString += @"@@";
                }
            }
            return r;
        }

        /// <summary>
        /// Performs compiler directives processing in a very particular order.
        /// </summary>
        /// <param name="codeLine">
        /// A single line of exploded MUSH code read from a text file.
        /// </param>
        /// <returns>
        /// A boolean value.  True if the string was processed as a Compiler Directive.  Otherwise False.
        /// </returns>
        bool processCompilerDirective(string codeLine)
        {
            bool r = isCompilerDirective(codeLine);
            if (r)
            {
                bool b = false;
                if (!b) { b = processCommentDirective(codeLine); }
                if (!b) { b = processIncludeDirective(codeLine); }
                if (!b) { b = processCvarsDirective(codeLine); }
                if (!b) { b = processBvarsDirective(codeLine); }
                if (!b) { b = processParamsDirective(codeLine); }
                if (!b) { b = processQVarDirective(codeLine); }
                if (!b) { b = processNullDirective(codeLine); }
                if (!b) { b = processBeginBlockDirective(codeLine); }
                if (!b) { b = processEndBlockDirective(codeLine); }
            }
            return r;
        }
        
        /// <summary>
        /// Gets a value indicating whether this might be a compiler-recognized comment or compiler directive.
        /// </summary>
        /// <param name="codeLine">
        /// A single line of exploded MUSH code read from a text file.
        /// </param>
        /// <returns>
        /// A boolean value.  True if the string is either a comment or directive.  Otherwise False.
        /// </returns>
        bool isCommentOrDirective(string codeLine)
        {
            return Regex.IsMatch(codeLine, commentOrDirectivePattern);
        }
        
        bool isLeadingSpace(string codeLine)
        {
            return Regex.IsMatch(codeLine, leadingSpacePattern);
        }

        //
        // In writing MU softcode, I like to write code exploded (lots of inner whitespace, carriage returns, etc) and then "compile" it down 
        // to that godawful spaghetti string that pastes into the client sendbox and stores in object attributes for serverside parsing.
        //
        // The compiler needs to know that and when and how NOT to cut whitespaces after certain terms when they appear on a line by themselves.  
        // 
        // These are those terms.
        //
        // tekmunkey - changed ^.*( to ^\s*( because 'th' (short for think) was being caught on terms like WIDTH
        string needsTrailingSpacePattern = @"^\s*("+ // opening the regex
                                                 // These lines can look confusing.  This string is a regex pattern itself, for System.Regex
                                                 //
                                                 // The outer @ leading each line tells C# to take the string LITERALLY - no compiler 
                                                 // regex inside the string (c# does compiler/string regex unrelated to System.RegularExpressions)
                                                 //
                                                 // The inner @ at the start of each pipe-delimited match option is actually part of the match 
                                                 // specification - matching @command or @command/switch/additionalswitch patterns
                                                 //
                                                 // Yes, I could optimize this in some way to recognize switches or remove the relevance of 
                                                 // switching order (so I didn't have to explicitly define every possible combination of switches)
                                                 // but I don't care that much.  This is a utility.  I compile way more often than I worry about it.
                                                 //
                                                 // IN ANY EVENT!  Each of these entries represents a command that must have a space between it and 
                                                 // an argument or parameter that follows it, where that parameter is commonly composed of a complex 
                                                 // set of exploded functions-within-functions.  
                                                 //
                                                 // Usage in a code file then looks like:
                                                 // think 
                                                 //       [set
                                                 //       {
                                                 //           @@ %! is a universal MUSH substitution referring to 'this object right here'
                                                 //           %!,
                                                 //           @@ someAttrib can be found on #68 (some parent object or other)
                                                 //           someAttrib:
                                                 //           @@ This calculation function is stored on blah blah blah
                                                 //           [create(someCalcForFunctionName(),0)]
                                                 //       )]
                                                 //
                                                 @"think|th|" +
                                                 @"@ATTRIBUTE|@ATTRIBUTE/ACCESS|@ATTRIBUTE/ACCESS/RETROACTIVE|" +
                                                 @"@CONFIG|@CONFIG/SET|@CONFIG/SAVE|@CONFIG/SET/SAVE|@CONFIG/SAVE/SET|" +
                                                 @"@FUNCTION|@FUNCTION/PRIVILEGED|" +
                                                 @"@DOLIST|@DOLIST/DELIMIT|@DOLIST/INLINE|@DOLIST/INLINE/DELIMIT|@DOLIST/DELIMIT/INLINE|" +
                                                 @"@DOL|@DOL/DELIMIT|" +
                                                 @"@ASSERT|" +
                                                 @"@BREAK|" +
                                                 @"@SWITCH|@SWITCH/INLINE|@SWITCH/INLINE/FIRST|@SWITCH/FIRST/INLINE|@SWITCH/FIRST|@SWITCH/ALL|@SWITCH/ALL/INLINE|" + 
                                                 @"@WAIT|@WAIT/UNTIL" +
                                                 @")+\s*$"; // closing the regex
        bool needsTrailingSpace(string codeLine)
          {
               return Regex.IsMatch(codeLine, needsTrailingSpacePattern, RegexOptions.IgnoreCase);
          }

        /// <summary>
        /// Given an input codeLine, performs cVar replacement within the string and returns the processed value.
        /// </summary>
        /// <param name="codeLine">
        /// A single line of exploded MUSH code read from a text file.
        /// </param>
        /// <returns>
        /// The result of processing the codeLine, having replaced cVar names with cVar values.
        /// </returns>
        string replaceCVars(string codeLine)
        {
            string r = codeLine;

            if (!object.ReferenceEquals(this.cvarsList, null))
            {
                foreach (compilerVariableClass cvar in this.cvarsList)
                {
                    r = cvar.replaceVariable(r);
                }
            }

            return r;
        }

        /// <summary>
        /// Given an input codeLine, performs bVar replacement within the string and returns the processed value.
        /// </summary>
        /// <param name="codeLine">
        /// A single line of exploded MUSH code read from a text file.
        /// </param>
        /// <returns>
        /// The result of processing the codeLine, having replaced bVar names with bVar values.
        /// </returns>
        string replaceBVars(string codeLine)
        {
            string r = codeLine;

            if (!object.ReferenceEquals(this.bvarsList, null))
            {
                foreach (compilerVariableClass bvar in this.bvarsList)
                {
                    r = bvar.replaceVariable(r);
                }
            }

            return r;
        }

        /// <summary>
        /// Given an input codeLine, performs qVar replacement within the string and returns the processed value.
        /// </summary>
        /// <param name="codeLine">
        /// A single line of exploded MUSH code read from a text file.
        /// </param>
        /// <returns>
        /// The result of processing the codeLine, having replaced qVar names with qVar values.
        /// </returns>
        string replaceQVars(string codeLine)
        {
            string r = codeLine;

            if (!object.ReferenceEquals(this.qvarsList, null))
            {
                foreach (compilerVariableClass qvar in this.qvarsList)
                {
                    r = qvar.replaceVariable(r);
                }
            }

            return r;
        }

        /// <summary>
        /// Given an input codeLine, performs params replacement within the string and returns the processed value.
        /// </summary>
        /// <param name="codeLine">
        /// A single line of exploded MUSH code read from a text file.
        /// </param>
        /// <returns>
        /// The result of processing the codeLine, having replaced params names with params values.
        /// </returns>
        string replaceParams(string codeLine)
        {
            string r = codeLine;

            if (!object.ReferenceEquals(this.paramsList, null))
            {
                foreach (compilerVariableClass param in this.paramsList)
                {
                    r = param.replaceVariable(r);
                }
            }

            return r;
        }

        bool exitBlock()
        {
            bool r = true;

            // Clean up the block-level compiler variables
            if (this.bvarsList != null)
            {
                this.bvarsList.Clear();
            }

            if (this.qvarsList != null)
            {
                this.qvarsList.Clear();
            }

            if (this.paramsList != null)
            {
                this.paramsList.Clear();
            }

            // Make sure the output string has clean linebreaks
            compiledCodeString += System.Environment.NewLine;
            // Flip the explicit isInBlock
            this.isInBlock = false;

            return r;
        }

        /// <summary>
        /// Processes a single line of exploded MUSH code read from a text file, both adding the processed line to its in-memory buffer and returning the processed line to its caller.
        /// </summary>
        /// <param name="codeLine">
        /// The line of text to process.
        /// </param>
        /// <returns>
        /// The processed text.
        /// </returns>
        public string processCodeLine(string codeLine)
        {
            if (this.filePath.Contains(@"troubleshoot") && codeLine.Contains( @"getTableScreenWidth") )
            {
                int bp = 0;
            }

            string r = string.Empty;

            // empty/blank text files caused program crashes.  problem solved.
            if (object.ReferenceEquals(codeLine, null)) { codeLine = string.Empty; }

            if (!isCommentOrDirective(codeLine))
            {
                // if codeline is neither comment or directive...
                // perform block-level bVar replacement first (allow bVars to reference qVars, params, and cVars)
                codeLine = replaceBVars(codeLine);
                // perform block-level qVar replacement second (allow qVars to reference params and cVars)
                codeLine = replaceQVars(codeLine);
                // perform block-level params replacement third (allow params to reference cVars)
                codeLine = replaceParams(codeLine);
                // finally perform file-level cVars replacement, which reference nothing
                codeLine = replaceCVars(codeLine);
                if (!isInBlock && !isLeadingSpace(codeLine))
                {
                    // !isInBlock means the programmer has not manually specified BEGIN BLOCK
                    // !isLeadingSpace means the codeline is butted up against the left edge of the document (no leading whitespace)
                    if (string.IsNullOrEmpty(codeLine))
                    {
                        // codeline is in fact empty (meaning it contains nothing but a System.Environment.Newline)
                        // in this instance, we consider the current input to be END OF BLOCK so terminate the current line in output
                        //
                        // This termination is added directly into compiledCodeString since .Trim() below will actually eliminate the 
                        // linebreaks from codeLine, which we want in cases where there are multiple linebreaks (say 3 or 4) in a row, 
                        // but we still want to terminate the current line of output so:
                        //
                        // test first if compiledCodeString.Length is even long enough to contain a line terminator yet, with AndAlso
                        // if so, test if its last NewLine.Length chars match NewLine...
                        if ((compiledCodeString.Length >= System.Environment.NewLine.Length) && !compiledCodeString.Substring(compiledCodeString.Length-2,System.Environment.NewLine.Length).Equals(System.Environment.NewLine))
                        {
                             // ... if not then go ahead and terminate the current line of output
                            this.exitBlock();
                        }
                    }
                    // .Trim() removes whitespace in several forms so if codeLine is already a System.Environment.Newline then this doesn't 
                    // result in adding another one after the one we just tacked onto output manually
                    compiledCodeString += codeLine.Trim();
                }
                else if (isInBlock || isLeadingSpace(codeLine))
                {
                    // isInBlock means that the programmer has manually specified BEGIN BLOCK
                    // OR ELSE isLeadingSpace means that the programmer has left whitespace at the front of this line
                    // in either case there is nothing to do but add the current codeline to the output after cutting whitespace off both ends
                    compiledCodeString += codeLine.Trim();
                }

                // Finally tack on a trailing space if needed
                bool b = needsTrailingSpace(codeLine);
                if (b)
                {
                    compiledCodeString += @" ";
                }
            }
            else if (!processCompilerDirective(codeLine))
            {
                // the callout in the else if processes directive-style comments
                // the compiler also supports comment-style comments as directives for shorthand notation so another callout is required
                this.processCommentDirective(codeLine);
            }

            return r;
        }

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
        public string processASCIILine(string codeLine)
          {
               string r = codeLine;
               r = r.Replace(@"\", @"\\"); // replace any \ characters in the ASCII art FIRST
               r = r.Replace(@"[", @"\["); // replace any parseable characters in the ASCII art with the \ escape character
               r = r.Replace(@"]", @"\]");
               r = r.Replace(@"(", @"\(");
               r = r.Replace(@")", @"\)");
               r = r.Replace(@"|", @"\|");
               r = r.Replace(@" ", @"%b"); // replace any spaces with the %b
               //r = r.Replace(System.Environment.NewLine, @"%r"); // replace newline with %r
               r += @"%r"; // looks like newlines don't translate when reading text line by line with StreamReader, so just tack %r onto every line processed
               compiledCodeString += r;
               return r;
          }

        /// <summary>
        /// Initializes a new instance of the mushCompilerClass with empty/default options and variables.
        /// </summary>
        internal mushCompilerClass_OLD(string filepath)
        {
            this.filePath = filepath;
            //
            //   The code ENACTOR is the object or player that called the command or function from the "commandline."  In all forms of MUSH this replacement is 
            //   %# and one of the most famous and common errors on the books is confusing and/or typoing %# with %!, which causes insane and insanely difficult 
            //   to find problems at runtime.  Using these explicit cVars with the mushCompiler should help to eliminate that error entirely and forever.
            //
            this.cvarsList.Add(compilerVariableClass.getCompilerVariable(@"cvENACTOR",@"%#"));
            //
            //   The code EXECUTOR is the object that actually contains and is performing the code execution that is currently running.  In all forms of MUSH this 
            //   replacement is %! and one of the most famous and common errors on the books is confusing and/or typoing %# with %!, which causes insane and insanely 
            //   difficult to find problems at runtime.  Using these explicit cVars with the mushCompiler should help to eliminate that error entirely and forever.
            //
            this.cvarsList.Add(compilerVariableClass.getCompilerVariable(@"cvEXECUTOR",@"%!"));
            //
            //   TinyMUX actually demands/requires a NULL-Delimiter in iter() output delimiter fields where you want nothing to appear for delimiters, where Penn (for 
            //   example) lets you just leave the field blank/empty for null.  We did add the NULL Compiler Directive for this purpose, but that's unhandy in places where 
            //   a person actually wants an @@ with a comma after it too, so introducing a cVar.  Sheesh!
            //
            this.cvarsList.Add(compilerVariableClass.getCompilerVariable(@"cvNULL",@"@@"));
            //
            //   The cvCodeObject cVar is required for various include files in the coreCodePackage - defaulting it here is a courtesy
            //
            this.cvarsList.Add(compilerVariableClass.getCompilerVariable(@"cvCodeObject", @"%!"));
        }

        /// <summary>
        /// Initializes a new instance of the mushCompilerClass with the specified cVars (file-level-scoped compiler variables).  
        /// 
        /// Generally intended only for use with includes.
        /// </summary>
        /// <param name="withInitialCvars">
        /// A list of cVars to initialize the new mushCompilerClass instance with.
        /// </param>
        internal mushCompilerClass_OLD(string filepath, List<compilerVariableClass> withInitialCvars)
        {
            char[] pathcopy = new char[filepath.Length];
            filepath.CopyTo(0, pathcopy, 0, pathcopy.Length);
            this.filePath = new string(pathcopy);
            pathcopy = null;

            if (withInitialCvars != null)
            {
                this.cvarsList = new List<compilerVariableClass>(withInitialCvars);
            }
        }

        ~mushCompilerClass_OLD()
        {
            this.beginBlockDirectivePattern = null;
            this.bvarsDirectivePattern = null;
            this.commentDirectivePattern = null;
            this.commentOrDirectivePattern = null;
            this.compiledCodeString = null;
            this.compilerDirectivePattern = null;
            this.cvarsDirectivePattern = null;
            this.endBlockDirectivePattern = null;
            this.includeDirectivePattern = null;
            this.leadingSpacePattern = null;
            this.needsTrailingSpacePattern = null;
            this.paramsDirectivePattern = null;
            this.qvarsDirectivePattern = null;

            if (this.paramsList != null)
            {
                this.paramsList.Clear();
                this.paramsList = null;
            }

            if (this.qvarsList != null)
            {
                this.qvarsList.Clear();
                this.qvarsList = null;
            }

            if (this.bvarsList != null)
            {
                this.bvarsList.Clear();
                this.bvarsList = null;
            }

            if (this.cvarsList != null)
            {
                this.cvarsList.Clear();
                this.cvarsList = null;
            }

            this.filePath = null;

            GC.Collect();
        }
     }
}
