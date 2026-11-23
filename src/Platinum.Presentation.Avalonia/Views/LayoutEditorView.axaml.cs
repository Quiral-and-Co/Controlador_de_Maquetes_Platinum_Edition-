using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using Platinum.Presentation.Avalonia.ViewModels;

namespace Platinum.Presentation.Avalonia.Views;

public sealed partial class LayoutEditorView : UserControl
{
    private TrackCanvas? _canvas;
    private Track3DCanvas? _canvas3D;
    private LayoutEditorViewModel? _viewModel;
    private (double X, double Y)? _dragAnchor;
    private (double X, double Y)? _lastDragPoint;
    private bool _dragging;

    public LayoutEditorView()
    {
        AvaloniaXamlLoader.Load(this);
        _canvas = this.FindControl<TrackCanvas>("EditorCanvas");
        _canvas3D = this.FindControl<Track3DCanvas>("Editor3DCanvas");
        DataContextChanged += (_, _) => AttachViewModel();
        AttachViewModel();
        if (_canvas is not null)
        {
            _canvas.PointerPressed += OnCanvasPointerPressed;
            _canvas.PointerMoved += OnCanvasPointerMoved;
            _canvas.PointerReleased += OnCanvasPointerReleased;
            _canvas.PointerWheelChanged += OnCanvasPointerWheelChanged;
        }
    }

    private void AttachViewModel()
    {
        if (_viewModel is not null) _viewModel.PropertyChanged -= OnViewModelPropertyChanged;
        _viewModel = DataContext as LayoutEditorViewModel;
        if (_viewModel is null) return;
        _viewModel.PropertyChanged += OnViewModelPropertyChanged;
        RefreshCanvases();
    }

    private void OnViewModelPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e) =>
        RefreshCanvases();

    private void RefreshCanvases()
    {
        if (_viewModel is null) return;
        if (_canvas is not null)
        {
            _canvas.Engine = _viewModel.Engine;
            _canvas.CatalogueItems = _viewModel.CatalogueItems;
            _canvas.ActiveLevelId = _viewModel.ActiveLevelId;
            _canvas.ZoomFactor = _viewModel.ZoomFactor;
            _canvas.GridMillimetres = _viewModel.GridMillimetres;
            _canvas.SnapToGrid = _viewModel.SnapToGrid;
            _canvas.SelectedSegmentId = _viewModel.SelectedSegmentId;
            _canvas.SelectedPieceId = _viewModel.SelectedPieceId;
            _canvas.Refresh();
        }
        if (_canvas3D is not null)
        {
            _canvas3D.Engine = _viewModel.Engine;
            _canvas3D.CatalogueItems = _viewModel.CatalogueItems;
            _canvas3D.Levels = _viewModel.Levels;
            _canvas3D.ActiveLevelId = _viewModel.ActiveLevelId;
            _canvas3D.InvalidateVisual();
        }
    }

    private void OnCanvasPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (_viewModel is null || _canvas is null || !e.GetCurrentPoint(_canvas).Properties.IsLeftButtonPressed) return;
        var model = _canvas.ToModel(e.GetPosition(_canvas));
        var snapped = _viewModel.Snap(model.X, model.Y);
        if (_viewModel.PlacementItem is not null)
        {
            _viewModel.PlaceCataloguePieceAt(snapped.X, snapped.Y);
            _canvas.InvalidateVisual();
            e.Handled = true;
            return;
        }
        if (_viewModel.ConnectMode)
        {
            _viewModel.ConnectPortAt(snapped.X, snapped.Y);
            _canvas.InvalidateVisual();
            e.Handled = true;
            return;
        }
        if (_viewModel.Tool == TrackEditorTool.Select)
        {
            _viewModel.SelectAt(snapped.X, snapped.Y);
            if (_viewModel.SelectedPieceId.HasValue || _viewModel.SelectedSegmentId.HasValue)
            {
                _viewModel.BeginDrag();
                _dragging = true;
                _lastDragPoint = snapped;
                e.Pointer.Capture(_canvas);
            }
        }
        else
        {
            _dragAnchor = snapped;
            _canvas.PreviewStart = snapped;
            _canvas.PreviewEnd = snapped;
            e.Pointer.Capture(_canvas);
            _canvas.InvalidateVisual();
        }
        e.Handled = true;
    }

    private void OnCanvasPointerMoved(object? sender, PointerEventArgs e)
    {
        if (_viewModel is null || _canvas is null) return;
        var model = _canvas.ToModel(e.GetPosition(_canvas));
        var snapped = _viewModel.Snap(model.X, model.Y);
        if (_dragging && _lastDragPoint.HasValue && e.GetCurrentPoint(_canvas).Properties.IsLeftButtonPressed)
        {
            _viewModel.MoveSelection(snapped.X - _lastDragPoint.Value.X, snapped.Y - _lastDragPoint.Value.Y);
            _lastDragPoint = snapped;
        }
        else if (_dragAnchor.HasValue && e.GetCurrentPoint(_canvas).Properties.IsLeftButtonPressed)
        {
            _canvas.PreviewEnd = model;
            _canvas.InvalidateVisual();
        }
    }

    private void OnCanvasPointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        if (_viewModel is null || _canvas is null) return;
        if (_dragging)
        {
            _dragging = false;
            _lastDragPoint = null;
            _viewModel.OnEditorChanged();
        }
        else if (_dragAnchor.HasValue)
        {
            var model = _canvas.ToModel(e.GetPosition(_canvas));
            var end = _viewModel.Snap(model.X, model.Y);
            var start = _dragAnchor.Value;
            _dragAnchor = null;
            _canvas.PreviewStart = null;
            _canvas.PreviewEnd = null;
            var dx = end.X - start.X;
            var dy = end.Y - start.Y;
            if (Math.Sqrt(dx * dx + dy * dy) >= 8)
                _viewModel.AddSegment(start.X, start.Y, end.X, end.Y);
        }
        e.Pointer.Capture(null);
        _canvas.InvalidateVisual();
        e.Handled = true;
    }

    private void OnCanvasPointerWheelChanged(object? sender, PointerWheelEventArgs e)
    {
        if (_viewModel is null) return;
        _viewModel.Zoom(e.Delta.Y > 0 ? 1.15 : 1d / 1.15);
        e.Handled = true;
    }
}