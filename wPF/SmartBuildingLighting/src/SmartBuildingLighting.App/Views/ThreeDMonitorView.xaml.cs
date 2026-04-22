using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using HelixToolkit.Wpf;
using SmartBuildingLighting.App.ViewModels;
using SmartBuildingLighting.Core.Enums;
using SmartBuildingLighting.Core.Models;

namespace SmartBuildingLighting.App.Views;

public partial class ThreeDMonitorView : UserControl
{
    private ThreeDMonitorViewModel? _viewModel;

    public ThreeDMonitorView()
    {
        InitializeComponent();
        Unloaded += OnUnloaded;
    }

    private async void UserControl_Loaded(object sender, RoutedEventArgs e)
    {
        if (DataContext is ThreeDMonitorViewModel vm)
        {
            if (!ReferenceEquals(_viewModel, vm))
            {
                if (_viewModel != null)
                    _viewModel.PropertyChanged -= ViewModel_PropertyChanged;

                _viewModel = vm;
                _viewModel.PropertyChanged += ViewModel_PropertyChanged;
            }

            await vm.LoadDataAsync();
            RenderScene();
        }
    }

    private void ResetCamera_Click(object sender, RoutedEventArgs e)
    {
        Viewport.ZoomExtents();
    }

    private void ViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(ThreeDMonitorViewModel.VisibleLayouts) or nameof(ThreeDMonitorViewModel.SelectedCircuit) or nameof(ThreeDMonitorViewModel.SelectedFloor) or nameof(ThreeDMonitorViewModel.ShowAllFloors))
            Dispatcher.Invoke(RenderScene);
    }

    private void RenderScene()
    {
        while (Viewport.Children.Count > 1)
            Viewport.Children.RemoveAt(1);

        if (_viewModel == null)
            return;

        foreach (var layout in _viewModel.VisibleLayouts)
        {
            double baseZ = (layout.FloorId - 1) * 14;
            Viewport.Children.Add(new BoxVisual3D
            {
                Center = new Point3D(0, 0, baseZ - 0.6),
                Length = 44,
                Width = 44,
                Height = 0.8,
                Fill = new SolidColorBrush(Color.FromArgb(160, 20, 33, 46))
            });

            foreach (var area in layout.Areas)
            {
                var center = CreateAreaCenter(area, baseZ);
                Viewport.Children.Add(new BoxVisual3D
                {
                    Center = center,
                    Length = Math.Max(area.Width * 0.38, 1.2),
                    Width = Math.Max(area.Height * 0.38, 1.2),
                    Height = 1.1,
                    Fill = new SolidColorBrush(Color.FromArgb(90, 111, 168, 207))
                });
            }
        }

        foreach (var circuit in _viewModel.VisibleCircuits)
        {
            var color = circuit.Status switch
            {
                CircuitStatus.On => Color.FromRgb(0, 230, 118),
                CircuitStatus.Fault => Color.FromRgb(255, 82, 82),
                CircuitStatus.Offline => Color.FromRgb(255, 179, 0),
                _ => Color.FromRgb(120, 144, 156)
            };

            Viewport.Children.Add(new SphereVisual3D
            {
                Center = circuit.Position,
                Radius = _viewModel.SelectedCircuit?.CircuitId == circuit.CircuitId ? 1.1 : 0.8,
                PhiDiv = 20,
                ThetaDiv = 20,
                Fill = new SolidColorBrush(color)
            });
        }
    }

    private static Point3D CreateAreaCenter(AreaLayoutSnapshot area, double baseZ)
    {
        double x = (area.PositionX + area.Width / 2d - 50d) * 0.38;
        double y = (area.PositionY + area.Height / 2d - 50d) * 0.38;
        return new Point3D(x, y, baseZ);
    }

    private void OnUnloaded(object sender, RoutedEventArgs e)
    {
        if (_viewModel != null)
            _viewModel.PropertyChanged -= ViewModel_PropertyChanged;
    }
}
