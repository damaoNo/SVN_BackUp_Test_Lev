// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;
using System.Globalization;
using System.Runtime;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Security;

namespace System.Globalization
{
    internal partial class FormatProvider
    {
        // The Number class implements methods for formatting and parsing
        // numeric values. To format and parse numeric values, applications should
        // use the Format and Parse methods provided by the numeric
        // classes (Byte, Int16, Int32, Int64,
        // Single, Double, Currency, and Decimal). Those
        // Format and Parse methods share a common implementation
        // provided by this class, and are thus documented in detail here.
        //
        // Formatting
        //
        // The Format methods provided by the numeric classes are all of the
        // form
        //
        //  public static String Format(XXX value, String format);
        //  public static String Format(XXX value, String format, NumberFormatInfo info);
        //
        // where XXX is the name of the particular numeric class. The methods convert
        // the numeric value to a string using the format string given by the
        // format parameter. If the format parameter is null or
        // an empty string, the number is formatted as if the string "G" (general
        // format) was specified. The info parameter specifies the
        // NumberFormatInfo instance to use when formatting the number. If the
        // info parameter is null or omitted, the numeric formatting information
        // is obtained from the current culture. The NumberFormatInfo supplies
        // such information as the characters to use for decimal and thousand
        // separators, and the spelling and placement of currency symbols in monetary
        // values.
        //
        // Format strings fall into two categories: Standard format strings and
        // user-defined format strings. A format string consisting of a single
        // alphabetic character (A-Z or a-z), optionally followed by a sequence of
        // digits (0-9), is a standard format string. All other format strings are
        // used-defined format strings.
        //
        // A standard format string takes the form Axx, where A is an
        // alphabetic character called the format specifier and xx is a
        // sequence of digits called the precision specifier. The format
        // specifier controls the type of formatting applied to the number and the
        // precision specifier controls the number of significant digits or decimal
        // places of the formatting operation. The following table describes the
        // supported standard formats.
        //
        // C c - Currency format. The number is
        // converted to a string that represents a currency amount. The conversion is
        // controlled by the currency format information of the NumberFormatInfo
        // used to format the number. The precision specifier indicates the desired
        // number of decimal places. If the precision specifier is omitted, the default
        // currency precision given by the NumberFormatInfo is used.
        //
        // D d - Decimal format. This format is
        // supported for integral types only. The number is converted to a string of
        // decimal digits, prefixed by a minus sign if the number is negative. The
        // precision specifier indicates the minimum number of digits desired in the
        // resulting string. If required, the number will be left-padded with zeros to
        // produce the number of digits given by the precision specifier.
        //
        // E e Engineering (scientific) format.
        // The number is converted to a string of the form
        // "-d.ddd...E+ddd" or "-d.ddd...e+ddd", where each
        // 'd' indicates a digit (0-9). The string starts with a minus sign if the
        // number is negative, and one digit always precedes the decimal point. The
        // precision specifier indicates the desired number of digits after the decimal
        // point. If the precision specifier is omitted, a default of 6 digits after
        // the decimal point is used. The format specifier indicates whether to prefix
        // the exponent with an 'E' or an 'e'. The exponent is always consists of a
        // plus or minus sign and three digits.
        //
        // F f Fixed point format. The number is
        // converted to a string of the form "-ddd.ddd....", where each
        // 'd' indicates a digit (0-9). The string starts with a minus sign if the
        // number is negative. The precision specifier indicates the desired number of
        // decimal places. If the precision specifier is omitted, the default numeric
        // precision given by the NumberFormatInfo is used.
        //
        // G g - General format. The number is
        // converted to the shortest possible decimal representation using fixed point
        // or scientific format. The precision specifier determines the number of
        // significant digits in the resulting string. If the precision specifier is
        // omitted, the number of significant digits is determined by the type of the
        // number being converted (10 for int, 19 for long, 7 for
        // float, 15 for double, 19 for Currency, and 29 for
        // Decimal). Trailing zeros after the decimal point are removed, and the
        // resulting string contains a decimal point only if required. The resulting
        // string uses fixed point format if the exponent of the number is less than
        // the number of significant digits and greater than or equal to -4. Otherwise,
        // the resulting string uses scientific format, and the case of the format
        // specifier controls whether the exponent is prefixed with an 'E' or an
        // 'e'.
        //
        // N n Number format. The number is
        // converted to a string of the form "-d,ddd,ddd.ddd....", where
        // each 'd' indicates a digit (0-9). The string starts with a minus sign if the
        // number is negative. Thousand separators are inserted between each group of
        // three digits to the left of the decimal point. The precision specifier
        // indicates the desired number of decimal places. If the precision specifier
        // is omitted, the default numeric precision given by the
        // NumberFormatInfo is used.
        //
        // X x - Hexadecimal format. This format is
        // supported for integral types only. The number is converted to a string of
        // hexadecimal digits. The format specifier indicates whether to use upper or
        // lower case characters for the hexadecimal digits above 9 ('X' for 'ABCDEF',
        // and 'x' for 'abcdef'). The precision specifier indicates the minimum number
        // of digits desired in the resulting string. If required, the number will be
        // left-padded with zeros to produce the number of digits given by the
        // precision specifier.
        //
        // Some examples of standard format strings and their results are shown in the
        // table below. (The examples all assume a default NumberFormatInfo.)
        //
        // Value        Format  Result
        // 12345.6789   C       $12,345.68
        // -12345.6789  C       ($12,345.68)
        // 12345        D       12345
        // 12345        D8      00012345
        // 12345.6789   E       1.234568E+004
        // 12345.6789   E10     1.2345678900E+004
        // 12345.6789   e4      1.2346e+004
        // 12345.6789   F       12345.68
        // 12345.6789   F0      12346
        // 12345.6789   F6      12345.678900
        // 12345.6789   G       12345.6789
        // 12345.6789   G7      12345.68
        // 123456789    G7      1.234568E8
        // 12345.6789   N       12,345.68
        // 123456789    N4      123,456,789.0000
        // 0x2c45e      x       2c45e
        // 0x2c45e      X       2C45E
        // 0x2c45e      X8      0002C45E
        //
        // Format strings that do not start with an alphabetic character, or that start
        // with an alphabetic character followed by a non-digit, are called
        // user-defined format strings. The following table describes the formatting
        // characters that are supported in user defined format strings.
        //
        // 
        // 0 - Digit placeholder. If the value being
        // formatted has a digit in the position where the '0' appears in the format
        // string, then that digit is copied to the output string. Otherwise, a '0' is
        // stored in that position in the output string. The position of the leftmost
        // '0' before the decimal point and the rightmost '0' after the decimal point
        // determines the range of digits that are always present in the output
        // string.
        //
        // # - Digit placeholder. If the value being
        // formatted has a digit in the position where the '#' appears in the format
        // string, then that digit is copied to the output string. Otherwise, nothing
        // is stored in that position in the output string.
        //
        // . - Decimal point. The first '.' character
        // in the format string determines the location of the decimal separator in the
        // formatted value; any additional '.' characters are ignored. The actual
        // character used as a the decimal separator in the output string is given by
        // the NumberFormatInfo used to format the number.
        //
        // , - Thousand separator and number scaling.
        // The ',' character serves two purposes. First, if the format string contains
        // a ',' character between two digit placeholders (0 or #) and to the left of
        // the decimal point if one is present, then the output will have thousand
        // separators inserted between each group of three digits to the left of the
        // decimal separator. The actual character used as a the decimal separator in
        // the output string is given by the NumberFormatInfo used to format the
        // number. Second, if the format string contains one or more ',' characters
        // immediately to the left of the decimal point, or after the last digit
        // placeholder if there is no decimal point, then the number will be divided by
        // 1000 times the number of ',' characters before it is formatted. For example,
        // the format string '0,,' will represent 100 million as just 100. Use of the
        // ',' character to indicate scaling does not also cause the formatted number
        // to have thousand separators. Thus, to scale a number by 1 million and insert
        // thousand separators you would use the format string '#,##0,,'.
        //
        // % - Percentage placeholder. The presence of
        // a '%' character in the format string causes the number to be multiplied by
        // 100 before it is formatted. The '%' character itself is inserted in the
        // output string where it appears in the format string.
        //
        // E+ E- e+ e-   - Scientific notation.
        // If any of the strings 'E+', 'E-', 'e+', or 'e-' are present in the format
        // string and are immediately followed by at least one '0' character, then the
        // number is formatted using scientific notation with an 'E' or 'e' inserted
        // between the number and the exponent. The number of '0' characters following
        // the scientific notation indicator determines the minimum number of digits to
        // output for the exponent. The 'E+' and 'e+' formats indicate that a sign
        // character (plus or minus) should always precede the exponent. The 'E-' and
        // 'e-' formats indicate that a sign character should only precede negative
        // exponents.
        //
        // \ - Literal character. A backslash character
        // causes the next character in the format string to be copied to the output
        // string as-is. The backslash itself isn't copied, so to place a backslash
        // character in the output string, use two backslashes (\\) in the format
        // string.
        //
        // 'ABC' "ABC" - Literal string. Characters
        // enclosed in single or double quotation marks are copied to the output string
        // as-is and do not affect formatting.
        //
        // ; - Section separator. The ';' character is
        // used to separate sections for positive, negative, and zero numbers in the
        // format string.
        //
        // Other - All other characters are copied to
        // the output string in the position they appear.
        //
        // For fixed point formats (formats not containing an 'E+', 'E-', 'e+', or
        // 'e-'), the number is rounded to as many decimal places as there are digit
        // placeholders to the right of the decimal point. If the format string does
        // not contain a decimal point, the number is rounded to the nearest
        // integer. If the number has more digits than there are digit placeholders to
        // the left of the decimal point, the extra digits are copied to the output
        // string immediately before the first digit placeholder.
        //
        // For scientific formats, the number is rounded to as many significant digits
        // as there are digit placeholders in the format string.
        //
        // To allow for different formatting of positive, negative, and zero values, a
        // user-defined format string may contain up to three sections separated by
        // semicolons. The results of having one, two, or three sections in the format
        // string are described in the table below.
        //
        // Sections:
        //
        // One - The format string applies to all values.
        //
        // Two - The first section applies to positive values
        // and zeros, and the second section applies to negative values. If the number
        // to be formatted is negative, but becomes zero after rounding according to
        // the format in the second section, then the resulting zero is formatted
        // according to the first section.
        //
        // Three - The first section applies to positive
        // values, the second section applies to negative values, and the third section
        // applies to zeros. The second section may be left empty (by having no
        // characters between the semicolons), in which case the first section applies
        // to all non-zero values. If the number to be formatted is non-zero, but
        // becomes zero after rounding according to the format in the first or second
        // section, then the resulting zero is formatted according to the third
        // section.
        //
        // For both standard and user-defined formatting operations on values of type
        // float and double, if the value being formatted is a NaN (Not
        // a Number) or a positive or negative infinity, then regardless of the format
        // string, the resulting string is given by the NaNSymbol,
        // PositiveInfinitySymbol, or NegativeInfinitySymbol property of
        // the NumberFormatInfo used to format the number.
        //
        // Parsing
        //
        // The Parse methods provided by the numeric classes are all of the form
        //
        //  public static XXX Parse(String s);
        //  public static XXX Parse(String s, int style);
        //  public static XXX Parse(String s, int style, NumberFormatInfo info);
        //
        // where XXX is the name of the particular numeric class. The methods convert a
        // string to a numeric value. The optional style parameter specifies the
        // permitted style of the numeric string. It must be a combination of bit flags
        // from the NumberStyles enumeration. The optional info parameter
        // specifies the NumberFormatInfo instance to use when parsing the
        // string. If the info parameter is null or omitted, the numeric
        // formatting information is obtained from the current culture.
        //
        // Numeric strings produced by the Format methods using the Currency,
        // Decimal, Engineering, Fixed point, General, or Number standard formats
        // (the C, D, E, F, G, and N format specifiers) are guaranteed to be parseable
        // by the Parse methods if the NumberStyles.Any style is
        // specified. Note, however, that the Parse methods do not accept
        // NaNs or Infinities.
        //
        //This class contains only static members and does not need to be serializable 

        private partial class Number
        {
            private Number() { }

            // Constants used by number parsing
            private const Int32 NumberMaxDigits = 32;

            internal const int DECIMAL_PRECISION = 29; // Decimal.DecCalc also uses this value

            private const int MIN_SB_BUFFER_SIZE = 105;

            private static Boolean IsWhite(char ch)
            {
                return (((ch) == 0x20) || ((ch) >= 0x09 && (ch) <= 0x0D));
            }

            [System.Security.SecurityCritical]  // auto-generated
            private unsafe static char* MatchChars(char* p, string str)
            {
                fixed (char* stringPointer = str)
                {
                    return MatchChars(p, stringPointer);
                }
            }
            [System.Security.SecurityCritical]  // auto-generated
            private unsafe static char* MatchChars(char* p, char* str)
            {
                Debug.Assert(p != null && str != null);

                if (*str == '\0')
                {
                    return null;
                }
                for (; (*str != '\0'); p++, str++)
                {
                    if (*p != *str)
                    { //We only hurt the failure case
                        if ((*str == '\u00A0') && (*p == '\u0020'))
                        {// This fix is for French or Kazakh cultures. Since a user cannot type 0xA0 as a 
                            // space character we use 0x20 space character instead to mean the same.
                            continue;
                        }
                        return null;
                    }
                }
                return p;
            }

            [System.Security.SecurityCritical]  // auto-generated
            private unsafe static Boolean ParseNumber(ref char* str, NumberStyles options, ref NumberBuffer number, StringBuilder sb, NumberFormatInfo numfmt, Boolean parseDecimal)
            {
                const Int32 StateSign = 0x0001;
                const Int32 StateParens = 0x0002;
                const Int32 StateDigits = 0x0004;
                const Int32 StateNonZero = 0x0008;
                const Int32 StateDecimal = 0x0010;
                const Int32 StateCurrency = 0x0020;

                number.scale = 0;
                number.sign = false;
                string decSep;                  // decimal separator from NumberFormatInfo.
                string groupSep;                // group separator from NumberFormatInfo.
                string currSymbol = null;       // currency symbol from NumberFormatInfo.

                string altdecSep = null;        // decimal separator from NumberFormatInfo as a decimal
                string altgroupSep = null;      // group separator from NumberFormatInfo as a decimal

                Boolean parsingCurrency = false;
                if ((options & NumberStyles.AllowCurrencySymbol) != 0)
                {
                    currSymbol = numfmt.CurrencySymbol;
                    // The idea here is to match the currency separators and on failure match the number separators to keep the perf of VB's IsNumeric fast.
                    // The values of decSep are setup to use the correct relevant separator (currency in the if part and decimal in the else part).
                    altdecSep = numfmt.NumberDecimalSeparator;
                    altgroupSep = numfmt.NumberGroupSeparator;
                    decSep = numfmt.CurrencyDecimalSeparator;
                    groupSep = numfmt.CurrencyGroupSeparator;
                    parsingCurrency = true;
                }
                else
                {
                    decSep = numfmt.NumberDecimalSeparator;
                    groupSep = numfmt.NumberGroupSeparator;
                }

                Int32 state = 0;
                Boolean signflag = false; // Cache the results of "options & PARSE_LEADINGSIGN && !(state & STATE_SIGN)" to avoid doing this twice
                Boolean bigNumber = (sb != null); // When a StringBuilder is provided then we use it in place of the number.digits char[50]
                Boolean bigNumberHex = (bigNumber && ((options & NumberStyles.AllowHexSpecifier) != 0));
                Int32 maxParseDigits = bigNumber ? Int32.MaxValue : NumberMaxDigits;

                char* p = str;
                char ch = *p;
                char* next;

                while (true)
                {
                    // Eat whitespace unless we've found a sign which isn't followed by a currency symbol.
                    // "-Kr 1231.47" is legal but "- 1231.47" is not.
                    if (IsWhite(ch) && ((options & NumberStyles.AllowLeadingWhite) != 0) && (((state & StateSign) == 0) || (((state & StateSign) != 0) && (((state & StateCurrency) != 0) || numfmt.NumberNegativePattern == 2))))
                    {
                        // Do nothing here. We will increase p at the end of the loop.
                    }
                    else if ((signflag = (((options & NumberStyles.AllowLeadingSign) != 0) && ((state & StateSign) == 0))) && ((next = MatchChars(p, numfmt.PositiveSign)) != null))
                    {
                        state |= StateSign;
                        p = next - 1;
                    }
                    else if (signflag && (next = MatchChars(p, numfmt.NegativeSign)) != null)
                    {
                        state |= StateSign;
                        number.sign = true;
                        p = next - 1;
                    }
                    else if (ch == '(' && ((options & NumberStyles.AllowParentheses) != 0) && ((state & StateSign) == 0))
                    {
                        state |= StateSign | StateParens;
                        number.sign = true;
                    }
                    else if (currSymbol != null && (next = MatchChars(p, currSymbol)) != null)
                    {
                        state |= StateCurrency;
                        currSymbol = null;
                        // We already found the currency symbol. There should not be more currency symbols. Set
                        // currSymbol to NULL so that we won't search it again in the later code path.
                        p = next - 1;
                    }
                    else
                    {
                        break;
                    }
                    ch = *++p;
                }
                Int32 digCount = 0;
                Int32 digEnd = 0;
                while (true)
                {
                    if ((ch >= '0' && ch <= '9') || (((options & NumberStyles.AllowHexSpecifier) != 0) && ((ch >= 'a' && ch <= 'f') || (ch >= 'A' && ch <= 'F'))))
                    {
                        state |= StateDigits;

                        if (ch != '0' || (state & StateNonZero) != 0 || bigNumberHex)
                        {
                            if (digCount < maxParseDigits)
                            {
                                if (bigNumber)
                                    sb.Append(ch);
                                else
                                    number.digits[digCount++] = ch;
                                if (ch != '0' || parseDecimal)
                                {
                                    digEnd = digCount;
                                }
                            }
                            if ((state & StateDecimal) == 0)
                            {
                                number.scale++;
                            }
                            state |= StateNonZero;
                        }
                        else if ((state & StateDecimal) != 0)
                        {
                            number.scale--;
                        }
                    }
                    else if (((options & NumberStyles.AllowDecimalPoint) != 0) && ((state & StateDecimal) == 0) && ((next = MatchChars(p, decSep)) != null || ((parsingCurrency) && (state & StateCurrency) == 0) && (next = MatchChars(p, altdecSep)) != null))
                    {
                        state |= StateDecimal;
                        p = next - 1;
                    }
                    else if (((options & NumberStyles.AllowThousands) != 0) && ((state & StateDigits) != 0) && ((state & StateDecimal) == 0) && ((next = MatchChars(p, groupSep)) != null || ((parsingCurrency) && (state & StateCurrency) == 0) && (next = MatchChars(p, altgroupSep)) != null))
                    {
                        p = next - 1;
                    }
                    else
                    {
                        break;
                    }
                    ch = *++p;
                }

                Boolean negExp = false;
                number.precision = digEnd;
                if (bigNumber)
                    sb.Append('\0');
                else
                    number.digits[digEnd] = '\0';
                if ((state & StateDigits) != 0)
                {
                    if ((ch == 'E' || ch == 'e') && ((options & NumberStyles.AllowExponent) != 0))
                    {
                        char* temp = p;
                        ch = *++p;
                        if ((next = MatchChars(p, numfmt.PositiveSign)) != null)
                        {
                            ch = *(p = next);
                        }
                        else if ((next = MatchChars(p, numfmt.NegativeSign)) != null)
                        {
                            ch = *(p = next);
                            negExp = true;
                        }
                        if (ch >= '0' && ch <= '9')
                        {
                            Int32 exp = 0;
                            do
                            {
                                exp = exp * 10 + (ch - '0');
                                ch = *++p;
                                if (exp > 1000)
                                {
                                    exp = 9999;
                                    while (ch >= '0' && ch <= '9')
                                    {
                                        ch = *++p;
                                    }
                                }
                            } while (ch >= '0' && ch <= '9');
                            if (negExp)
                            {
                                exp = -exp;
                            }
                            number.scale += exp;
                        }
                        else
                        {
                            p = temp;
                            ch = *p;
                        }
                    }
                    while (true)
                    {
                        if (IsWhite(ch) && ((options & NumberStyles.AllowTrailingWhite) != 0))
                        {
                        }
                        else if ((signflag = (((options & NumberStyles.AllowTrailingSign) != 0) && ((state & StateSign) == 0))) && (next = MatchChars(p, numfmt.PositiveSign)) != null)
                        {
                            state |= StateSign;
                            p = next - 1;
                        }
                        else if (signflag && (next = MatchChars(p, numfmt.NegativeSign)) != null)
                        {
                            state |= StateSign;
                            number.sign = true;
                            p = next - 1;
                        }
                        else if (ch == ')' && ((state & StateParens) != 0))
                        {
                            state &= ~StateParens;
                        }
                        else if (currSymbol != null && (next = MatchChars(p, currSymbol)) != null)
                        {
                            currSymbol = null;
                            p = next - 1;
                        }
                        else
                        {
                            break;
                        }
                        ch = *++p;
                    }
                    if ((state & StateParens) == 0)
                    {
                        if ((state & StateNonZero) == 0)
                        {
                            if (!parseDecimal)
                            {
                                number.scale = 0;
                            }
                            if ((state & StateDecimal) == 0)
                            {
                                number.sign = false;
                            }
                        }
                        str = p;
                        return true;
                    }
                }
                str = p;
                return false;
            }

            private static Boolean TrailingZeros(String s, Int32 index)
            {
                // For compatability, we need to allow trailing zeros at the end of a number string
                for (int i = index; i < s.Length; i++)
                {
                    if (s[i] != '\0')
                    {
                        return false;
                    }
                }
                return true;
            }

            [System.Security.SecuritySafeCritical]  // auto-generated
            internal unsafe static Boolean TryStringToNumber(String str, NumberStyles options, ref NumberBuffer number, StringBuilder sb, NumberFormatInfo numfmt, Boolean parseDecimal)
            {
                if (str == null)
                {
                    return false;
                }
                Debug.Assert(numfmt != null);

                fixed (char* stringPointer = str)
                {
                    char* p = stringPointer;
                    if (!ParseNumber(ref p, options, ref number, sb, numfmt, parseDecimal)
                        || (p - stringPointer < str.Length && !TrailingZeros(str, (int)(p - stringPointer))))
                    {
                        return false;
                    }
                }

                return true;
            }

            // **********************************************************************************************************
            //
            // The remaining code in this module is an almost direct translation from the original unmanaged version in
            // the CLR. The code uses NumberBuffer directly instead of an analog of the NUMBER unmanaged data structure
            // but this causes next to no differences since we've modified NumberBuffer to take account of the changes (it
            // has an inline array of digits and no need of a pack operation to prepare for use by the "unmanaged" code).
            //
            // Some minor cleanup has been done (e.g. taking advantage of StringBuilder instead of having to precompute
            // string buffer sizes) but there's still plenty of opportunity to further C#'ize this code and potentially
            // better unify it with the code above.
            //

            private static string[] s_posCurrencyFormats =
            {
                "$#", "#$", "$ #", "# $"
            };

            private static string[] s_negCurrencyFormats =
            {
                "($#)", "-$#", "$-#", "$#-",
                "(#$)", "-#$", "#-$", "#$-",
                "-# $", "-$ #", "# $-", "$ #-",
                "$ -#", "#- $", "($ #)", "(# $)"
            };

            private static string[] s_posPercentFormats =
            {
                "# %", "#%", "%#", "% #"                // Last one is new in Whidbey
            };

            private static string[] s_negPercentFormats =
            {
                "-# %", "-#%", "-%#",
                "%-#", "%#-",                        // Last 9 are new in WHidbey
                "#-%", "#%-",
                "-% #", "# %-", "% #-",
                "% -#", "#- %"
            };

            private static string[] s_negNumberFormats =
            {
                "(#)", "-#", "- #", "#-", "# -",
            };

            private static string s_posNumberFormat = "#";

            [SecurityCritical]
            internal unsafe static void Int32ToDecChars(char* buffer, ref int index, uint value, int digits)
            {
                while (--digits >= 0 || value != 0)
                {
                    buffer[--index] = (char)(value % 10 + '0');
                    value /= 10;
                }
            }

            [SecurityCritical]
            internal static unsafe char ParseFormatSpecifier(string format, out int digits)
            {
                if (format != null)
                {
                    fixed (char* pFormat = format)
                    {
                        int i = 0;
                        char ch = pFormat[i];
                        if (ch != 0)
                        {
                            if (((ch >= 'A') && (ch <= 'Z')) || ((ch >= 'a') && (ch <= 'z')))
                            {
                                i++;
                                int n = -1;
                                if ((pFormat[i] >= '0') && (pFormat[i] <= '9'))
                                {
                                    n = pFormat[i++] - '0';
                                    while ((pFormat[i] >= '0') && (pFormat[i] <= '9'))
                                    {
                                        n = (n * 10) + pFormat[i++] - '0';
                                        if (n >= 10)
                                            break;
                                    }
                                }
                                if (pFormat[i] == 0)
                                {
                                    digits = n;
                                    return ch;
                                }
                            }

                            digits = -1;
                            return '\0';
                        }
                    }
                }

                digits = -1;
                return 'G';
            }

            [SecurityCritical]
            internal static unsafe string NumberToString(NumberBuffer number, char format, int nMaxDigits, NumberFormatInfo info, bool isDecimal)
            {
                int nMinDigits = -1;

                StringBuilder sb = new StringBuilder(MIN_SB_BUFFER_SIZE);

                switch (format)
                {
                    case 'C':
                    case 'c':
                        {
                            nMinDigits = nMaxDigits >= 0 ? nMaxDigits : info.CurrencyDecimalDigits;
                            if (nMaxDigits < 0)
                                nMaxDigits = info.CurrencyDecimalDigits;

                            RoundNumber(ref number, number.scale + nMaxDigits); // Don't change this line to use digPos since digCount could have its sign changed.

                            FormatCurrency(sb, number, nMinDigits, nMaxDigits, info);

                            break;
                        }

                    case 'F':
                    case 'f':
                        {
                            if (nMaxDigits < 0)
                                nMaxDigits = nMinDigits = info.NumberDecimalDigits;
                            else
                                nMinDigits = nMaxDigits;

                            RoundNumber(ref number, number.scale + nMaxDigits);

                            if (number.sign)
                                sb.Append(info.NegativeSign);

                            FormatFixed(sb, number, nMinDigits, nMaxDigits, info, null, info.NumberDecimalSeparator, null);

                            break;
                        }

                    case 'N':
                    case 'n':
                        {
                            if (nMaxDigits < 0)
                                nMaxDigits = nMinDigits = info.NumberDecimalDigits; // Since we are using digits in our calculation
                            else
                                nMinDigits = nMaxDigits;

                            RoundNumber(ref number, number.scale + nMaxDigits);

                            FormatNumber(sb, number, nMinDigits, nMaxDigits, info);

                            break;
                        }

                    case 'E':
                    case 'e':
                        {
                            if (nMaxDigits < 0)
                                nMaxDigits = nMinDigits = 6;
                            else
                                nMinDigits = nMaxDigits;
                            nMaxDigits++;

                            RoundNumber(ref number, nMaxDigits);

                            if (number.sign)
                                sb.Append(info.NegativeSign);

                            FormatScientific(sb, number, nMinDigits, nMaxDigits, info, format);

                            break;
                        }

                    case 'G':
                    case 'g':
                        {
                            bool enableRounding = true;
                            if (nMaxDigits < 1)
                            {
                                if (isDecimal && (nMaxDigits == -1))
                                {
                                    // Default to 29 digits precision only for G formatting without a precision specifier
                                    // This ensures that the PAL code pads out to the correct place even when we use the default precision
                                    nMaxDigits = nMinDigits = DECIMAL_PRECISION;
                                    enableRounding = false;  // Turn off rounding for ECMA compliance to output trailing 0's after decimal as significant
                                }
                                else
                                {
                                    // This ensures that the PAL code pads out to the correct place even when we use the default precision
                                    nMaxDigits = nMinDigits = number.precision;
                                }
                            }
                            else
                                nMinDigits = nMaxDigits;

                            if (enableRounding) // Don't round for G formatting without precision
                                RoundNumber(ref number, nMaxDigits); // This also fixes up the minus zero case
                            else
                            {
                                if (isDecimal && (number.digits[0] == 0))
                                {
                                    // Minus zero should be formatted as 0
                                    number.sign = false;
                                }
                            }

                            if (number.sign)
                                sb.Append(info.NegativeSign);

                            FormatGeneral(sb, number, nMinDigits, nMaxDigits, info, (char)(format - ('G' - 'E')), !enableRounding);

                            break;
                        }

                    case 'P':
                    case 'p':
                        {
                            if (nMaxDigits < 0)
                                nMaxDigits = nMinDigits = info.PercentDecimalDigits;
                            else
                                nMinDigits = nMaxDigits;
                            number.scale += 2;

                            RoundNumber(ref number, number.scale + nMaxDigits);

                            FormatPercent(sb, number, nMinDigits, nMaxDigits, info);

                            break;
                        }

                    default:
                        throw new FormatException(SR.Argument_BadFormatSpecifier);
                }

                return sb.ToString();
            }

            [SecuritySafeCritical]
            private static void FormatCurrency(StringBuilder sb, NumberBuffer number, int nMinDigits, int nMaxDigits, NumberFormatInfo info)
            {
                string fmt = number.sign ?
                    s_negCurrencyFormats[info.CurrencyNegativePattern] :
                    s_posCurrencyFormats[info.CurrencyPositivePattern];

                foreach (char ch in fmt)
                {
                    switch (ch)
                    {
                        case '#':
                            FormatFixed(sb, number, nMinDigits, nMaxDigits, info, info.CurrencyGroupSizes, info.CurrencyDecimalSeparator, info.CurrencyGroupSeparator);
                            break;
                        case '-':
                            sb.Append(info.NegativeSign);
                            break;
                        case '$':
                            sb.Append(info.CurrencySymbol);
                            break;
                        default:
                            sb.Append(ch);
                            break;
                    }
                }
            }

            [SecurityCritical]
            private static unsafe int wcslen(char* s)
            {
                int result = 0;
                while (*s++ != '\0')
                    result++;
                return result;
            }

            [SecurityCritical]
            private static unsafe void FormatFixed(StringBuilder sb, NumberBuffer number, int nMinDigits, int nMaxDigits, NumberFormatInfo info, int[] groupDigits, string sDecimal, string sGroup)
            {
                int digPos = number.scale;
                char* dig = number.digits;
                int digLength = wcslen(dig);

                if (digPos > 0)
                {
                    if (groupDigits != null)
                    {
                        int groupSizeIndex = 0;                             // index into the groupDigits array.
                        int groupSizeCount = groupDigits[groupSizeIndex];   // the current total of group size.
                        int groupSizeLen = groupDigits.Length;            // the length of groupDigits array.
                        int bufferSize = digPos;                        // the length of the result buffer string.
                        int groupSeparatorLen = sGroup.Length;              // the length of the group separator string.
                        int groupSize = 0;                                  // the current group size.

                        //
                        // Find out the size of the string buffer for the result.
                        //
                        if (groupSizeLen != 0) // You can pass in 0 length arrays
                        {
                            while (digPos > groupSizeCount)
                            {
                                groupSize = groupDigits[groupSizeIndex];
                                if (groupSize == 0)
                                    break;

                                bufferSize += groupSeparatorLen;
                                if (groupSizeIndex < groupSizeLen - 1)
                                    groupSizeIndex++;

                                groupSizeCount += groupDigits[groupSizeIndex];
                                if (groupSizeCount < 0 || bufferSize < 0)
                                    throw new ArgumentOutOfRangeException(); // if we overflow
                            }
                            if (groupSizeCount == 0) // If you passed in an array with one entry as 0, groupSizeCount == 0
                                groupSize = 0;
                            else
                                groupSize = groupDigits[0];
                        }

                        char* tmpBuffer = stackalloc char[bufferSize];
                        groupSizeIndex = 0;
                        int digitCount = 0;
                        int digStart;
                        digStart = (digPos < digLength) ? digPos : digLength;
                        char* p = tmpBuffer + bufferSize - 1;
                        for (int i = digPos - 1; i >= 0; i--)
                        {
                            *(p--) = (i < digStart) ? dig[i] : '0';

                            if (groupSize > 0)
                            {
                                digitCount++;
                                if ((digitCount == groupSize) && (i != 0))
                                {
                                    for (int j = groupSeparatorLen - 1; j >= 0; j--)
                                        *(p--) = sGroup[j];

                                    if (groupSizeIndex < groupSizeLen - 1)
                                    {
                                        groupSizeIndex++;
                                        groupSize = groupDigits[groupSizeIndex];
                                    }
                                    digitCount = 0;
                                }
                            }
                        }

                        sb.Append(tmpBuffer, bufferSize);
                        dig += digStart;
                    }
                    else
                    {
                        int digits = Math.Min(digLength, digPos);
                        sb.Append(dig, digits);
                        dig += digits;
                        if (digPos > digLength)
                            sb.Append('0', digPos - digLength);
                    }
                }
                else
                {
                    sb.Append('0');
                }

                if (nMaxDigits > 0)
                {
                    sb.Append(sDecimal);
                    if ((digPos < 0) && (nMaxDigits > 0))
                    {
                        int zeroes = Math.Min(-digPos, nMaxDigits);
                        sb.Append('0', zeroes);
                        digPos += zeroes;
                        nMaxDigits -= zeroes;
                    }

                    while (nMaxDigits > 0)
                    {
                        sb.Append((*dig != 0) ? *dig++ : '0');
                        nMaxDigits--;
                    }
                }
            }

            [SecuritySafeCritical]
            private static void FormatNumber(StringBuilder sb, NumberBuffer number, int nMinDigits, int nMaxDigits, NumberFormatInfo info)
            {
                string fmt = number.sign ?
                    s_negNumberFormats[info.NumberNegativePattern] :
                    s_posNumberFormat;

                foreach (char ch in fmt)
                {
                    switch (ch)
                    {
                        case '#':
                            FormatFixed(sb, number, nMinDigits, nMaxDigits, info, info.NumberGroupSizes, info.NumberDecimalSeparator, info.NumberGroupSeparator);
                            break;
                        case '-':
                            sb.Append(info.NegativeSign);
                            break;
                        default:
                            sb.Append(ch);
                            break;
                    }
                }
            }

            [SecurityCritical]
            private static unsafe void FormatScientific(StringBuilder sb, NumberBuffer number, int nMinDigits, int nMaxDigits, NumberFormatInfo info, char expChar)
            {
                char* dig = number.digits;

                sb.Append((*dig != 0) ? *dig++ : '0');

                if (nMaxDigits != 1) // For E0 we would like to suppress the decimal point
                    sb.Append(info.NumberDecimalSeparator);

                while (--nMaxDigits > 0)
                    sb.Append((*dig != 0) ? *dig++ : '0');

                int e = number.digits[0] == 0 ? 0 : number.scale - 1;
                FormatExponent(sb, info, e, expChar, 3, true);
            }

            [SecurityCritical]
            private static unsafe void FormatExponent(StringBuilder sb, NumberFormatInfo info, int value, char expChar, int minDigits, bool positiveSign)
            {
                sb.Append(expChar);

                if (value < 0)
                {
                    sb.Append(info.NegativeSign);
                    value = -value;
                }
                else
                {
                    if (positiveSign)
                        sb.Append(info.PositiveSign);
                }

                char* digits = stackalloc char[11];
                int index = 10;
                Int32ToDecChars(digits, ref index, (uint)value, minDigits);
                int i = 10 - index;
                while (--i >= 0)
                    sb.Append(digits[index++]);
            }

            [SecurityCritical]
            private static unsafe void FormatGeneral(StringBuilder sb, NumberBuffer number, int nMinDigits, int nMaxDigits, NumberFormatInfo info, char expChar, bool bSuppressScientific)
            {
                int digPos = number.scale;
                bool scientific = false;

                if (!bSuppressScientific)
                {
                    // Don't switch to scientific notation
                    if (digPos > nMaxDigits || digPos < -3)
                    {
                        digPos = 1;
                        scientific = true;
                    }
                }

                char* dig = number.digits;

                if (digPos > 0)
                {
                    do
                    {
                        sb.Append((*dig != 0) ? *dig++ : '0');
                    } while (--digPos > 0);
                }
                else
                {
                    sb.Append('0');
                }

                if (*dig != 0 || digPos < 0)
                {
                    sb.Append(info.NumberDecimalSeparator);

                    while (digPos < 0)
                    {
                        sb.Append('0');
                        digPos++;
                    }

                    while (*dig != 0)
                        sb.Append(*dig++);
                }

                if (scientific)
                    FormatExponent(sb, info, number.scale - 1, expChar, 2, true);
            }

            [SecuritySafeCritical]
            private static void FormatPercent(StringBuilder sb, NumberBuffer number, int nMinDigits, int nMaxDigits, NumberFormatInfo info)
            {
                string fmt = number.sign ?
                    s_negPercentFormats[info.PercentNegativePattern] :
                    s_posPercentFormats[info.PercentPositivePattern];

                foreach (char ch in fmt)
                {
                    switch (ch)
                    {
                        case '#':
                            FormatFixed(sb, number, nMinDigits, nMaxDigits, info, info.PercentGroupSizes, info.PercentDecimalSeparator, info.PercentGroupSeparator);
                            break;
                        case '-':
                            sb.Append(info.NegativeSign);
                            break;
                        case '%':
                            sb.Append(info.PercentSymbol);
                            break;
                        default:
                            sb.Append(ch);
                            break;
                    }
                }
            }

            [SecurityCritical]
            private static unsafe void RoundNumber(ref NumberBuffer number, int pos)
            {
                int i = 0;
                while (i < pos && number.digits[i] != 0)
                    i++;

                if (i == pos && number.digits[i] >= '5')
                {
                    while (i > 0 && number.digits[i - 1] == '9')
                        i--;

                    if (i > 0)
                    {
                        number.digits[i - 1]++;
                    }
                    else
                    {
                        number.scale++;
                        number.digits[0] = '1';
                        i = 1;
                    }
                }
                else
                {
                    while (i > 0 && number.digits[i - 1] == '0')
                        i--;
                }
                if (i == 0)
                {
                    number.scale = 0;
                    number.sign = false;
                }
                number.digits[i] = '\0';
            }

            [SecurityCritical]
            private static unsafe int FindSection(string format, int section)
            {
                int src;
                char ch;

                if (section == 0)
                    return 0;

                fixed (char* pFormat = format)
                {
                    src = 0;
                    for (; ;)
                    {
                        switch (ch = pFormat[src++])
                        {
                            case '\'':
                            case '"':
                                while (pFormat[src] != 0 && pFormat[src++] != ch)
                                    ;
                                break;
                            case '\\':
                                if (pFormat[src] != 0)
                                    src++;
                                break;
                            case ';':
                                if (--section != 0)
                                    break;
                                if (pFormat[src] != 0 && pFormat[src] != ';')
                                    return src;
                                goto case '\0';
                            case '\0':
                                return 0;
                        }
                    }
                }
            }

            [SecurityCritical]
            internal static unsafe string NumberToStringFormat(NumberBuffer number, string format, NumberFormatInfo info)
            {
                int digitCount;
                int decimalPos;
                int firstDigit;
                int lastDigit;
                int digPos;
                bool scientific;
                int thousandPos;
                int thousandCount = 0;
                bool thousandSeps;
                int scaleAdjust;
                int adjust;

                int section;
                int src;
                char* dig;
                char ch;

                section = FindSection(format, number.digits[0] == 0 ? 2 : number.sign ? 1 : 0);

                while (true)
                {
                    digitCount = 0;
                    decimalPos = -1;
                    firstDigit = 0x7FFFFFFF;
                    lastDigit = 0;
                    scientific = false;
                    thousandPos = -1;
                    thousandSeps = false;
                    scaleAdjust = 0;
                    src = section;

                    fixed (char* pFormat = format)
                    {
                        while ((ch = pFormat[src++]) != 0 && ch != ';')
                        {
                            switch (ch)
                            {
                                case '#':
                                    digitCount++;
                                    break;
                                case '0':
                                    if (firstDigit == 0x7FFFFFFF)
                                        firstDigit = digitCount;
                                    digitCount++;
                                    lastDigit = digitCount;
                                    break;
                                case '.':
                                    if (decimalPos < 0)
                                        decimalPos = digitCount;
                                    break;
                                case ',':
                                    if (digitCount > 0 && decimalPos < 0)
                                    {
                                        if (thousandPos >= 0)
                                        {
                                            if (thousandPos == digitCount)
                                            {
                                                thousandCount++;
                                                break;
                                            }
                                            thousandSeps = true;
                                        }
                                        thousandPos = digitCount;
                                        thousandCount = 1;
                                    }
                                    break;
                                case '%':
                                    scaleAdjust += 2;
                                    break;
                                case '\x2030':
                                    scaleAdjust += 3;
                                    break;
                                case '\'':
                                case '"':
                                    while (pFormat[src] != 0 && pFormat[src++] != ch)
                                        ;
                                    break;
                                case '\\':
                                    if (pFormat[src] != 0)
                                        src++;
                                    break;
                                case 'E':
                                case 'e':
                                    if (pFormat[src] == '0' || ((pFormat[src] == '+' || pFormat[src] == '-') && pFormat[src + 1] == '0'))
                                    {
                                        while (pFormat[++src] == '0')
                                            ;
                                        scientific = true;
                                    }
                                    break;
                            }
                        }
                    }

                    if (decimalPos < 0)
                        decimalPos = digitCount;

                    if (thousandPos >= 0)
                    {
                        if (thousandPos == decimalPos)
                            scaleAdjust -= thousandCount * 3;
                        else
                            thousandSeps = true;
                    }

                    if (number.digits[0] != 0)
                    {
                        number.scale += scaleAdjust;
                        int pos = scientific ? digitCount : number.scale + digitCount - decimalPos;
                        RoundNumber(ref number, pos);
                        if (number.digits[0] == 0)
                        {
                            src = FindSection(format, 2);
                            if (src != section)
                            {
                                section = src;
                                continue;
                            }
                        }
                    }
                    else
                    {
                        number.sign = false;   // We need to format -0 without the sign set.
                        number.scale = 0;      // Decimals with scale ('0.00') should be rounded.
                    }

                    break;
                }

                firstDigit = firstDigit < decimalPos ? decimalPos - firstDigit : 0;
                lastDigit = lastDigit > decimalPos ? decimalPos - lastDigit : 0;
                if (scientific)
                {
                    digPos = decimalPos;
                    adjust = 0;
                }
                else
                {
                    digPos = number.scale > decimalPos ? number.scale : decimalPos;
                    adjust = number.scale - decimalPos;
                }
                src = section;
                dig = number.digits;

                // Adjust can be negative, so we make this an int instead of an unsigned int.
                // Adjust represents the number of characters over the formatting eg. format string is "0000" and you are trying to
                // format 100000 (6 digits). Means adjust will be 2. On the other hand if you are trying to format 10 adjust will be
                // -2 and we'll need to fixup these digits with 0 padding if we have 0 formatting as in this example.
                int[] thousandsSepPos = new int[4];
                int thousandsSepCtr = -1;

                if (thousandSeps)
                {
                    // We need to precompute this outside the number formatting loop
                    if (info.NumberGroupSeparator.Length > 0)
                    {
                        // <EMAIL>rajeshc</EMAIL> - We need this array to figure out where to insert the thousands seperator. We would have to traverse the string
                        // backwords. PIC formatting always traverses forwards. These indices are precomputed to tell us where to insert
                        // the thousands seperator so we can get away with traversing forwards. Note we only have to compute upto digPos.
                        // The max is not bound since you can have formatting strings of the form "000,000..", and this
                        // should handle that case too.

                        int[] groupDigits = info.NumberGroupSizes;

                        int groupSizeIndex = 0;     // index into the groupDigits array.
                        int groupTotalSizeCount = 0;
                        int groupSizeLen = groupDigits.Length;    // the length of groupDigits array.
                        if (groupSizeLen != 0)
                            groupTotalSizeCount = groupDigits[groupSizeIndex];   // the current running total of group size.
                        int groupSize = groupTotalSizeCount;

                        int totalDigits = digPos + ((adjust < 0) ? adjust : 0); // actual number of digits in o/p
                        int numDigits = (firstDigit > totalDigits) ? firstDigit : totalDigits;
                        while (numDigits > groupTotalSizeCount)
                        {
                            if (groupSize == 0)
                                break;
                            ++thousandsSepCtr;
                            if (thousandsSepCtr >= thousandsSepPos.Length)
                                Array.Resize(ref thousandsSepPos, thousandsSepPos.Length * 2);

                            thousandsSepPos[thousandsSepCtr] = groupTotalSizeCount;
                            if (groupSizeIndex < groupSizeLen - 1)
                            {
                                groupSizeIndex++;
                                groupSize = groupDigits[groupSizeIndex];
                            }
                            groupTotalSizeCount += groupSize;
                        }
                    }
                }

                StringBuilder sb = new StringBuilder(MIN_SB_BUFFER_SIZE);

                if (number.sign && section == 0)
                    sb.Append(info.NegativeSign);

                bool decimalWritten = false;

                fixed (char* pFormat = format)
                {
                    while ((ch = pFormat[src++]) != 0 && ch != ';')
                    {
                        if (adjust > 0)
                        {
                            switch (ch)
                            {
                                case '#':
                                case '0':
                                case '.':
                                    while (adjust > 0)
                                    {
                                        // digPos will be one greater than thousandsSepPos[thousandsSepCtr] since we are at
                                        // the character after which the groupSeparator needs to be appended.
                                        sb.Append(*dig != 0 ? *dig++ : '0');
                                        if (thousandSeps && digPos > 1 && thousandsSepCtr >= 0)
                                        {
                                            if (digPos == thousandsSepPos[thousandsSepCtr] + 1)
                                            {
                                                sb.Append(info.NumberGroupSeparator);
                                                thousandsSepCtr--;
                                            }
                                        }
                                        digPos--;
                                        adjust--;
                                    }
                                    break;
                            }
                        }

                        switch (ch)
                        {
                            case '#':
                            case '0':
                                {
                                    if (adjust < 0)
                                    {
                                        adjust++;
                                        ch = digPos <= firstDigit ? '0' : '\0';
                                    }
                                    else
                                    {
                                        ch = *dig != 0 ? *dig++ : digPos > lastDigit ? '0' : '\0';
                                    }
                                    if (ch != 0)
                                    {
                                        sb.Append(ch);
                                        if (thousandSeps && digPos > 1 && thousandsSepCtr >= 0)
                                        {
                                            if (digPos == thousandsSepPos[thousandsSepCtr] + 1)
                                            {
                                                sb.Append(info.NumberGroupSeparator);
                                                thousandsSepCtr--;
                                            }
                                        }
                                    }

                                    digPos--;
                                    break;
                                }
                            case '.':
                                {
                                    if (digPos != 0 || decimalWritten)
                                    {
                                        // For compatability, don't echo repeated decimals
                                        break;
                                    }
                                    // If the format has trailing zeros or the format has a decimal and digits remain
                                    if (lastDigit < 0 || (decimalPos < digitCount && *dig != 0))
                                    {
                                        sb.Append(info.NumberDecimalSeparator);
                                        decimalWritten = true;
                                    }
                                    break;
                                }
                            case '\x2030':
                                sb.Append(info.PerMilleSymbol);
                                break;
                            case '%':
                                sb.Append(info.PercentSymbol);
                                break;
                            case ',':
                                break;
                            case '\'':
                            case '"':
                                while (pFormat[src] != 0 && pFormat[src] != ch)
                                    sb.Append(pFormat[src++]);
                                if (pFormat[src] != 0)
                                    src++;
                                break;
                            case '\\':
                                if (pFormat[src] != 0)
                                    sb.Append(pFormat[src++]);
                                break;
                            case 'E':
                            case 'e':
                                {
                                    bool positiveSign = false;
                                    int i = 0;
                                    if (scientific)
                                    {
                                        if (pFormat[src] == '0')
                                        {
                                            //Handles E0, which should format the same as E-0
                                            i++;
                                        }
                                        else if (pFormat[src] == '+' && pFormat[src + 1] == '0')
                                        {
                                            //Handles E+0
                                            positiveSign = true;
                                        }
                                        else if (pFormat[src] == '-' && pFormat[src + 1] == '0')
                                        {
                                            //Handles E-0
                                            //Do nothing, this is just a place holder s.t. we don't break out of the loop.
                                        }
                                        else
                                        {
                                            sb.Append(ch);
                                            break;
                                        }

                                        while (pFormat[++src] == '0')
                                            i++;
                                        if (i > 10)
                                            i = 10;

                                        int exp = number.digits[0] == 0 ? 0 : number.scale - decimalPos;
                                        FormatExponent(sb, info, exp, ch, i, positiveSign);
                                        scientific = false;
                                    }
                                    else
                                    {
                                        sb.Append(ch); // Copy E or e to output
                                        if (pFormat[src] == '+' || pFormat[src] == '-')
                                            sb.Append(pFormat[src++]);
                                        while (pFormat[src] == '0')
                                            sb.Append(pFormat[src++]);
                                    }
                                    break;
                                }
                            default:
                                sb.Append(ch);
                                break;
                        }
                    }
                }

                return sb.ToString();
            }
        }
    }
}

