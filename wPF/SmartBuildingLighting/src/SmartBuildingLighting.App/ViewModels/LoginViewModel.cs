using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SmartBuildingLighting.Core.Interfaces;

namespace SmartBuildingLighting.App.ViewModels;

public partial class LoginViewModel : ObservableObject
{
    private readonly IUserService _userService;
    private readonly ILogService _logService;
    public event Action? LoginSucceeded;

    [ObservableProperty] private string _username = string.Empty;
    [ObservableProperty] private string _errorMessage = string.Empty;
    [ObservableProperty] private bool _isLoading;
    [ObservableProperty] private bool _hasError;

    // Password不能用绑定，需要在View中处理
    public string Password { get; set; } = string.Empty;

    public LoginViewModel(IUserService userService, ILogService logService)
    {
        _userService = userService;
        _logService = logService;
    }

    [RelayCommand]
    private async Task LoginAsync()
    {
        HasError = false;
        ErrorMessage = string.Empty;

        if (string.IsNullOrWhiteSpace(Username))
        {
            ErrorMessage = "请输入用户名";
            HasError = true;
            return;
        }
        if (string.IsNullOrWhiteSpace(Password))
        {
            ErrorMessage = "请输入密码";
            HasError = true;
            return;
        }

        IsLoading = true;
        try
        {
            var user = await _userService.LoginAsync(Username, Password);
            if (user != null)
            {
                await _logService.LogAsync(Core.Enums.OperationType.Login, $"用户 {user.DisplayName} 登录系统", user.Username, user.Id);
                LoginSucceeded?.Invoke();
            }
            else
            {
                ErrorMessage = "用户名或密码错误";
                HasError = true;
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = $"登录失败: {ex.Message}";
            HasError = true;
        }
        finally
        {
            IsLoading = false;
        }
    }
}
