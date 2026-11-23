using System.Windows.Input;
using Platinum.Domain.Layout;
using Platinum.Domain.Catalogue;
using Platinum.Infrastructure.Catalogue;
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
    private CatalogueItem[] _catalogueItems = [];
    private CatalogueItem? _selectedCatalogueItem;
    private CatalogueItem? _placementItem;
    private bool _connectMode;
    private (Guid PieceId, string PortId)? _pendingConnection;

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
        PlaceCataloguePieceCommand = new RelayCommand(BeginCataloguePlacement);
        ToggleConnectModeCommand = new RelayCommand(ToggleConnectMode);
        LoadCatalogue();
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
    public IReadOnlyList<CatalogueItem> CatalogueItems => _catalogueItems;
    public CatalogueItem? SelectedCatalogueItem { get => _selectedCatalogueItem; set => SetProperty(ref _selectedCatalogueItem, value); }
    public CatalogueItem? PlacementItem { get => _placementItem; private set => SetProperty(ref _placementItem, value); }
    public bool ConnectMode { get => _connectMode; private set => SetProperty(ref _connectMode, value); }

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
    public ICommand PlaceCataloguePieceCommand { get; }
    public ICommand ToggleConnectModeCommand { get; }

    private void LoadCatalogue()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "Assets", "catalogue-marklin-c.json");
        if (!File.Exists(path))
        {
            Status = "Catàleg Märklin no trobat; pots dibuixar traçats geomètrics.";
            return;
        }
        try
        {
            _catalogueItems = MarklinCCatalogue.Load(path).Items.ToArray();
            Engine.SetCataloguePorts(_catalogueItems.SelectMany(item =>
                item.Ports.Select(port => new TrackEditorPort(item.Ref, port.Id, port.X, port.Y, port.Accuracy))));
            OnPropertyChanged(nameof(CatalogueItems));
            Status = $"Catàleg carregat · {_catalogueItems.Length} referències · ports locals en mm.";
        }
        catch (Exception ex) when (ex is IOException or System.Text.Json.JsonException)
        {
            Status = "No s'ha pogut carregar el catàleg: " + ex.Message;
        }
    }

    private void BeginCataloguePlacement()
    {
        if (SelectedCatalogueItem is null) { Status = "Selecciona una referència del catàleg."; return; }
        PlacementItem = SelectedCatalogueItem;
        ConnectMode = false;
        Tool = TrackEditorTool.Select;
        Status = $"Col·loca {PlacementItem.Ref} fent clic al canvas; geometria {PlacementItem.GeometryStatus}.";
    }

    public void PlaceCataloguePieceAt(double x, double y)
    {
        if (PlacementItem is null) return;
        var point = Snap(x, y);
        var item = PlacementItem;
        Engine.AddPiece(item.Ref, (decimal)point.X, (decimal)point.Y, 0m, ActiveLevelId, item.GeometryStatus);
        PlacementItem = null;
        Status = $"Peça {item.Ref} col·locada · geometria {item.GeometryStatus}.";
        OnEditorChanged();
    }

    private void ToggleConnectMode()
    {
        ConnectMode = !ConnectMode;
        _pendingConnection = null;
        PlacementItem = null;
        Status = ConnectMode
            ? "Connexió: selecciona dos ports reals del catàleg. Connexió lògica, no verificació física."
            : "Mode de connexió desactivat.";
    }

    public void ConnectPortAt(double x, double y)
    {
        var point = Snap(x, y);
        var hit = Engine.HitTestPort((decimal)point.X, (decimal)point.Y, ActiveLevelId);
        if (hit is null) { Status = "No hi ha cap port del catàleg dins la tolerància de 22 mm."; return; }
        if (!_pendingConnection.HasValue)
        {
            _pendingConnection = hit;
            Status = $"Port {hit.Value.PortId} seleccionat. Tria el segon extrem.";
            return;
        }
        if (Engine.TryConnect(_pendingConnection.Value.PieceId, _pendingConnection.Value.PortId,
            hit.Value.PieceId, hit.Value.PortId, out _))
        {
            var sourceAccuracy = PortAccuracy(_pendingConnection.Value.PieceId, _pendingConnection.Value.PortId);
            var targetAccuracy = PortAccuracy(hit.Value.PieceId, hit.Value.PortId);
            Status = sourceAccuracy == "estimated" || targetAccuracy == "estimated"
                ? "Connexió lògica creada; almenys un port és estimat i requereix verificació de plànol."
                : "Connexió lògica creada amb ports derivats del catàleg; no certifica encaix físic.";
            OnEditorChanged();
        }
        else Status = "Connexió no creada: mateix component, port invàlid o connexió duplicada.";
        _pendingConnection = null;
    }

    private string? PortAccuracy(Guid pieceId, string portId)
    {
        var piece = Engine.Pieces.FirstOrDefault(x => x.Id == pieceId);
        return piece is null ? null : Engine.CataloguePorts.FirstOrDefault(x =>
            x.CatalogueCode.Equals(piece.CatalogueCode, StringComparison.OrdinalIgnoreCase) &&
            x.Id.Equals(portId, StringComparison.OrdinalIgnoreCase))?.Accuracy;
    }

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
            "Les connexions utilitzen els ports i les coordenades locals del catàleg Märklin compartit. Són relacions lògiques i no constitueixen una verificació d'encaix físic.");
}