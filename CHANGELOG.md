# 2017-12-15

## Changelog initiated

## programArgsClass created

## Program.COMPILE_TYPE_* constants created

## Added new ASCII Compile Type.

    Previously, ASCII compiles were performed with only code-usage in mind, compiling ASCII art down to a single continuous line with linebreaks replaced with %r, and MUSH-parsed characters escaped so that a whole ASCII art image could be stored in an object attribute.

    A new multi-line compile type has been created, allowing ASCII art to be formatted with MUSH-parsed characters escaped but without removing/replacing linebreaks, so that very large ASCII art images can be copy/pasted or @emitted via a MUSH client's upload feature line-by-line (this is a workaround for MUSH clients that have an input buffer limit, or platforms that have a pose/emit character length limit).

    The demo compile.bat (in bin/Debug directory) updated

## Default COMPILE_TYPE_CODE defined.  User no longer must specify /code at commandline unless it pleases them to do so.

    * If user does wish to specify compile type they must now specify:

    * /compiletype=code

  OR

    * /compiletype=ascii

    (which is the same as)

    * /compiletype=ascii-singleline

  OR

    * /compiletype=ascii-multiline

## mushCompilerClass renamed to mushCompilerClass_OLD

## asciiCompilerClass created - functionality rewritten/overhauled from mushCompilerClass_OLD (left in original for posterity)