using System.Windows;
using System.Windows.Controls;
using SmartBuildingLighting.App.ViewModels;

namespace SmartBuildingLighting.App.Views;

public partial class LogView : UserControl
{
    public LogView() { InitializeComponent(); }
    private async void UserControl_Loaded(object sender, RoutedEventArgs e)
    { if (DataContext is LogViewModel vm) await vm.LoadDataAsync(); }
}
