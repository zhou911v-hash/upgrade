using System.Windows;
using System.Windows.Controls;
using SmartBuildingLighting.App.ViewModels;

namespace SmartBuildingLighting.App.Views;

public partial class SettingsView : UserControl
{
    public SettingsView() { InitializeComponent(); }

    private async void UserControl_Loaded(object sender, RoutedEventArgs e)
    {
        if (DataContext is SettingsViewModel vm)
            await vm.LoadDataAsync();
    }

    private void ChangePassword_Click(object sender, RoutedEventArgs e)
    {
        if (DataContext is SettingsViewModel vm)
        {
            vm.OldPassword = OldPwdBox.Password;
            vm.NewPassword = NewPwdBox.Password;
            vm.ChangePasswordCommand.Execute(null);
        }
    }
}
