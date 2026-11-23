using System.Windows.Input;
using Platinum.Domain.Layout;
using Platinum.Presentation.Avalonia.Services;

namespace Platinum.Presentation.Avalonia.ViewModels;

public enum TrackEditorTool { Select, Straight, Curve, Turnout }

public sealed class LayoutEditorViewModel : PageViewModel
{
    private readonly IDialogService _dialogs;
    private readonly TrackEditorEngine _engine;
    private Guid _activeLevelId = Guid.Empty;
    private Guid? _selectedSegmentId;
    private Guid? _selectedPieceId;
    private TrackEditorTool _tool = TrackEditorTool.Select;
    private decimal _gridMillimetres = 10m;
    private bool _snapToGrid = true;
    private double _zoomFactor = 1d;
    private string _status = "Editor 2D preparat · coordenades en mm · mode segur";

    public LayoutEditorViewModel(IDialogService dialogs, TrackEditorEngine engine) : base(
        "Editor de maquetes 2D/3D", "Editor basat en el motor geomètric compartit del domini. Les vistes només representen i tradueixen gestos a operacions.")
    {
        _dialogs = dialogs;
        _engine = engine;
        UndoCommand = new RelayCommand(Undo);
        RedoCommand = new RelayCommand(Redo);
        SelectToolCommand = new RelayCommand(() => SetTool(TrackEditorTool.Select));
        StraightToolCommand = new RelayCommand(() => SetTool(TrackEditorTool.Straight));
        CurveToolCommand = new RelayCommand(() => SetTool(TrackEditorTool.Curve));
        TurnoutToolCommand = new RelayCommand(() => SetTool(TrackEditorTool.Turnout));
        RotateMinusCommand = new RelayCommand(() => RotateSelected(-15m));
        RotatePlusCommand = new RelayCommand(() => RotateSelected(15m));
        Rotate90Command = new RelayCommand(() => RotateSelected(90m));
        ZoomInCommand = new RelayCommand(() => Zoom(1.2));
        ZoomOutCommand = new RelayCommand(() => Zoom(1d / 1.2));
        ResetZoomCommand = new RelayCommand(() => ZoomFactor = 1d);
        ExplainConnectionsCommand = new RelayCommand(() => _ = ExplainConnectionsAsync());
    }

    public TrackEditorEngine Engine => _engine;
    public Guid ActiveLevelId { get => _activeLevelId; set => SetProperty(ref _activeLevelId, value); }
    public Guid? SelectedSegmentId { get => _selectedSegmentId; private set => SetProperty(ref _selectedSegmentId, value); }
    public Guid? SelectedPieceId { get => _selectedPieceId; private set => SetProperty(ref _selectedPieceId, value); }
    public TrackEditorTool Tool { get => _tool; private set => SetProperty(ref _tool, value); }
    public decimal GridMillimetres { get => _gridMillimetres; set => SetProperty(ref _gridMillimetres, value); }
    public bool SnapToGrid { get => _snapToGrid; set => SetProperty(ref _snapToGrid, value); }
    public double ZoomFactor { get => _zoomFactor; private set => SetProperty(ref _zoomFactor, Math.Clamp(value, 0.35, 3.5)); }
    public string Status { get => _status; private set => SetProperty(ref _status, value); }
    public IReadOnlyList<TrackLevel> Levels => [new TrackLevel(Guid.Empty, "Nivell base", 0m)];

    public ICommand UndoCommand { get; }
    public ICommand RedoCommand { get; }
    public ICommand SelectToolCommand { get; }
    public ICommand StraightToolCommand { get; }
    public ICommand CurveToolCommand { get; }
    public ICommand TurnoutToolCommand { get; }
    public ICommand RotateMinusCommand { get; }
    public ICommand RotatePlusCommand { get; }
    public ICommand Rotate90Command { get; }
    public ICommand ZoomInCommand { get; }
    public ICommand ZoomOutCommand { get; }
    public ICommand ResetZoomCommand { get; }
    public ICommand ExplainConnectionsCommand { get; }

    public void SetTool(TrackEditorTool tool)
    {
        Tool = tool;
        Status = tool switch
        {
            TrackEditorTool.Select => "Selecció: clica una peça o traçat i arrossega per moure'l.",
            TrackEditorTool.Straight => "Via recta: arrossega del punt inicial al final.",
            TrackEditorTool.Curve => "Corba quadràtica: arrossega per definir extrems; es conserva el control original.",
            TrackEditorTool.Turnout => "Desviament: arrossega per definir la via principal i la branca.",
            _ => Status
        };
    }

    public (double X, double Y) Snap(double x, double y)
    {
        var snapped = TrackEditorEngine.Snap((decimal)x, (decimal)y, GridMillimetres, SnapToGrid);
        return ((double)snapped.X, (double)snapped.Y);
    }

    public void SelectAt(double x, double y)
    {
        SelectedPieceId = Engine.HitTestPiece((decimal)x, (decimal)y, ActiveLevelId);
        SelectedSegmentId = SelectedPieceId.HasValue ? null :
            Engine.HitTestSegment((decimal)x, (decimal)y, ActiveLevelId);
        Status = SelectedPieceId.HasValue ? "Peça seleccionada · arrossega per moure-la o gira-la." :
            SelectedSegmentId.HasValue ? "Traçat seleccionat · arrossega per moure'l." : "Cap element seleccionat.";
    }

    public void BeginDrag() => Engine.BeginEdit();

    public void MoveSelection(double deltaX, double deltaY)
    {
        var dx = (decimal)deltaX;
        var dy = (decimal)deltaY;
        if (SelectedPieceId.HasValue) Engine.MovePiece(SelectedPieceId.Value, dx, dy, false);
        else if (SelectedSegmentId.HasValue) Engine.MoveSegment(SelectedSegmentId.Value, dx, dy, false);
        OnEditorChanged();
    }

    public void AddSegment(double startX, double startY, double endX, double endY)
    {
        try
        {
            var kind = Tool switch
            {
                TrackEditorTool.Curve => TrackGeometryKind.Curve,
                TrackEditorTool.Turnout => TrackGeometryKind.Turnout,
                _ => TrackGeometryKind.Straight
            };
            var segment = Engine.AddSegment(kind, (decimal)startX, (decimal)startY,
                (decimal)endX, (decimal)endY, ActiveLevelId);
            SelectedSegmentId = segment.Id;
            SelectedPieceId = null;
            Status = $"{kind} creat · coordenades i punts de control emmagatzemats en mm.";
            OnEditorChanged();
        }
        catch (ArgumentException ex) { Status = ex.Message; }
    }

    public void RotateSelected(decimal degrees)
    {
        if (!SelectedPieceId.HasValue)
        {
            Status = "Selecciona una peça de catàleg abans de girar-la.";
            return;
        }
        if (Engine.RotatePiece(SelectedPieceId.Value, degrees))
        {
            Status = "Orientació de la peça actualitzada.";
            OnEditorChanged();
        }
    }

    public void Undo()
    {
        if (Engine.Undo()) { SelectedPieceId = null; SelectedSegmentId = null; Status = "Operació desfeta."; OnEditorChanged(); }
    }

    public void Redo()
    {
        if (Engine.Redo()) { SelectedPieceId = null; SelectedSegmentId = null; Status = "Operació refeita."; OnEditorChanged(); }
    }

    public void Zoom(double factor) => ZoomFactor *= factor;

    public void OnEditorChanged()
    {
        OnPropertyChanged(nameof(Engine));
        OnPropertyChanged(nameof(Levels));
    }

    private async Task ExplainConnectionsAsync() =>
        await _dialogs.ShowMessageAsync("Connexions de via",
            "El motor del domini ja crea connexions lògiques per identificador de port i impedeix duplicats. La vista Avalonia encara no importa el catàleg Märklin ni exposa els ports reals; no s'ofereixen ports ficticis ni es marca cap connexió com a verificació física.");
}