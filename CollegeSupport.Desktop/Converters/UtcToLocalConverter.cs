using System.Globalization;
using System.Windows.Data;

namespace CollegeSupport.Desktop.Converters;

/// <summary>Сервер хранит время в UTC; в таблице показываем местное время пользователя.</summary>
public class UtcToLocalConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture) =>
        value is DateTime dateTime ? dateTime.ToLocalTime() : value;

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
