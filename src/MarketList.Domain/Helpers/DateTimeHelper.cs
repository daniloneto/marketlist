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
            if (value.Kind == DateTimeKind.Local)
                return value.ToUniversalTime();
            // Unspecified
            return DateTime.SpecifyKind(value, DateTimeKind.Utc);
        }

        public static DateTime EnsureUtc(DateTime dateTime)
        {
            if (dateTime.Kind == DateTimeKind.Utc)
                return dateTime;
            if (dateTime.Kind == DateTimeKind.Local)
                return dateTime.ToUniversalTime();
            // Unspecified
            return DateTime.SpecifyKind(dateTime, DateTimeKind.Utc);
        }
    }
}
