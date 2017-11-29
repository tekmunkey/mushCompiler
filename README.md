# mushCompiler

## HISTORICAL/SCENIC ROUTE:  (FEEL FREE TO SKIP DOWN TO USAGE NOTES)

Saying that we 'compile' MUSH softcode is inaccurate, but this thing actually does include many of the features found in real compilers.  Additionally there already is (and was 20+ years ago) a product called "MUSH Code Formatter" which did/does nothing but cut linebreaks and trim leading/trailing whitespace from exploded MUSHCode, and this product does a whole lot more.

Among these features are include files, named and scoped variables, compiler-stripped and compiler-passed (distribution) comments, and pre-processor directives designed to facilitate MUSH Softcoding regardless of your chosen MUSH platform.  

In addition to converting exploded-format MUSH softcode into MU/SH/X parse-able lines which you can easily upload via a client's input->send function, this product also rapidly reformats ASCII Art and escapes troublesome characters into a format you can send straight into a MUSH or MUX for that quality look and feel players love.

I would not say that this is my proudest achievement in programming or even in C#.  I never sat down with the intention to write a MUSH Softcode compiler.  What happened is that one day I got tired of manually copying exploded MUSH code into a second text file and deleting linebreaks and leading spaces by hand, so I wrote a bit of code to do it for me.  Over the following weeks, months, and years, bits and pieces got added to it.  I couldn't even tell you in what order.  There's never been a development plan for this thing or even for any particular feature, it just gets hacked together and modded in-place whenever I decide I want some new bit here or there, like a named variable at some scope or other, in some fashion or other.

The only reason this Frankenproject is getting posted to github at all (when it's really just a private toolkit for my personal use) is because as of this date MUSH Softcoding seems to be in sharp decline.  My toolkit makes it a lot faster and easier for me, and I wonder if it wouldn't be more attractive for newbies to get into if they could employ more common concepts like codeblocks and named variables with scope, when I've been selfishly keeping to myself for several years just because it never occurred to me to share it.

I'm also starting off on a new MUSH Code project that I'm going to be posting on GitHub and while I'll be posting the compiled code ready-for-upload for those who don't give a damn about looking under the hood or making custom modifications, the original source will be posted along with that which means exploded code with named and scoped variables, which means unless a pro-grade softcoder takes the compiled code and rips into it folks are going to need my compiler.  So here's the compiler with source, with my best wishes and sincere hope that it helps renew interest in the sport.

## USAGE NOTES:

This project is C# DotNET and the .sln and .vcproj files are Console Application files from Visual Studio (VS) 2008 Professional.  It targets DotNET Framework 2.0 and uses references to System, System.IO, System.Text.RegularExpressions.  It should compile and run under the Mono Framework for Linux, but hasn't been tested there.  It has compiled and run just fine under every version of Windows from XP to Windows 7.  It should compile and run under Windows 2000 (if you deliberately install DotNET Framework 2.0), 8, and 10 (without any special steps), but hasn't been tested.

###### If you want to use this code with any Express version of VS 2008 or any earlier version, just create a new Console Application project and copy mushCompiler/Program.cs and mushCompiler/mushCompilerClass.cs into the directory where that project's Program.cs file resides and let it overwrite.  Then compile your own copy.  Or grab mushCompiler.exe from the mushCompiler/bin/Debug directory and study the comments in compile.bat script and provided sample files in that directory for usage documentation.

If you open this .sln or .vcproj with any newer version of VS (2012 and up) then it should have no problem converting the solution and project into the newer format.  If it does, then just refer to the previous line and treat it like an earlier version.

If you don't care about the source, then just navigate into mushCompiler/bin/Debug (yes, I'm a sloppy horrible programmer who never bothered to create a proper 'Release' version - read above to find out why or read on to get more useful info) and take a look at the operational samples provided.

The 'debug' configuration for the .vcproj file is basically the same as the compile.bat file, except without the ASCII Art demonstration (it only compiles the code file).  That's because you can only do one commandline in VS 2008 Debug Configurations and that's the one I opted for.

###### You'll find:  mushCompiler.exe

Usage:
######    mushCompiler.exe /code -infile=sourcePath -outfile=destPath

######    mushCompiler.exe /ascii -infile=sourcePath -outfile=destPath

      ie:  mushCompiler.exe /code -infile="c:\my code directory\some sub directory\my input file.msh" -outfile="c:\my code directory\some sub directory\Compiles\my ouput file.mush"

**Note the quotation marks around the paths.  These are absolutely necessary in windows commandline paths if and only if there are spaces in path names.**

      ie:  mushCompiler.exe /ascii -infile="c:\my art directory\my input file.txt" -outfile="c:\my art directory\my output file.txt"

###### You'll find: 0x00.includeTest.inmsh

      This is a MUSH softcode file that demonstrates the compiler's pre-processor "include" directive (it contains softcode and it's the file that gets included in another code file).  This file doesn't get compiled directly when you run the compile.bat file.

###### You'll find: 0x01.compileTest.msh

      This is a MUSH softcode file that actually includes the includeTest file and contains additional softcode.  This is the MUSH Softcode that actually gets compiled when you run the compile.bat file.

###### You'll find:  ASCIIWorkingFile.ascii

      This is a plain ASCII text file (UTF-8 without a byte order marker) containing 3 pieces of ASCII Art.  2 of them are bubble-font characters (the number 1 and the letter A) and 1 of them is a thunder cloud with a lightning bolt.  This is an ASCII Art file that gets compiled when you run the compile.bat file.

###### You'll find: cmd.bat

This Windows Batch file should be compatible with any version of Windows back to XP and possibly 2000, but has only been tested as far back as Windows 7.  Compatibility is determined by documentation.  All it does is (if you double-click on it) open a command prompt and ensure that the command prompt is focused on the directory where the batch file was double-clicked, even if you right-click and select Run As Administrator (which without some trickery included in the batch file would otherwise focus on %WINDIR%\System32)

This batch file is handy if, for example, you like keeping a command prompt open while using the compile.bat so you can keep an eye on output (you would just click on the command prompt window, hit the UP ARROW on the keyboard, hit ENTER, watch).

###### You'll find: compile.bat

This script assumes that it resides in a directory with mushCompiler.exe and a subdirectory named Compiles, one or more softcode files with the extension .msh, and one or more ascii art files with the extension .ascii

For each .msh file it finds, it automagically calls mushCompiler.exe /code with the .msh file as the input and .\Compiles\samename-COMPILED.mush as the output

For each .ascii file it finds, it automagically calls mushCompiler.exe /ascii with the .ascii file as the input and .\Compiles\samename-COMPILED.ascsh as the output

If there are no .msh or .ascii files then it simply doesn't do anything with that particular extension.  If there is one but not the other, then it does something with what it does find.

### So you can copy mushCompiler.exe and the compile.bat file into any/every directory where you store your softcode files, or you can copy mushCompiler.exe into some static location and edit the value of the SET compilerPath=".\mushCompiler.exe" variable in the batch file, and replace the . with the static path to the directory where mushCompiler.exe lives, then just copy the batch file into any/every directory where softcode or ASCII art resides (this last method is my strong recommendation).

      