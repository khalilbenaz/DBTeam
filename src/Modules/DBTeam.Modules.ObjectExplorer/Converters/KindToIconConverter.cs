using System;
using System.Globalization;
using System.Windows.Data;
using DBTeam.Core.Models;
using MaterialDesignThemes.Wpf;

namespace DBTeam.Modules.ObjectExplorer.Converters;

public sealed class KindToIconConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture) => value switch
    {
        DbObjectKind.Server => PackIconKind.Server,
        DbObjectKind.Database => PackIconKind.Database,
        DbObjectKind.TableFolder => PackIconKind.Folder,
        DbObjectKind.Table => PackIconKind.Table,
        DbObjectKind.ViewFolder => PackIconKind.Folder,
        DbObjectKind.View => PackIconKind.TableEye,
        DbObjectKind.StoredProcedureFolder => PackIconKind.Folder,
        DbObjectKind.StoredProcedure => PackIconKind.ScriptText,
        DbObjectKind.FunctionFolder => PackIconKind.Folder,
        DbObjectKind.Function => PackIconKind.FunctionVariant,
        DbObjectKind.ColumnFolder => PackIconKind.Folder,
        DbObjectKind.Column => PackIconKind.TableColumn,
        DbObjectKind.IndexFolder => PackIconKind.Folder,
        DbObjectKind.Index => PackIconKind.Key,
        DbObjectKind.PrimaryKey => PackIconKind.KeyVariant,
        DbObjectKind.ForeignKey => PackIconKind.LinkVariant,
        _ => PackIconKind.CircleOutline
    };

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => Binding.DoNothing;
}
