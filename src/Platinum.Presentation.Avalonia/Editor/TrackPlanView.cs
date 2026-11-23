using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Platinum.Domain.Layout;
using Platinum.Presentation.Avalonia.Catalogue;

namespace Platinum.Presentation.Avalonia.Editor;

public sealed class TrackPlanView : Control
{
    private const double Pad=28;
    private TrackLayout? _layout;
    private Guid _levelId;
    private string _tool="Seleccionar";
    private Point? _start;
    private CatalogueItem? _placement;
    private readonly Dictionary<string,CatalogueItem> _catalogue=new(StringComparer.OrdinalIgnoreCase);

    public TrackPlanView() { ClipToBounds=true; Focusable=true; }
    public void SetLayout(TrackLayout layout,Guid levelId) { _layout=layout; _levelId=levelId; _start=null; InvalidateVisual(); }
    public void SetActiveLevel(Guid id) { _levelId=id; _start=null; InvalidateVisual(); }
    public void SetTool(string tool) { _tool=tool; _start=null; _placement=null; InvalidateVisual(); }
    public void BeginCataloguePlacement(CatalogueItem item) { _placement=item; _tool="Col·locar peça"; _start=null; Focus(); }
    public void SetCatalogue(IEnumerable<CatalogueItem> items) { _catalogue.Clear(); foreach(var item in items) _catalogue[item.Ref]=item; InvalidateVisual(); }

    protected override void OnRender(DrawingContext ctx)
    {
        base.OnRender(ctx);
        ctx.DrawRectangle(new SolidColorBrush(Color.Parse("#121B24")),null,Bounds);
        if(_layout is null || Bounds.Width<80 || Bounds.Height<80) return;
        var scale=Math.Min((Bounds.Width-Pad*2)/(double)_layout.WidthMillimetres,(Bounds.Height-Pad*2)/(double)_layout.HeightMillimetres);
        var origin=new Point(Pad,Pad);
        Point Map(decimal x,decimal y)=>new(origin.X+(double)x*scale,origin.Y+(double)y*scale);
        var board=new Rect(origin.X,origin.Y,(double)_layout.WidthMillimetres*scale,(double)_layout.HeightMillimetres*scale);
        ctx.DrawRectangle(new SolidColorBrush(Color.Parse("#17232E")),new Pen(new SolidColorBrush(Color.Parse("#526170")),1),board);
        var grid=new Pen(new SolidColorBrush(Color.Parse("#263542")),0.55);
        var step=Math.Max(10,(double)_layout.GridMillimetres);
        for(var x=0d;x<=(double)_layout.WidthMillimetres;x+=step) ctx.DrawLine(grid,new Point(origin.X+x*scale,origin.Y),new Point(origin.X+x*scale,origin.Y+board.Height));
        for(var y=0d;y<=(double)_layout.HeightMillimetres;y+=step) ctx.DrawLine(grid,new Point(origin.X,origin.Y+y*scale),new Point(origin.X+board.Width,origin.Y+y*scale));
        var rail=new Pen(new SolidColorBrush(Color.Parse("#D9B66D")),2.2);
        foreach(var s in _layout.GeometrySegments.Where(x=>x.LevelId==_levelId))
        {
            var a=Map(s.StartX,s.StartY); var b=Map(s.EndX,s.EndY);
            if(s.Kind==TrackGeometryKind.Straight) ctx.DrawLine(rail,a,b);
            else
            {
                var prev=a;
                for(var i=1;i<=32;i++)
                {
                    var t=i/32d; var u=1d-t;
                    var px=u*u*(double)s.StartX+2*u*t*(double)s.ControlX+t*t*(double)s.EndX;
                    var py=u*u*(double)s.StartY+2*u*t*(double)s.ControlY+t*t*(double)s.EndY;
                    var next=Map((decimal)px,(decimal)py); ctx.DrawLine(rail,prev,next); prev=next;
                }
                if(s.Kind==TrackGeometryKind.Turnout) ctx.DrawLine(rail,a,new Point(b.X,b.Y+Math.Max(18,80*scale)));
            }
        }
        foreach(var p in _layout.Pieces.Where(x=>x.LevelId==_levelId))
        {
            var c=Map(p.X,p.Y);
            var len=_catalogue.TryGetValue(p.CatalogueCode,out var item)?(double)(item.LengthMm??188.3m)*scale:32;
            var r=(double)p.RotationDegrees*Math.PI/180d; var dx=Math.Cos(r)*len/2; var dy=Math.Sin(r)*len/2;
            ctx.DrawLine(new Pen(new SolidColorBrush(Color.Parse("#8CC7E8")),4),new Point(c.X-dx,c.Y-dy),new Point(c.X+dx,c.Y+dy));
            ctx.DrawEllipse(new SolidColorBrush(Color.Parse("#F3D28A")),null,c,4,4);
        }
        var label="Nivell actiu · "+_tool+" · "+_layout.Pieces.Count(x=>x.LevelId==_levelId)+" peces";
        ctx.DrawText(new FormattedText(label,System.Globalization.CultureInfo.CurrentCulture,FlowDirection.LeftToRight,Typeface.Default,12,Brushes.Gainsboro),new Point(Pad,Math.Max(Pad,Bounds.Height-24)));
    }

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);
        if(_layout is null || !e.GetCurrentPoint(this).Properties.IsLeftButtonPressed) return;
        var p=e.GetPosition(this);
        var scale=Math.Min((Bounds.Width-Pad*2)/(double)_layout.WidthMillimetres,(Bounds.Height-Pad*2)/(double)_layout.HeightMillimetres);
        if(scale<=0) return;
        var grid=_layout.GridMillimetres>0?_layout.GridMillimetres:10m;
        var x=Math.Round((decimal)Math.Clamp((p.X-Pad)/scale,0,(double)_layout.WidthMillimetres)/grid,MidpointRounding.AwayFromZero)*grid;
        var y=Math.Round((decimal)Math.Clamp((p.Y-Pad)/scale,0,(double)_layout.HeightMillimetres)/grid,MidpointRounding.AwayFromZero)*grid;
        if(_placement is not null)
        {
            _layout.Pieces.Add(new TrackPiece(Guid.NewGuid(),_placement.Ref,x,y,0m,_levelId,_placement.GeometryStatus));
            _placement=null; _tool="Seleccionar"; InvalidateVisual(); e.Handled=true; return;
        }
        if(_tool=="Seleccionar") return;
        if(_start is null) { _start=new Point((double)x,(double)y); e.Handled=true; return; }
        var a=_start.Value;
        var kind=_tool=="Corba"?TrackGeometryKind.Curve:_tool=="Desviament"?TrackGeometryKind.Turnout:TrackGeometryKind.Straight;
        var cx=(decimal)((a.X+(double)x)/2d);
        var cy=(decimal)((a.Y+(double)y)/2d-Math.Abs((double)x-a.X)*0.25d);
        _layout.GeometrySegments.Add(new TrackGeometrySegment(Guid.NewGuid(),kind,(decimal)a.X,(decimal)a.Y,x,y,cx,cy,_levelId));
        _start=null; InvalidateVisual(); e.Handled=true;
    }
}