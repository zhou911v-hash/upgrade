using System.Windows;
using System.Windows.Controls;
using SmartBuildingLighting.App.ViewModels;
using SmartBuildingLighting.Core.Models;

namespace SmartBuildingLighting.App.Views;

public partial class ScheduleView : UserControl
{
    public ScheduleView() { InitializeComponent(); }
    private async void UserControl_Loaded(object sender, RoutedEventArgs e)
    { if (DataContext is ScheduleViewModel vm) await vm.LoadDataAsync(); }

    private async void ToggleSchedule_Click(object sender, RoutedEventArgs e)
    { if (sender is Button btn && btn.Tag is Schedule s && DataContext is ScheduleViewModel vm) await vm.ToggleScheduleCommand.ExecuteAsync(s); }

    private async void DeleteSchedule_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is Schedule s && DataContext is ScheduleViewModel vm)
        {
            if (MessageBox.Show($"确定删除任务 \"{s.Name}\"？", "确认", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
                await vm.DeleteScheduleCommand.ExecuteAsync(s);
        }
    }
}
