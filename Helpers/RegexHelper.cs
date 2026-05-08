namespace KeplerCMS.Helpers;

public class RegexHelper
{
    public static string RegexSafeString(string input)
    {
        var usernameRegex = "";
        foreach (var t in input)
        {
            var character = t.ToString();
            if(character == "-") {
                character = "\\-";
            }
            usernameRegex += character;
        }
        var regexPattern = "^["+usernameRegex+"]*$";
        return regexPattern;
    }
}