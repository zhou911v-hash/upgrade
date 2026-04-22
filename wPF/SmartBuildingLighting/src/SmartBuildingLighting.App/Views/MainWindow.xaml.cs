using System.Windows;
using System.Windows.Input;
using SmartBuildingLighting.App.ViewModels;

namespace SmartBuildingLighting.App.Views;

public partial class MainWindow : Window
{
    private readonly MainViewModel _viewModel;

    public MainWindow(MainViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        DataContext = viewModel;
        viewModel.NavigateToDefault();
        viewModel.LogoutRequested += OnLogoutRequested;
    }

    private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ClickCount == 2)
            MaximizeButton_Click(sender, e);
        else
            DragMove();
    }

    private void MinimizeButton_Click(object sender, RoutedEventArgs e) => WindowState = WindowState.Minimized;

    private void MaximizeButton_Click(object sender, RoutedEventArgs e)
        => WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;

    private void CloseButton_Click(object sender, RoutedEventArgs e) => Application.Current.Shutdown();

    private void LogoutButton_Click(object sender, RoutedEventArgs e)
    {
        _viewModel.LogoutCommand.Execute(null);
    }

    private void OnLogoutRequested()
    {
        var app = (App)Application.Current;
        app.ShowLoginWindow();
        Close();
    }
}
