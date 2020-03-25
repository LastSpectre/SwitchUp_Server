using System.Text.RegularExpressions;

namespace BA_Praxis_Library
{
    public static class Helper
    {
        public static Regex regexItem = new Regex("^[a-zA-Z0-9_]*$");

        public static bool CheckForValidString(string _string)
        {
            // check if string is empty
            if (_string == "")
            {
                return false;
            }

            // check if string contains special characters
            if (regexItem.IsMatch(_string))
            {
                return true;
            }

            return false;
        }
    }
}