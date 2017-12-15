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