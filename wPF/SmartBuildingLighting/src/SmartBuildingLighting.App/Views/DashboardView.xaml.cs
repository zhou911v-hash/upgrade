using System.Windows.Controls;
using SmartBuildingLighting.App.ViewModels;

namespace SmartBuildingLighting.App.Views;

public partial class DashboardView : UserControl
{
    public DashboardView() { InitializeComponent(); }

    private async void UserControl_Loaded(object sender, System.Windows.RoutedEventArgs e)
    {
        if (DataContext is DashboardViewModel vm) await vm.LoadDataAsync();
    }
}
