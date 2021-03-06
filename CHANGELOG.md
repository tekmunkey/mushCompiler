# 2017-12-15

* Changelog initiated

* mushCompilerClass renamed to mushCompilerClass_OLD

* asciiCompilerClass created - functionality rewritten/overhauled from mushCompilerClass_OLD (left in original for posterity)

* Program.COMPILE_TYPE_* constants created

* programArgsClass created

## * Added new ASCII Compile Type.

Previously, ASCII compiles were performed with only code-usage in mind, compiling ASCII art down to a single continuous line with linebreaks replaced with %r, and MUSH-parsed characters escaped so that a whole ASCII art image could be stored in an object attribute.

A new multi-line compile type has been created, allowing ASCII art to be formatted with MUSH-parsed characters escaped but without removing/replacing linebreaks, so that very large ASCII art images can be copy/pasted or @emitted via a MUSH client's upload feature line-by-line (this is a workaround for MUSH clients that have an input buffer limit, or platforms that have a pose/emit character length limit).

The demo compile.bat (in bin/Debug directory) updated

* Default COMPILE_TYPE_CODE defined.  User no longer must specify /code at commandline unless it pleases them to do so.  If user does wish to specify compile type they must now specify:

    /compiletype=code

###### OR

    /compiletype=ascii

###### (which is the same as)

    /compiletype=ascii-singleline

###### OR

    /compiletype=ascii-multiline

## * Order of commandline arguments is no longer relevant, and compile type defaults to code.

Previously, users were absolutely required to specify:

    mushCompiler.exe /type -infile=filename -outfile=filename

Where /type was code or ascii and must be the first parameter, infile must be the first parameter, and outfile must be the second.

Now:

    mushCompiler.exe -outfile=otherfilename -infile=filename

OR

    mushCompiler.exe --infile=filename /outfile=otherfilename

Are exactly the same as:

    mushCompiler.exe /infile=filename -codetype=code --outfile=otherfilename

Of course you're welcome to use the same parameter lead-character for everything.  The point of making all those lead-ins recognizable is that people tend to be familiar with different styles (and get used to different styles depending on what they're working with) and it this helps keep your workflow going smoothly.  So when you're used to linux commandline switches, you can use --parameter=value, and when you're in MUSH command switching mode, /parameter=value, and so on.

    mushCompiler.exe --codetype=ascii --infile=filename --outfile=otherfilename

# 2018-01-18

* No clue exactly when this bug was introduced, but:  At some point an "update" to the DotNET Framework introduced a problem with initializing System.Collections.Generic.List objects at declaration time.  Specifically, Lists would work for a little while (if you add items to them and then refer back to this.ListObject they would return their elements) and then randomly they would quit working (for no apparent reason and with absolutely no consistency you would refer back to this.ListObject and it would return no elements at all, as if it were a 'new' ListObject as declared).  When I say 'without consistency' I mean that across a hundred runtime tests, this error occurred in the same place 20 or 30 times and then occurred in different places 15 or 20 times, and different places still another 40 times.  I shudder to think how many people replaced RAM or CPUs and still didn't fix the problem.  The solution seems to be to declare these objects as 'null' and instantiate them as needed rather than instantiate them at declaration time.  Nice work, lowest bidder in unnamed but likely guessable 3rd World Country who Microsoft outsourced this update to!!!

# 2018-01-20

* Added @switch/all and @switch/all/inline to needsTrailingSpacePattern regex pattern (allowing MUSH coders to use these command switches with the @switch statement)

* Updated EditPad Syntax Coloring

# 2018-01-21

* OK SO the update on 2018-01-18 wasn't Microsoft's fault.  Was my fault.  Not a bug in DotNET.  Something stupid I did.  Found it.  Fixed it.  By myself.  Sorry, 3rd world programmers.

# 2018-01-22

* Added @break and @assert to mushCompiler's recognized command syntax (needsTrailingSpacePattern, a regex that allows floating words to retain a trailing space when trimming/concatenating lines)

* Added @break and @assert to Editpad syntax highlighting

# 2018-01-24

* Added file-include feature/function allowing subdirectory traversal and tracking so files in large projects can be organized in subdirectories and relative paths in include directives would work properly.  Yes, this had to be added manually and No I don't consider it a bug or an issue with my code.  DotNET Framework didn't recognize .\somefile.txt as a valid file when called from a System.IO.FileStream instance that had a file open in the same directory as somefile.txt.

* Discovered/corrected a bug/issue I had introduced (when fixing a previous issue or adding a previous feature) where an extra %q string was being added to qVar string replacements.

# 2018-01-25

* Added NULL compiler directive (@@ CDIR NULL) providing facility for piping through literal @@ characters without whitespace and without linebreaks into codeblocks, such as for injecting the @@ as a "Null Delimiter" specifier into softcode for TinyMUX.

# 2018-02-03

* Added SAFEPARAMETERS and SAFEPARAMS (alongside PARAMETERS AND PARAMS) compiler directives.  When specified SAFE, the params directives convert named variables to positional values with objeval() ie: objeval(%#,%0) rather than simply %0

# 2018-02-06

* Modded the way PARAMS/SAFEPARAMS and all variable (cvar/bvar/qvar/params) operates as follows:  When specifying safeparams, this now sets objeval() as the **default** output style for parameter variable replacements.  All variables now have the additional reference styles of variableSAFE, variableUNSAFE, and variableLIT where adding the SAFE keyword replaces the variable name with objeval(%#,variableValue) whether the variable was originally declared with safeparams or not, variableUNSAFE replaces the variable name with the raw variableValue whether the variable was originally declared with safeparams or not, and variableLIT replaces the variable name with lit(variableValue)

# 2018-02-09

* Added cVar replacement to INCLUDE directive processing, so for example a static directory path may be set into a cVar and re-used repeatedly for multiple file includes (and passed into those included files as cVars traverse INCLUDE boundaries)

# 2018-02-10

* Added compiler variable SETQ and SETR options, providing the facility to easily declare any compiler variable containing a q-register and then automatically set that register by pre-compiler name.

    @@ CDIR bvar bvMyBlockVariable = %q0

    &myFunctionAttribute myObject=[bvMyBlockVariableSETQ(add(2,5))][%q0]

# 2018-05-30

* Fixed a bug that mangled cVar/bVar names/replacements when there was no space preceding the = in the Var definition line.

* Modded the AddonFunc behavior/signature to help differentiate var names from addon functions

    @@ CDIR bvar bvMyBlockVariable = %q0

    &myFunctionAttribute myObject=[bvMyBlockVariable.SETQ(add(2,5))][%q0]

# 2018-06-02

* Added mushDecompilerClass

* Added mushCode directory/namespace to project

* Added mushCode.muFunctionClass 

* Added mushCode.characters (a static class)

* Added commandline options (--compiletype=option) "decompile" and "decomp" and "decompcode" to refer to softcode decompile/deformat/explode operation.