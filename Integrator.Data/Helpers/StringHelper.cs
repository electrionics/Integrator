using System.Text;
using System.Text.RegularExpressions;

namespace Integrator.Data.Helpers
{
    public class StringHelper
    {
        public static string RemoveExtraSymbols(string source, string separator = " ")
        {
            //return new string(source.Where(x => x < 128).ToArray());

            var filterRegex = new Regex("[a-zA-Zа-яА-Я0-9\\-\\+\\*\\.\\ _\\\\]+");
            var values = filterRegex.Matches(source).Select(x => x.Value).ToArray();
            var sb = new StringBuilder();

            for (var i = 0; i < values.Length - 1; i++)
            {
                var value = values[i].Trim();
                if (!string.IsNullOrEmpty(value))
                {
                    sb.Append(value);
                    sb.Append(separator);
                }
            }

            if (values.Length > 0)
            {
                sb.Append(values[^1]);
            }

            return sb.ToString();
        }

        public static string GetParentFolder(string folderPath, string folderName)
        {
            return folderPath.Replace(folderName, string.Empty).Trim('\\');
        }
    }
}
