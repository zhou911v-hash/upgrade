using System.Windows;
using System.Windows.Controls;
using SmartBuildingLighting.App.ViewModels;

namespace SmartBuildingLighting.App.Views;

public partial class SceneModeView : UserControl
{
    public SceneModeView() { InitializeComponent(); }

    private async void UserControl_Loaded(object sender, RoutedEventArgs e)
    {
        if (DataContext is SceneModeViewModel vm) await vm.LoadDataAsync();
    }
}
