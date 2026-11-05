namespace SRM.Api.Utils.Extensions
{
    public static class DateExtensions
    {
        /// Normaliza a medianoche UTC conservando el dia calendario.
        public static DateTime ToUtcDate(this DateTime d) =>
            DateTime.SpecifyKind(d.Date, DateTimeKind.Utc);
    }
}
