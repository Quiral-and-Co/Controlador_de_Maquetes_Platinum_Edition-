using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Platinum.Domain.Layout;
using Platinum.Presentation.Avalonia.Catalogue;

namespace Platinum.Presentation.Avalonia.Editor;

/// <summary>Editor natiu 2D: geometria, peces de catàleg, ports, connexions i historial local.</summary>
public sealed class TrackPlanView : Control
{
    private const double Pad=24;
    private TrackLayout? _layout;
    private Guid _levelId;
    private string _tool="Seleccionar";
    private CatalogueItem? _placement;
    private readonly Dictionary<string,CatalogueItem> _catalogue=new(StringComparer.OrdinalIgnoreCase);
    private Guid? _selectedPiece,_selectedSegment,_pendingPiece;
    private string? _pendingPort;
    private Point? _anchor,_lastModel,_lastScreen;
    private bool _dragging,_panning,_connectMode,_snap=true;
    private double _zoom=1;
    private Vector _pan=new(0,0);
    private readonly Stack<Snapshot> _undo=[];
    private readonly Stack<Snapshot> _redo=[];
    private sealed record Snapshot(TrackGeometrySegment[] Segments,TrackPiece[] Pieces,TrackConnection[] Connections);

    public TrackPlanView(){ClipToBounds=true;Focusable=true;}
    public void SetLayout(TrackLayout layout,Guid levelId){_layout=layout;_levelId=levelId;_anchor=null;_selectedPiece=null;_selectedSegment=null;_undo.Clear();_redo.Clear();InvalidateVisual();}
    public void SetActiveLevel(Guid id){_levelId=id;_anchor=null;_selectedPiece=null;_selectedSegment=null;_pendingPiece=null;InvalidateVisual();}
    public void SetTool(string tool){_tool=tool;_placement=null;_connectMode=false;_anchor=null;InvalidateVisual();}
    public void SetCatalogue(IEnumerable<CatalogueItem> items){_catalogue.Clear();foreach(var item in items)_catalogue[item.Ref]=item;InvalidateVisual();}
    public void BeginCataloguePlacement(CatalogueItem item){_placement=item;_tool="Seleccionar";_connectMode=false;Focus();}
    public void ToggleConnectMode(){_connectMode=!_connectMode;_placement=null;_pendingPiece=null;_pendingPort=null;InvalidateVisual();}
    public void SetGridSnap(bool enabled)=>_snap=enabled;
    public void ZoomIn()=>SetZoom(_zoom*1.2);
    public void ZoomOut()=>SetZoom(_zoom/1.2);
    public void FitView(){_zoom=1;_pan=new Vector(0,0);InvalidateVisual();}
    public void Undo(){if(_undo.Count==0||_layout is null)return;_redo.Push(Capture());Restore(_undo.Pop());InvalidateVisual();}
    public void Redo(){if(_redo.Count==0||_layout is null)return;_undo.Push(Capture());Restore(_redo.Pop());InvalidateVisual();}
    public void RotateSelected(decimal degrees)
    {
        if(_layout is null||_selectedPiece is null)return;var i=_layout.Pieces.FindIndex(x=>x.Id==_selectedPiece);if(i<0)return;
        PushUndo();var p=_layout.Pieces[i];_layout.Pieces[i]=p with{RotationDegrees=((p.RotationDegrees+degrees)%360m+360m)%360m};InvalidateVisual();
    }
    public void DeleteSelected()
    {
        if(_layout is null)return;
        if(_selectedPiece is Guid p){PushUndo();_layout.Pieces.RemoveAll(x=>x.Id==p);_layout.Connections.RemoveAll(x=>x.FromPieceId==p||x.ToPieceId==p);_selectedPiece=null;}
        else if(_selectedSegment is Guid s){PushUndo();_layout.GeometrySegments.RemoveAll(x=>x.Id==s);_selectedSegment=null;}
        InvalidateVisual();
    }
    private void SetZoom(double value){_zoom=Math.Clamp(value,.25,5);InvalidateVisual();}
    private Snapshot Capture()=>new(_layout!.GeometrySegments.ToArray(),_layout.Pieces.ToArray(),_layout.Connections.ToArray());
    private void PushUndo(){if(_layout is null)return;_undo.Push(Capture());if(_undo.Count>100){var keep=_undo.Reverse().TakeLast(100).ToArray();_undo.Clear();foreach(var s in keep)_undo.Push(s);}_redo.Clear();}
    private void Restore(Snapshot s){_layout!.GeometrySegments.Clear();_layout.GeometrySegments.AddRange(s.Segments);_layout.Pieces.Clear();_layout.Pieces.AddRange(s.Pieces);_layout.Connections.Clear();_layout.Connections.AddRange(s.Connections);_selectedPiece=null;_selectedSegment=null;_pendingPiece=null;_pendingPort=null;}
    private double BaseScale=>_layout is null?1:Math.Min((Bounds.Width-Pad*2)/((double)_layout.WidthMillimetres+160),(Bounds.Height-Pad*2)/((double)_layout.HeightMillimetres+160));
    private Point ToScreen(double x,double y)=>new(Pad+((x+80)*BaseScale*_zoom)+_pan.X,Pad+((y+80)*BaseScale*_zoom)+_pan.Y);
    private Point ToModel(Point p)=>new(((p.X-Pad-_pan.X)/(BaseScale*_zoom))-80,((p.Y-Pad-_pan.Y)/(BaseScale*_zoom))-80);
    private Point Snap(Point p){if(!_snap||_layout is null)return p;var g=(double)(_layout.GridMillimetres>0?_layout.GridMillimetres:10m);return new Point(Math.Round(p.X/g)*g,Math.Round(p.Y/g)*g);}
    private Point WorldPort(TrackPiece piece,CataloguePort port){var a=(double)piece.RotationDegrees*Math.PI/180;return new Point((double)piece.X+(double)port.X*Math.Cos(a)-(double)port.Y*Math.Sin(a),(double)piece.Y+(double)port.X*Math.Sin(a)+(double)port.Y*Math.Cos(a));}
    private (TrackPiece Piece,CataloguePort Port)? HitPort(Point p)
    {
        if(_layout is null)return null;(TrackPiece Piece,CataloguePort Port)? hit=null;var best=24d;
        foreach(var piece in _layout.Pieces.Where(x=>x.LevelId==_levelId).Reverse())if(_catalogue.TryGetValue(piece.CatalogueCode,out var item))foreach(var port in item.Ports){var d=Math.Sqrt(Math.Pow(p.X-WorldPort(piece,port).X,2)+Math.Pow(p.Y-WorldPort(piece,port).Y,2));if(d<best){best=d;hit=(piece,port);}}
        return hit;
    }
    private Guid? HitPiece(Point p){if(_layout is null)return null;foreach(var piece in _layout.Pieces.Where(x=>x.LevelId==_levelId).Reverse())if(Math.Sqrt(Math.Pow(p.X-(double)piece.X,2)+Math.Pow(p.Y-(double)piece.Y,2))<120)return piece.Id;return null;}
    private static double Distance(Point p,Point a,Point b){var v=b-a;var d=v.X*v.X+v.Y*v.Y;if(d<1e-8)returnMath.Sqrt(Math.Pow(p.X-a.X,2)+Math.Pow(p.Y-a.Y,2));var t=Math.Clamp(((p.X-a.X)*v.X+(p.Y-a.Y)*v.Y)/d,0,1);returnMath.Sqrt(Math.Pow(p.X-(a+v*t).X,2)+Math.Pow(p.Y-(a+v*t).Y,2));}
    private Guid? HitSegment(Point p)
    {
        if(_layout is null)return null;foreach(var s in _layout.GeometrySegments.Where(x=>x.LevelId==_levelId).Reverse())
        {var a=new Point((double)s.StartX,(double)s.StartY);var b=new Point((double)s.EndX,(double)s.EndY);if(s.Kind!=TrackGeometryKind.Curve){if(Distance(p,a,b)<60)return s.Id;}else{var prev=a;for(var i=1;i<=32;i++){var t=i/32d;var u=1-t;var q=new Point(u*u*a.X+2*u*t*(double)s.ControlX+t*t*b.X,u*u*a.Y+2*u*t*(double)s.ControlY+t*t*b.Y);if(Distance(p,prev,q)<60)return s.Id;prev=q;}}}
        return null;
    }
    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);if(_layout is null)return;var props=e.GetCurrentPoint(this).Properties;var screen=e.GetPosition(this);
        if(props.IsMiddleButtonPressed||props.IsRightButtonPressed){_panning=true;_lastScreen=screen;e.Pointer.Capture(this);e.Handled=true;return;}
        if(!props.IsLeftButtonPressed)return;var model=Snap(ToModel(screen));
        if(_placement is not null){PushUndo();_layout.Pieces.Add(new TrackPiece(Guid.NewGuid(),_placement.Ref,(decimal)model.X,(decimal)model.Y,0,_levelId,_placement.GeometryStatus));_placement=null;InvalidateVisual();e.Handled=true;return;}
        if(_connectMode)
        {
            var hit=HitPort(model);if(hit is null)return;
            if(_pendingPiece is null){_pendingPiece=hit.Value.Piece.Id;_pendingPort=hit.Value.Port.Id;_selectedPiece=_pendingPiece;}
            else if(_pendingPiece==hit.Value.Piece.Id&&_pendingPort==hit.Value.Port.Id){}
            else
            {
                var from=_pendingPiece.Value;var fromPort=_pendingPort??"";var exists=_layout.Connections.Any(x=>(x.FromPieceId==from&&x.FromPort==fromPort&&x.ToPieceId==hit.Value.Piece.Id&&x.ToPort==hit.Value.Port.Id)||(x.ToPieceId==from&&x.ToPort==fromPort&&x.FromPieceId==hit.Value.Piece.Id&&x.FromPort==hit.Value.Port.Id));
                if(!exists){PushUndo();_layout.Connections.Add(new TrackConnection(Guid.NewGuid(),from,fromPort,hit.Value.Piece.Id,hit.Value.Port.Id,false));}
                _pendingPiece=null;_pendingPort=null;
            }
            InvalidateVisual();e.Handled=true;return;
        }
        if(_tool=="Seleccionar")
        {
            _selectedPiece=HitPiece(model);_selectedSegment=_selectedPiece is null?HitSegment(model):null;
            if(_selectedPiece is not null||_selectedSegment is not null){PushUndo();_dragging=true;_lastModel=model;e.Pointer.Capture(this);InvalidateVisual();e.Handled=true;}return;
        }
        _anchor=model;e.Pointer.Capture(this);e.Handled=true;
    }
    protected override void OnPointerMoved(PointerEventArgs e)
    {
        base.OnPointerMoved(e);if(_layout is null)return;var screen=e.GetPosition(this);
        if(_panning&&_lastScreen is Point last){_pan+=screen-last;_lastScreen=screen;InvalidateVisual();return;}
        if(_dragging&&_lastModel is Point prev&&e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
        {
            var now=Snap(ToModel(screen));var old=Snap(prev);var dx=(decimal)(now.X-old.X);var dy=(decimal)(now.Y-old.Y);
            if(_selectedPiece is Guid pid){var i=_layout.Pieces.FindIndex(x=>x.Id==pid);if(i>=0){var p=_layout.Pieces[i];_layout.Pieces[i]=p with{X=p.X+dx,Y=p.Y+dy};}}
            if(_selectedSegment is Guid sid){var i=_layout.GeometrySegments.FindIndex(x=>x.Id==sid);if(i>=0){var s=_layout.GeometrySegments[i];_layout.GeometrySegments[i]=s with{StartX=s.StartX+dx,StartY=s.StartY+dy,EndX=s.EndX+dx,EndY=s.EndY+dy,ControlX=s.ControlX+dx,ControlY=s.ControlY+dy};}}
            _lastModel=now;InvalidateVisual();return;
        }
        if(_anchor is Point start&&e.GetCurrentPoint(this).Properties.IsLeftButtonPressed){InvalidateVisual();}
    }
    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        base.OnPointerReleased(e);if(_layout is null)return;
        if(_panning){_panning=false;_lastScreen=null;e.Pointer.Capture(null);return;}
        if(_dragging){_dragging=false;_lastModel=null;e.Pointer.Capture(null);return;}
        if(_anchor is not Point start)return;var end=Snap(ToModel(e.GetPosition(this)));_anchor=null;e.Pointer.Capture(null);
        if(Math.Sqrt(Math.Pow(end.X-start.X,2)+Math.Pow(end.Y-start.Y,2))<8){InvalidateVisual();return;}PushUndo();
        var dx=end.X-start.X;var dy=end.Y-start.Y;var kind=_tool=="Corba"?TrackGeometryKind.Curve:_tool=="Desviament"?TrackGeometryKind.Turnout:TrackGeometryKind.Straight;
        _layout.GeometrySegments.Add(new TrackGeometrySegment(Guid.NewGuid(),kind,(decimal)start.X,(decimal)start.Y,(decimal)end.X,(decimal)end.Y,(decimal)(start.X+dx/2-dy*.22),(decimal)(start.Y+dy/2+dx*.22),_levelId));InvalidateVisual();
    }
    protected override void OnPointerWheelChanged(PointerWheelEventArgs e){base.OnPointerWheelChanged(e);SetZoom(_zoom*(e.Delta.Y>0?1.15:1/1.15));e.Handled=true;}
    public override void Render(DrawingContext ctx)
    {
        base.Render(ctx);ctx.DrawRectangle(new SolidColorBrush(Color.Parse("#121B24")),null,Bounds);if(_layout is null||Bounds.Width<100)return;
        var scale=BaseScale*_zoom;var board=new Rect(Pad+_pan.X,Pad+_pan.Y,((double)_layout.WidthMillimetres+160)*scale,((double)_layout.HeightMillimetres+160)*scale);
        ctx.DrawRectangle(new SolidColorBrush(Color.Parse("#17232E")),new Pen(new SolidColorBrush(Color.Parse("#526170")),1),board);
        var gridPen=new Pen(new SolidColorBrush(Color.Parse("#263542")),0.55);var step=(double)(_layout.GridMillimetres>0?_layout.GridMillimetres:10m);
        for(var x=0d;x<=(double)_layout.WidthMillimetres;x+=step){var a=ToScreen(x,0);var b=ToScreen(x,(double)_layout.HeightMillimetres);ctx.DrawLine(gridPen,a,b);}
        for(var y=0d;y<=(double)_layout.HeightMillimetres;y+=step){ctx.DrawLine(gridPen,ToScreen(0,y),ToScreen((double)_layout.WidthMillimetres,y));}
        foreach(var s in _layout.GeometrySegments.Where(x=>x.LevelId==_levelId))
        {
            var pen=new Pen(new SolidColorBrush(s.Id==_selectedSegment?Color.Parse("#FFC65C"):Color.Parse("#DDE6ED")),s.Id==_selectedSegment?6:4);
            var a=ToScreen((double)s.StartX,(double)s.StartY);var b=ToScreen((double)s.EndX,(double)s.EndY);
            if(s.Kind==TrackGeometryKind.Straight)ctx.DrawLine(pen,a,b);
            else if(s.Kind==TrackGeometryKind.Curve){var prev=a;for(var i=1;i<=40;i++){var t=i/40d;var u=1-t;var p=ToScreen(u*u*(double)s.StartX+2*u*t*(double)s.ControlX+t*t*(double)s.EndX,u*u*(double)s.StartY+2*u*t*(double)s.ControlY+t*t*(double)s.EndY);ctx.DrawLine(pen,prev,p);prev=p;}}
            else{ctx.DrawLine(pen,a,b);var dx=b.X-a.X;var dy=b.Y-a.Y;ctx.DrawLine(pen,new Point(a.X+dx*.48,a.Y+dy*.48),new Point(b.X-dx*.42-dy*.28,b.Y-dy*.42+dx*.28));}
        }
        foreach(var piece in _layout.Pieces.Where(x=>x.LevelId==_levelId))
        {
            var selected=piece.Id==_selectedPiece;var color=piece.GeometryStatus=="confirmed"?"#53BED2":piece.GeometryStatus=="partial"?"#F0AE52":"#A5AEB8";
            if(_catalogue.TryGetValue(piece.CatalogueCode,out var item)&&item.TrackPaths.Count>0)
            {
                var ports=item.Ports.ToDictionary(x=>x.Id,StringComparer.OrdinalIgnoreCase);var angle=(double)piece.RotationDegrees*Math.PI/180;Point Transform(decimal x,decimal y){var xx=(double)x;var yy=(double)y;return ToScreen((double)piece.X+xx*Math.Cos(angle)-yy*Math.Sin(angle),(double)piece.Y+xx*Math.Sin(angle)+yy*Math.Cos(angle));}
                foreach(var route in item.TrackPaths)if(ports.TryGetValue(route.FromPort,out var from)&&ports.TryGetValue(route.ToPort,out var to))
                {var a=Transform(from.X,from.Y);var b=Transform(to.X,to.Y);var pen=new Pen(new SolidColorBrush(Color.Parse(color)),selected?6:4);if(route.Kind=="quadratic"){var c=Transform(route.ControlX,route.ControlY);var prev=a;for(var i=1;i<=32;i++){var t=i/32d;var u=1-t;var p=new Point(u*u*a.X+2*u*t*c.X+t*t*b.X,u*u*a.Y+2*u*t*c.Y+t*t*b.Y);ctx.DrawLine(pen,prev,p);prev=p;}}else ctx.DrawLine(pen,a,b);}
            }
            else ctx.DrawEllipse(new SolidColorBrush(Color.Parse(color)),null,ToScreen((double)piece.X,(double)piece.Y),6,6);
            if(_catalogue.TryGetValue(piece.CatalogueCode,out var cat))foreach(var port in cat.Ports){var wp=WorldPort(piece,port);ctx.DrawEllipse(new SolidColorBrush(Color.Parse(port.Accuracy=="estimated"?"#E89B42":"#75D69A")),new Pen(Brushes.Black,1),ToScreen(wp.X,wp.Y),4.5,4.5);}
            ctx.DrawText(new FormattedText(piece.CatalogueCode,System.Globalization.CultureInfo.CurrentCulture,FlowDirection.LeftToRight,Typeface.Default,10,new SolidColorBrush(Color.Parse(color))),ToScreen((double)piece.X+28,(double)piece.Y-8));
        }
        foreach(var connection in _layout.Connections){var from=_layout.Pieces.FirstOrDefault(x=>x.Id==connection.FromPieceId&&x.LevelId==_levelId);var to=_layout.Pieces.FirstOrDefault(x=>x.Id==connection.ToPieceId&&x.LevelId==_levelId);if(from is null||to is null)continue;
            Point Port(TrackPiece p,string id){var item=_catalogue.GetValueOrDefault(p.CatalogueCode);var port=item?.Ports.FirstOrDefault(x=>x.Id==id);return port is null?new Point((double)p.X,(double)p.Y):WorldPort(p,port);}
            var a=Port(from,connection.FromPort);var b=Port(to,connection.ToPort);ctx.DrawLine(new Pen(new SolidColorBrush(Color.Parse(connection.PhysicallyVerified?"#62D58A":"#E89B42")),2),ToScreen(a.X,a.Y),ToScreen(b.X,b.Y));}
        var status="Eina: "+_tool+" · zoom "+(_zoom*100).ToString("0")+"% · "+_layout.Pieces.Count(x=>x.LevelId==_levelId)+" peces · "+_layout.GeometrySegments.Count(x=>x.LevelId==_levelId)+" trams";
        ctx.DrawText(new FormattedText(status,System.Globalization.CultureInfo.CurrentCulture,FlowDirection.LeftToRight,Typeface.Default,12,Brushes.Gainsboro),new Point(12,Bounds.Height-22));
    }
}