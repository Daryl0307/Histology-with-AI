using System.Globalization;

namespace RP.SOI.DotNet.Utils;

public static class ValidUtl
{
    public static bool CheckIfEmpty(params string[] list)
    {
        foreach (string o in list)
        {
            if (o == null || o.Trim().Equals(""))
            {
                return true;
            }
        }
        return false;
    }

    // Extension Method to check whether the contents of a string is an integer
    public static bool IsInteger(this string x)
    {
        return int.TryParse(x, out _);
    }

    // Extension Method to check whether the contents of a string is a double
    public static bool IsNumeric(this string x)
    {
        return double.TryParse(x, out _);
    }

    // Extension Method to check whether the contents of a string 
    // is a date specified by the "format" 
    public static bool IsDate(this string x, string format)
    {
        // E.g. format = "yyyy-MM-dd"
        return DateTime.TryParseExact(x, format,
                        CultureInfo.InvariantCulture,
                        DateTimeStyles.None,
                        out _);
    }

    // Extension Method to convert the contents of a string 
    // to a date specified by the "format" 
    public static DateTime ToDate(this string x, string format)
    {
        _ = DateTime.TryParseExact(x, format,
                     CultureInfo.InvariantCulture,
                     DateTimeStyles.None,
                     out DateTime tempDT);
        return tempDT;
    }

}
