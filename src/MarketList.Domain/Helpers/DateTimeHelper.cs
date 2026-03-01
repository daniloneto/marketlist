namespace MarketList.Domain.Helpers
{
    public static class DateTimeHelper
    {
        public static DateTime? EnsureUtc(DateTime? dateTime)
        {
            if (!dateTime.HasValue)
                return null;

            var value = dateTime.Value;
            if (value.Kind == DateTimeKind.Utc)
                return value;

            return TimeZoneInfo.ConvertTimeToUtc(value);
        }

        public static DateTime EnsureUtc(DateTime dateTime)
        {
            if (dateTime.Kind == DateTimeKind.Utc)
                return dateTime;

            return TimeZoneInfo.ConvertTimeToUtc(dateTime);
        }
    }
}
