using System;
using System.Globalization;
using System.Windows.Data;
using DBTeam.Core.Models;

namespace DBTeam.Modules.ConnectionManager.Converters;

public sealed class AuthModeDisplayConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture) => value switch
    {
        SqlAuthMode.Windows => "Windows Authentication",
        SqlAuthMode.SqlLogin => "SQL Server Authentication",
        SqlAuthMode.AzureActiveDirectoryIntegrated => "Azure AD - Integrated",
        SqlAuthMode.AzureActiveDirectoryPassword => "Azure AD - Password",
        _ => value?.ToString() ?? ""
    };
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => Binding.DoNothing;
}
