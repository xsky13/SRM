namespace SRM.Api.Utils
{
    public static class Escape
    {
        public static string EscapeLike(string s) => s.Replace("\\", "\\\\").Replace("%", "\\%").Replace("_", "\\_");
    }
}
