using System;

namespace MagicApp.Helpers
{
    public static class FormatHelper
    {
        /// <summary>
        /// 格式化数字（添加千分位逗号）
        /// </summary>
        /// <param name="number">要格式化的数字</param>
        /// <returns>格式化后的字符串</returns>
        public static string FormatNumber(int number)
        {
            return number.ToString("N0");
        }

        /// <summary>
        /// 格式化数字（添加千分位逗号）- 长整型版本
        /// </summary>
        /// <param name="number">要格式化的长整型数字</param>
        /// <returns>格式化后的字符串</returns>
        public static string FormatNumber(long number)
        {
            return number.ToString("N0");
        }

        /// <summary>
        /// 格式化数字（添加千分位逗号）- 双精度版本
        /// </summary>
        /// <param name="number">要格式化的双精度数字</param>
        /// <param name="decimalPlaces">小数位数</param>
        /// <returns>格式化后的字符串</returns>
        public static string FormatNumber(double number, int decimalPlaces = 2)
        {
            string format = "N" + decimalPlaces.ToString();
            return number.ToString(format);
        }

        /// <summary>
        /// 格式化文件大小（B, KB, MB, GB, TB）
        /// </summary>
        /// <param name="bytes">字节数</param>
        /// <param name="decimalPlaces">小数位数</param>
        /// <returns>格式化后的文件大小字符串</returns>
        public static string FormatFileSize(long bytes, int decimalPlaces = 2)
        {
            if (bytes < 0)
            {
                return "0 B";
            }

            string[] sizes = { "B", "KB", "MB", "GB", "TB", "PB", "EB" };
            double len = bytes;
            int order = 0;

            while (len >= 1024 && order < sizes.Length - 1)
            {
                order++;
                len = len / 1024;
            }

            // 对于B单位，不使用小数
            if (order == 0)
            {
                return bytes.ToString() + " " + sizes[order];
            }

            string format = "F" + decimalPlaces.ToString();
            return len.ToString(format) + " " + sizes[order];
        }

        /// <summary>
        /// 格式化视频时长（秒转换为时:分:秒）
        /// </summary>
        /// <param name="seconds">总秒数</param>
        /// <returns>格式化后的时长字符串</returns>
        public static string FormatDuration(int seconds)
        {
            if (seconds < 0)
            {
                return "00:00";
            }

            TimeSpan timeSpan = TimeSpan.FromSeconds(seconds);

            if (timeSpan.TotalHours >= 1)
            {
                return timeSpan.ToString(@"hh\:mm\:ss");
            }
            else
            {
                return timeSpan.ToString(@"mm\:ss");
            }
        }

        /// <summary>
        /// 格式化视频时长（秒转换为时:分:秒）- 双精度版本
        /// </summary>
        /// <param name="seconds">总秒数（双精度）</param>
        /// <returns>格式化后的时长字符串</returns>
        public static string FormatDuration(double seconds)
        {
            if (seconds < 0)
            {
                return "00:00";
            }

            TimeSpan timeSpan = TimeSpan.FromSeconds(seconds);

            if (timeSpan.TotalHours >= 1)
            {
                return timeSpan.ToString(@"hh\:mm\:ss");
            }
            else
            {
                return timeSpan.ToString(@"mm\:ss");
            }
        }

        /// <summary>
        /// Unix时间戳转换为DateTime
        /// </summary>
        /// <param name="unixTimeStamp">Unix时间戳（秒）</param>
        /// <returns>转换后的DateTime</returns>
        public static DateTime UnixTimeStampToDateTime(long unixTimeStamp)
        {
            DateTime dateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
            dateTime = dateTime.AddSeconds(unixTimeStamp).ToLocalTime();
            return dateTime;
        }

        /// <summary>
        /// DateTime转换为Unix时间戳
        /// </summary>
        /// <param name="dateTime">要转换的DateTime</param>
        /// <returns>Unix时间戳（秒）</returns>
        public static long DateTimeToUnixTimeStamp(DateTime dateTime)
        {
            DateTimeOffset dto = new DateTimeOffset(dateTime);
            return dto.ToUnixTimeSeconds();
        }

        /// <summary>
        /// DateTime转换为Unix时间戳（毫秒）
        /// </summary>
        /// <param name="dateTime">要转换的DateTime</param>
        /// <returns>Unix时间戳（毫秒）</returns>
        public static long DateTimeToUnixTimeMilliseconds(DateTime dateTime)
        {
            DateTimeOffset dto = new DateTimeOffset(dateTime);
            return dto.ToUnixTimeMilliseconds();
        }

        /// <summary>
        /// Unix时间戳（毫秒）转换为DateTime
        /// </summary>
        /// <param name="unixTimeMilliseconds">Unix时间戳（毫秒）</param>
        /// <returns>转换后的DateTime</returns>
        public static DateTime UnixTimeMillisecondsToDateTime(long unixTimeMilliseconds)
        {
            DateTimeOffset dto = DateTimeOffset.FromUnixTimeMilliseconds(unixTimeMilliseconds);
            return dto.LocalDateTime;
        }

        /// <summary>
        /// 格式化DateTime为可读字符串
        /// </summary>
        /// <param name="dateTime">要格式化的DateTime</param>
        /// <param name="format">格式字符串，默认"yyyy-MM-dd HH:mm:ss"</param>
        /// <returns>格式化后的字符串</returns>
        public static string FormatDateTime(DateTime dateTime, string format = "yyyy-MM-dd HH:mm:ss")
        {
            return dateTime.ToString(format);
        }

        /// <summary>
        /// 格式化Unix时间戳为可读字符串
        /// </summary>
        /// <param name="unixTimeStamp">Unix时间戳（秒）</param>
        /// <param name="format">格式字符串，默认"yyyy-MM-dd HH:mm:ss"</param>
        /// <returns>格式化后的字符串</returns>
        public static string FormatUnixTimeStamp(long unixTimeStamp, string format = "yyyy-MM-dd HH:mm:ss")
        {
            DateTime dateTime = UnixTimeStampToDateTime(unixTimeStamp);
            return FormatDateTime(dateTime, format);
        }

        /// <summary>
        /// 截断文本并添加省略号
        /// </summary>
        /// <param name="text">原始文本</param>
        /// <param name="maxLength">最大长度</param>
        /// <returns>截断后的文本</returns>
        public static string TruncateText(string text, int maxLength)
        {
            if (string.IsNullOrEmpty(text) || text.Length <= maxLength)
                return text;

            return text.Substring(0, maxLength) + "...";
        }
    }
}