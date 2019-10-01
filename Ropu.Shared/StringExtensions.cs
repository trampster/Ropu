namespace Ropu.Shared
{
    public static class StringExtensions
    {
        public static string EmptyIfNull(this string? text)
        {
            if(text == null) return "";
            return text;
        }
    }
}