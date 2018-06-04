using System;
using System.Collections.Generic;
using System.Text;

namespace mushCompiler.mushCode
{
    internal class characters
    {
        /// <summary>
        /// escChars characters are characters which precede another character which should always be taken as 
        /// a character literal, whether it is a MUSH-parseable character or not.
        /// </summary>
        internal static char[] escChars = new char[]
        {
            (char)0x5c, // backslash /
            (char)0x25, // percent %
        };

        /// <summary>
        /// lineTerms characters are characters which represent either some platform-specific lineterm or the 
        /// last character found in any given platform's lineterm.
        /// </summary>
        internal static char[] lineTerms = new char[]
        {
            (char)0x0d, // CR
            (char)0x0a, // LF
        };

        internal static char[] whiteSpace = new char[] 
        {
            (char)0x09, // horz tabstop
            (char)0x20, // standard non-breaking-space
        };

        /// <summary>
        /// cmdGroup characters are used to signal to MUSH that commands contained within should be grouped, such 
        /// as with individual @switch conditions, @dolist list-operations, etc.
        /// </summary>
        internal static char[] cmdGroup = new char[]
        {
            (char)0x7b, // left-curly-brace {
            (char)0x7d, // right-curly-brace }
        };

        /// <summary>
        /// forceParse characters are characters used in MUSH softcode to indicate that what is found between them 
        /// must always be parsed by the MU platform.
        /// </summary>
        internal static char[] forceParse = new char[]
        {
            (char)0x5b, // left-square-bracket [
            (char)0x5d, // right-square-bracket ]
        };

        /// <summary>
        /// breakBeforeChars is only used in decompile operations, indicating the list of characters which should always 
        /// follow a linebreak.  
        /// 
        ///   * Chars that should appear on a line by themselves should appear in both breakBeforeChars and breakAfterChars.
        /// </summary>
        internal static char[] breakBeforeChars = new char[]
        {
            (char)0x2c, // comma
            (char)0x3b, // semicolon ;
            (char)0x28, // left-paren (
            (char)0x29, // right-paren )
            (char)0x5b, // left-square-bracket [
            (char)0x7b, // left-curly-brace {  
            (char)0x7d, // right-curly-brace }
            (char)0x2f, // forward slash /
        };

        /// <summary>
        /// breakAfterChars is used only in decompile operations, indicating the list of characters which should always 
        /// be followed by a linebreak.  
        /// 
        ///   * Chars that should appear on a line by themselves should appear in both breakBeforeChars and breakAfterChars.
        /// </summary>
        internal static char[] breakAfterChars = new char[]
        {
            (char)0x2c, // comma
            (char)0x3b, // semicolon char ;
            (char)0x28, // left-paren (
            (char)0x5d, // right-square-bracket ]
            (char)0x7b, // left-curly-brace {
            (char)0x7d, // right-curly-brace }
            (char)0x2f, // forward slash /
        };

        /// <summary>
        /// indentTriggers is used only in decompile operations, indicating the list of characters which trigger additional 
        /// indentation on lines to follow the line on which this char appears.  
        /// 
        ///   * Additional indentation is typically applied not on the same line, but only on lines after it.
        /// </summary>
        internal static char[] indentTriggers = new char[]
        {
            (char)0x28, // left-paren (
            (char)0x7b, // left-curly-brace {
            (char)0x3d, // equals sign =
        };

        /// <summary>
        /// dedentTriggers is used only in decompile operations, indicating the list of characters which trigger reduced 
        /// indentation on lines to follow the line on which this char appears.
        /// 
        ///   * Reduced indentation is typically applied on the same line as the trigger and lines after it.
        /// </summary>
        internal static char[] dedentTriggers = new char[]
        {
            (char)0x29, // right-paren )
            (char)0x7d, // right-curly-brace }
            (char)0x3b, // semicolon ;
        };

        /// <summary>
        /// Tests if the specified char is found in the specified char array.
        /// </summary>
        /// <param name="cArr">
        /// The char array to look in.
        /// </param>
        /// <param name="c">
        /// The char to look for.
        /// </param>
        /// <returns>
        /// A boolean value.  True if char occurs in cArr.  Otherwise False.
        /// </returns>
        internal static bool isCharIn(char[] cArr, char c)
        {
            bool r = false;

            // Testing each of the defined cArr characters
            for (int i = 0; i < cArr.Length; i++)
            {
                // Set return value to boolean test of current cArr char vs input char
                r = (cArr[i] == c);
                // If r is ever true, we're done processing
                if (r) { break; }
            }

            return r;
        }

        /// <summary>
        /// Gets a value indicating whether the char at the specified index into testString should be treated 
        /// as an escaped character.
        /// </summary>
        /// <param name="testString"></param>
        /// <param name="charIdx"></param>
        /// <returns></returns>
        internal static bool isCharEscaped(string testString, int charIdx)
        {
            bool r = false;

            if (!string.IsNullOrEmpty(testString) && (charIdx > 0) && (charIdx < testString.Length))
            {
                // 
                // If the char immediately preceding charIdx is an escape character, then MAYBE charIdx is escaped
                //
                r = isCharIn(escChars, testString[charIdx - 1]);
                //
                // Now if r is true the char preceding charIdx is an escape char, but we still need to validate 
                // against all preceding characters to ensure that this escape char isn't itself escaped
                //
                if (r)
                {
                    // We only need to proceed with this additional test if there are in fact characters preceding 
                    // the already-discovered escape char that's ahead of charIdx
                    if ((charIdx - 2) > 0)
                    {
                        // Step backward until the first char that is not an escape-char
                        for (int i = (charIdx - 2); i > -1; i--)
                        {
                            if (!isCharIn(escChars, testString[i]))
                            {
                                // The first non-escape char is the end of iteration
                                break;
                            }
                            else
                            {
                                //
                                // As long as the previous character continues to be an escape character, just toggle r 
                                // to whatever its boolean opposite is, since the character following the one currently 
                                // tested is definitely AN ESCAPED ESCAPE CHAR up until we hit the first non-escape char
                                //
                                // Since r is FALSE when the first char preceding charIdx
                                r = !r;
                            }
                        }
                    }
                }
            }

            return r;
        }

        /// <summary>
        /// Gets a count of how many unescaped instances of the char c occur in the string testString.  Useful for comparing 
        /// opening vs closing brackets.
        /// 
        /// In MU Code, escape characters are typically \ and %.
        /// </summary>
        /// <param name="testString"></param>
        /// <param name="c"></param>
        /// <returns></returns>
        internal static uint countUnescaped(string testString, char c)
        {
            uint r = 0;

            for (int i = 0; i < testString.Length; i++)
            {
                if ((testString[i] == c) && !characters.isCharEscaped(testString, i))
                {
                    r++;
                }
            }

            return r;
        }
    }
}
