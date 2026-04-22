using System.Windows;
using System.Windows.Controls;
using SmartBuildingLighting.App.ViewModels;

namespace SmartBuildingLighting.App.Views;

public partial class EnergyStatsView : UserControl
{
    public EnergyStatsView() { InitializeComponent(); }
    private async void UserControl_Loaded(object sender, RoutedEventArgs e)
    { if (DataContext is EnergyStatsViewModel vm) await vm.LoadDataAsync(); }
}
