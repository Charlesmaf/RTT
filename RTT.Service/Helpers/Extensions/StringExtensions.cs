namespace RTT.Service.Helpers.Extensions
{
    public static class StringExtensions
    {
        public static string CsvField(this string? value)
        {
            if (string.IsNullOrEmpty(value))
                return "";

            if ("=+-@".Contains(value[0]))
                value = "'" + value;

            return value.IndexOfAny([',', '"', '\r', '\n']) >= 0
                ? "\"" + value.Replace("\"", "\"\"") + "\""
                : value;
        }
    }
}
