using System.Windows;
using System.Windows.Controls;
using SmartBuildingLighting.App.ViewModels;

namespace SmartBuildingLighting.App.Views;

public partial class FloorMonitorView : UserControl
{
    public FloorMonitorView() { InitializeComponent(); }

    private async void UserControl_Loaded(object sender, RoutedEventArgs e)
    {
        if (DataContext is FloorMonitorViewModel vm) await vm.LoadDataAsync();
    }
}
