@ECHO OFF

SETLOCAL 
rem
rem Set the path to the mushCompiler.exe - this must be a path to the exe itself
rem
SET compilerPath=".\mushCompiler.exe"
rem 
rem Set the path to the output directory - this must be a path to a DIRECTORY and not to a file 
rem 
rem Don't put a drive letter in this variable that isn't the same drive that the batch file lives on
rem
rem Don't put a trailing backslash on this variable name - it's added as needed in later codelines
rem
rem Don't put quotation marks in this variable even if the pathname contains spaces - we need to concat 
rem a filename later on and we add quotation marks as needed in later codelines
rem
SET outputPath=.\Compiles

rem
rem Create the output path directory if it doesn't exist 
rem
IF NOT EXIST "%outputPath%\" mkdir "%outputPath%\"

rem
rem 'compiling' exploded MUSH code is done by processing Compiler Directives and unless instructed otherwise, simply 
rem removing whitespace from the beginning and end of text lines and treating any lines that aren't blank (CRLF in 
rem Windows, LF in Linux, recognized by System.Environment.NewLine via DotNet/Mono) as 'codeblocks' to be concatenated 
rem to the line above - creating a single unending line of code from the first non-blank line to the next one (or the 
rem first BEGIN BLOCK compiler directive to the first END BLOCK, or the first non-blank line to the first END BLOCK 
rem directive, whatever comes first).
rem
rem The manual/commandline form of this command is mushCompiler /code -infile=sourcePath -outfile=targetPath
rem
rem The next batch command will:
rem   recurse all .msh code files and compile to file of same name in .\Compiles, replacing .msh with .mush extension
rem
for %%f in (.\*.msh) do %compilerPath% /code -infile="%%f" -outfile="%outputPath%\%%~nf-COMPILED.mush"

rem
rem 'compiling' ASCII art is done simply by escaping out all the characters that MU/SH/X platforms like to try and process,
rem such as % or \ or { or } or ( or ) or [ or ] or ; or , or whatever, then converting all newlines to %r
rem
rem The output file will contain a single string constructed from the input file, so the compiler assumes a single ASCII art 
rem image is presented per compile run.  In this case 3 ASCII art images are contained in the example file (a bubble-text-font 
rem number 1, a bubble-text-font letter A, and a lightning bolt coming out of a thundercloud).  You can open the test file 
rem titled ASCIIWorkingFile.ascii to view the original, then open the .\Compiles\ASCIIWorkingFile.ascsh file and copy the 
rem string out of it into any MUSH to test it.  Just type think <paste the text here> to see the result of ASCII art 
rem compilation.
rem
rem The manual/commandline form of this command is mushCompiler /ascii -infile=sourcePath -outfile=targetPath
rem
rem rem The next batch command will:
rem   recurse all .ascii text files and compile to file of same name in .\Compiles, replacing .ascii with .ascsh extension
rem
for %%f in (.\*.ascii) do %compilerPath% /ascii -infile="%%f" -outfile="%outputPath%\%%~nf-COMPILED.ascsh"

rem
rem next line obsoleted by error trapping/pause in console app
rem
rem if NOT ["%errorlevel%"]==["0"] pause
ENDLOCAL