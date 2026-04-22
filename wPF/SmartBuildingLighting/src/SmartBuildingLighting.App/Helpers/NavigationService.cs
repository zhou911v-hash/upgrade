using CommunityToolkit.Mvvm.ComponentModel;

namespace SmartBuildingLighting.App.Helpers;

/// <summary>
/// 导航服务 — 管理页面切换
/// </summary>
public class NavigationService : ObservableObject
{
    private ObservableObject? _currentViewModel;
    public ObservableObject? CurrentViewModel
    {
        get => _currentViewModel;
        set => SetProperty(ref _currentViewModel, value);
    }

    private readonly Dictionary<string, Func<ObservableObject>> _viewModelFactories = new();

    public void Register(string key, Func<ObservableObject> factory)
    {
        _viewModelFactories[key] = factory;
    }

    public void NavigateTo(string key)
    {
        if (_viewModelFactories.TryGetValue(key, out var factory))
        {
            CurrentViewModel = factory();
        }
    }
}
