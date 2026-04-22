using System.Windows;
using System.Windows.Input;
using SmartBuildingLighting.App.ViewModels;

namespace SmartBuildingLighting.App.Views;

public partial class LoginView : Window
{
    private readonly LoginViewModel _viewModel;

    public LoginView(LoginViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        DataContext = _viewModel;
        _viewModel.LoginSucceeded += OnLoginSucceeded;
        // 支持拖动窗口
        MouseLeftButtonDown += (s, e) => { if (e.ChangedButton == MouseButton.Left) DragMove(); };
    }

    private void LoginButton_Click(object sender, RoutedEventArgs e)
    {
        _viewModel.Password = PasswordBox.Password;
        _viewModel.LoginCommand.Execute(null);
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        Application.Current.Shutdown();
    }

    private void OnLoginSucceeded()
    {
        var app = (App)Application.Current;
        app.ShowMainWindow();
        Close();
    }
}
