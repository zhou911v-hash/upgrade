using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SmartBuildingLighting.Core.Enums;
using SmartBuildingLighting.Core.Interfaces;
using SmartBuildingLighting.Core.Models;

namespace SmartBuildingLighting.App.ViewModels;

public partial class LogViewModel : ObservableObject
{
    private readonly ILogService _logService;

    [ObservableProperty] private int _currentPage = 1;
    [ObservableProperty] private int _totalPages = 1;
    [ObservableProperty] private int _totalCount;
    [ObservableProperty] private int _pageSize = 20;
    [ObservableProperty] private DateTime? _filterStartDate;
    [ObservableProperty] private DateTime? _filterEndDate;
    [ObservableProperty] private bool _isLoading;

    public ObservableCollection<OperationLog> Logs { get; } = new();

    public LogViewModel(ILogService logService) { _logService = logService; }

    [RelayCommand]
    public async Task LoadDataAsync()
    {
        IsLoading = true;
        try
        {
            var (logs, total) = await _logService.GetLogsAsync(CurrentPage, PageSize, null, FilterStartDate, FilterEndDate);
            Logs.Clear();
            foreach (var l in logs) Logs.Add(l);
            TotalCount = total;
            TotalPages = (int)Math.Ceiling((double)total / PageSize);
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task NextPageAsync()
    {
        if (CurrentPage < TotalPages) { CurrentPage++; await LoadDataAsync(); }
    }

    [RelayCommand]
    private async Task PrevPageAsync()
    {
        if (CurrentPage > 1) { CurrentPage--; await LoadDataAsync(); }
    }

    [RelayCommand]
    private async Task FilterAsync()
    {
        CurrentPage = 1;
        await LoadDataAsync();
    }
}
