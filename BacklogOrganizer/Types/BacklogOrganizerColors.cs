using System.Windows.Media;


namespace BacklogOrganizer.Types
{
    internal class BacklogOrganizerColors
    {
        public static readonly string Green = "#90EE90";
        public static readonly string Red = "#FD5454";
        public static readonly string DarkRed = "#FF0000";
        public static readonly string Yellow = "#FFFF00";
        public static readonly string Header = "#7B7B7B";

        // ReSharper disable once PossibleNullReferenceException
        public static SolidColorBrush GreenColor => new SolidColorBrush((Color)ColorConverter.ConvertFromString(Green));
        // ReSharper disable once PossibleNullReferenceException
        public static SolidColorBrush RedColor => new SolidColorBrush((Color)ColorConverter.ConvertFromString(Red));
        // ReSharper disable once PossibleNullReferenceException
        public static SolidColorBrush DarkRedColor => new SolidColorBrush((Color)ColorConverter.ConvertFromString(DarkRed));
        // ReSharper disable once PossibleNullReferenceException
        public static SolidColorBrush YellowColor => new SolidColorBrush((Color)ColorConverter.ConvertFromString(Yellow));
        // ReSharper disable once PossibleNullReferenceException
        public static SolidColorBrush HeaderColor => new SolidColorBrush((Color)ColorConverter.ConvertFromString(Header));
    }
}
