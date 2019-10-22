using System;

namespace Integration.Common.Extensions
{
    public static class DateTimeExtension
    {
        private const string FORMAT_DATETIME = "dd-M-yyyy hh:mm:ss";

        private const string FORMAT_DATE = "dd/MM/yyyy";
        public static string ConvertFormatDateTime(DateTime? dateTime)
        {
            if (dateTime.HasValue && dateTime != DateTime.MinValue)
            {
                return dateTime.Value.ToString(FORMAT_DATETIME);
            }
            return string.Empty;
        }

        public static string ConvertFormatDate(DateTime? dateTime)
        {
            if (dateTime.HasValue && dateTime != DateTime.MinValue)
            {
                return dateTime.Value.ToString(FORMAT_DATE);
            }
            return string.Empty;
        }
    }
}
