using System.Windows;
using System.Windows.Controls;
using SmartBuildingLighting.App.ViewModels;
using SmartBuildingLighting.Core.Models;

namespace SmartBuildingLighting.App.Views;

public partial class GroupControlView : UserControl
{
    public GroupControlView() { InitializeComponent(); }

    private async void UserControl_Loaded(object sender, RoutedEventArgs e)
    {
        if (DataContext is GroupControlViewModel vm) await vm.LoadDataAsync();
    }

    private async void GroupOn_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is int groupId && DataContext is GroupControlViewModel vm)
            await vm.ControlGroupCommand.ExecuteAsync((groupId, true));
    }

    private async void GroupOff_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is int groupId && DataContext is GroupControlViewModel vm)
            await vm.ControlGroupCommand.ExecuteAsync((groupId, false));
    }

    private async void GroupDelete_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is LightGroup group && DataContext is GroupControlViewModel vm)
        {
            var result = MessageBox.Show($"确定删除分组 \"{group.Name}\"？", "确认删除", MessageBoxButton.YesNo);
            if (result == MessageBoxResult.Yes) await vm.DeleteGroupCommand.ExecuteAsync(group);
        }
    }
}
