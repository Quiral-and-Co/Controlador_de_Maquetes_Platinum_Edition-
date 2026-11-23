using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Platinum.Domain.Layout;

namespace Platinum.Presentation.Avalonia.Editor;

public sealed class ThreeDTrackView : Control
{
    private TrackLayout? _layout;
    private Guid _levelId;
    private double _yaw=-42, _pitch=38, _zoom=1;
    private Point? _dragStart;
    private readonly Dictionary<string,CatalogueItem> _catalogue=new(StringComparer.OrdinalIgnoreCase);
    public void SetCatalogue(IEnumerable<CatalogueItem> items){_catalogue.Clear();foreach(var item in items)_catalogue[item.Ref]=item;InvalidateVisual();}
    public void ZoomIn()=>Zoom(0.82);
    public void ZoomOut()=>Zoom(1.22);
    private void Zoom(double factor){_zoom=Math.Clamp(_zoom*factor,0.2,5);InvalidateVisual();}
    public ThreeDTrackView(){ClipToBounds=true;Focusable=true;}
    public void SetLayout(TrackLayout layout,Guid levelId){_layout=layout;_levelId=levelId;InvalidateVisual();}
    public void SetActiveLevel(Guid levelId){_levelId=levelId;InvalidateVisual();}
    public void TopView(){_yaw=0;_pitch=82;InvalidateVisual();}
    public void IsometricView(){_yaw=-42;_pitch=38;InvalidateVisual();}
    public void SideView(){_yaw=-90;_pitch=12;InvalidateVisual();}
    public override void Render(DrawingContext ctx)
    {
        base.OnRender(ctx);ctx.DrawRectangle(new SolidColorBrush(Color.Parse("#10171F")),null,Bounds);
        if(_layout is null||Bounds.Width<100||Bounds.Height<100)return;
        var yaw=_yaw*Math.PI/180;var pitch=_pitch*Math.PI/180;var cy=Math.Cos(yaw);var sy=Math.Sin(yaw);var cp=Math.Cos(pitch);var sp=Math.Sin(pitch);
        (double X,double Y) Project(double x,double y,double z){var rx=x*cy-y*sy;var ry=x*sy+y*cy;return(rx,ry*cp-z*sp);}
        var corners=new[]{Project(0,0,0),Project((double)_layout.WidthMillimetres,0,0),Project(0,(double)_layout.HeightMillimetres,0),Project((double)_layout.WidthMillimetres,(double)_layout.HeightMillimetres,0)};
        var minX=corners.Min(p=>p.X);var maxX=corners.Max(p=>p.X);var minY=corners.Min(p=>p.Y);var maxY=corners.Max(p=>p.Y);
        var scale=Math.Min((Bounds.Width-70)/Math.Max(1,maxX-minX),(Bounds.Height-70)/Math.Max(1,maxY-minY))*_zoom;
        Point Map(double x,double y,double z){var p=Project(x,y,z);return new Point(35+(p.X-minX)*scale,35+(p.Y-minY)*scale);}
        var board=corners.Select(p=>new Point(35+(p.X-minX)*scale,35+(p.Y-minY)*scale)).ToArray();
        for(var i=0;i<4;i++)ctx.DrawLine(new Pen(new SolidColorBrush(Color.Parse("#53616E")),1.2),board[i],board[(i+1)%4]);
        var elevations=_layout.Levels.ToDictionary(x=>x.Id,x=>x.ElevationMillimetres);
        foreach(var s in _layout.GeometrySegments)
        {
            var z=(double)elevations.GetValueOrDefault(s.LevelId,0m);var color=s.LevelId==_levelId?"#E6BD70":"#64788A";var pen=new Pen(new SolidColorBrush(Color.Parse(color)),2);
            if(s.Kind==TrackGeometryKind.Straight)ctx.DrawLine(pen,Map((double)s.StartX,(double)s.StartY,z),Map((double)s.EndX,(double)s.EndY,z));
            else{var prev=Map((double)s.StartX,(double)s.StartY,z);for(var i=1;i<=24;i++){var t=i/24d;var u=1-t;var x=u*u*(double)s.StartX+2*u*t*(double)s.ControlX+t*t*(double)s.EndX;var y=u*u*(double)s.StartY+2*u*t*(double)s.ControlY+t*t*(double)s.EndY;var next=Map(x,y,z);ctx.DrawLine(pen,prev,next);prev=next;}}
        }
        foreach(var p in _layout.Pieces)
        {
            var z=(double)elevations.GetValueOrDefault(p.LevelId,0m);
            if(_catalogue.TryGetValue(p.CatalogueCode,out var item)&&item.TrackPaths.Count>0)
            {
                var ports=item.Ports.ToDictionary(x=>x.Id,StringComparer.OrdinalIgnoreCase);
                var angle=(double)p.RotationDegrees*Math.PI/180;
                Point Transform(decimal x,decimal y){var xx=(double)x;var yy=(double)y;return Map((double)p.X+xx*Math.Cos(angle)-yy*Math.Sin(angle),(double)p.Y+xx*Math.Sin(angle)+yy*Math.Cos(angle),z);}
                var pen=new Pen(new SolidColorBrush(Color.Parse(p.LevelId==_levelId?"#8CC7E8":"#536C7D")),2.6);
                foreach(var route in item.TrackPaths)
                {
                    if(!ports.TryGetValue(route.FromPort,out var from)||!ports.TryGetValue(route.ToPort,out var to))continue;
                    var a=Transform(from.X,from.Y);var b=Transform(to.X,to.Y);
                    if(route.Kind=="quadratic"&&ports.ContainsKey(route.ToPort))
                    {
                        var control=Transform(route.ControlX,route.ControlY);var prev=a;
                        for(var i=1;i<=24;i++){var t=i/24d;var u=1-t;var next=new Point(u*u*a.X+2*u*t*control.X+t*t*b.X,u*u*a.Y+2*u*t*control.Y+t*t*b.Y);ctx.DrawLine(pen,prev,next);prev=next;}
                    }
                    else ctx.DrawLine(pen,a,b);
                }
            }
            else {var point=Map((double)p.X,(double)p.Y,z);ctx.DrawEllipse(new SolidColorBrush(Color.Parse(p.LevelId==_levelId?"#8CC7E8":"#536C7D")),null,point,4,4);}
        }
        foreach(var connection in _layout.Connections)
        {
            var from=_layout.Pieces.FirstOrDefault(x=>x.Id==connection.FromPieceId);
            var to=_layout.Pieces.FirstOrDefault(x=>x.Id==connection.ToPieceId);
            if(from is null||to is null||from.LevelId!=to.LevelId)continue;
            Point Port(TrackPiece piece,string id)
            {
                var z=(double)elevations.GetValueOrDefault(piece.LevelId,0m);
                if(!_catalogue.TryGetValue(piece.CatalogueCode,out var item))return Map((double)piece.X,(double)piece.Y,z);
                var port=item.Ports.FirstOrDefault(x=>x.Id==id);if(port is null)return Map((double)piece.X,(double)piece.Y,z);
                var a=(double)piece.RotationDegrees*Math.PI/180;return Map((double)piece.X+(double)port.X*Math.Cos(a)-(double)port.Y*Math.Sin(a),(double)piece.Y+(double)port.X*Math.Sin(a)+(double)port.Y*Math.Cos(a),z);
            }
            var a=Port(from,connection.FromPort);var b=Port(to,connection.ToPort);
            ctx.DrawLine(new Pen(new SolidColorBrush(Color.Parse(connection.PhysicallyVerified?"#62D58A":"#E89B42")),1.5),a,b);
        }
        var text="Vista axonomètrica · "+_layout.Levels.Count+" nivells · "+_layout.Pieces.Count+" peces";
        ctx.DrawText(new FormattedText(text,System.Globalization.CultureInfo.CurrentCulture,FlowDirection.LeftToRight,Typeface.Default,12,Brushes.Gainsboro),new Point(16,Bounds.Height-24));
    }
    protected override void OnPointerPressed(PointerPressedEventArgs e){base.OnPointerPressed(e);if(e.GetCurrentPoint(this).Properties.IsLeftButtonPressed){_dragStart=e.GetPosition(this);e.Pointer.Capture(this);}}
    protected override void OnPointerMoved(PointerEventArgs e){base.OnPointerMoved(e);if(_dragStart is not Point start||!e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)return;var now=e.GetPosition(this);_yaw+=(now.X-start.X)*0.35;_pitch=Math.Clamp(_pitch+(now.Y-start.Y)*0.25,5,88);_dragStart=now;InvalidateVisual();}
    protected override void OnPointerReleased(PointerReleasedEventArgs e){base.OnPointerReleased(e);_dragStart=null;e.Pointer.Capture(null);}
    protected override void OnPointerWheelChanged(PointerWheelEventArgs e){base.OnPointerWheelChanged(e);_zoom=Math.Clamp(_zoom*(e.Delta.Y>0?1.12:0.89),0.25,4);InvalidateVisual();e.Handled=true;}
}