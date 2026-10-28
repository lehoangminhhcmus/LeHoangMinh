using System.Globalization;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using GammaCnf.Core;

namespace GammaCnf.App;

public sealed class SpectrumPlot : FrameworkElement
{
    public Spectrum? Spectrum { get; set; }
    public double[]? Values { get; set; }
    public IReadOnlyList<RoiResult> Rois { get; set; } = [];
    public event Action<int,int>? RoiSelected;
    public bool EnergyAxis { get; set; }
    public bool LogScale { get; set; }
    public event Action<string>? HoverChanged;
    private double left, right;
    private double? selection;
    private double selectionEnd;
    private Rect Area => new(78, 28, Math.Max(1, ActualWidth - 106), Math.Max(1, ActualHeight - 94));
    private static readonly Brush Ink = new SolidColorBrush(Color.FromRgb(53, 73, 99));
    private static readonly Pen GridPen = new(new SolidColorBrush(Color.FromRgb(230, 236, 244)), 1);
    private static readonly Pen LinePen = new(new SolidColorBrush(Color.FromRgb(23, 101, 174)), 1);
    public SpectrumPlot()
    {
        Cursor = Cursors.Cross;
        Focusable = true;
        SnapsToDevicePixels = true;
    }
    public void Reset()
    {
        left = 0; right = Math.Max(1, (Spectrum?.Counts.Length ?? 2) - 1);
        selection = null; ReleaseMouseCapture(); InvalidateVisual();
    }
    private double XValue(double channel) => EnergyAxis ? Spectrum?.Energy(channel) ?? channel : channel;
    private double PixelChannel(double pixel) => left + Math.Clamp((pixel-Area.Left)/Area.Width, 0, 1) * (right-left);
    // X pixel mapping uses actual calibrated energies (including quadratic/cubic terms).
    private double XPixel(double channel) => Area.Left + (XValue(channel)-XValue(left))/(XValue(right)-XValue(left))*Area.Width;
    private double ChannelAt(double pixel)
    {
        if (!EnergyAxis) return PixelChannel(pixel);
        double target = XValue(left) + Math.Clamp((pixel-Area.Left)/Area.Width, 0, 1)*(XValue(right)-XValue(left));
        double a = left, b = right;
        for (int i=0;i<40;i++) { double mid=(a+b)/2; if(XValue(mid)<target) a=mid; else b=mid; }
        return (a+b)/2;
    }
    private void Text(DrawingContext dc, string text, Point origin, double size=12, TextAlignment alignment=TextAlignment.Left)
    {
        var formatted = new FormattedText(text, CultureInfo.CurrentCulture, FlowDirection.LeftToRight,
            new Typeface("Segoe UI"), size, Ink, VisualTreeHelper.GetDpi(this).PixelsPerDip) { TextAlignment=alignment };
        dc.DrawText(formatted, origin);
    }
    protected override void OnRender(DrawingContext dc)
    {
        base.OnRender(dc);
        dc.DrawRectangle(Brushes.White, null, new Rect(RenderSize));
        var area=Area;
        if (Spectrum is not { } s)
        {
            Text(dc, "Mở hoặc kéo thả phổ gamma Canberra .CNF", new Point(ActualWidth/2, ActualHeight/2-15), 20, TextAlignment.Center);
            return;
        }
        if (right <= left) Reset();
        int first=Math.Max(0, (int)Math.Floor(left)), last=Math.Min(s.Counts.Length-1,(int)Math.Ceiling(right));
        double Value(int i) => Values?[i] ?? s.Counts[i];
        double maximum=1, minimum=0, positiveMin=double.PositiveInfinity;
        for(int i=first;i<=last;i++) { maximum=Math.Max(maximum,Value(i));minimum=Math.Min(minimum,Value(i));if(Value(i)>0)positiveMin=Math.Min(positiveMin,Value(i)); }
        double bottom=LogScale ? (double.IsPositiveInfinity(positiveMin)?0:Math.Min(0,Math.Floor(Math.Log10(positiveMin)))) : minimum*1.08;
        double top=LogScale ? Math.Max(bottom+1,Math.Log10(maximum)+.1) : maximum*1.08;
        double Y(double count) => area.Bottom - ((LogScale ? Math.Log10(Math.Max(1e-12,count)) : count)-bottom)/(top-bottom)*area.Height;
        for(int i=0;i<=5;i++)
        {
            double f=i/5.0;
            double x=area.Left+f*area.Width,y=area.Bottom-f*area.Height;
            dc.DrawLine(GridPen,new Point(x,area.Top),new Point(x,area.Bottom));
            dc.DrawLine(GridPen,new Point(area.Left,y),new Point(area.Right,y));
            Text(dc,(XValue(left)+f*(XValue(right)-XValue(left))).ToString("0.##"),new Point(x,area.Bottom+8),12,TextAlignment.Center);
            Text(dc,(LogScale ? Math.Pow(10,bottom+f*(top-bottom)) : bottom+f*(top-bottom)).ToString("0.##"),new Point(area.Left-8,y-8),12,TextAlignment.Right);
        }
        Text(dc, EnergyAxis ? "Năng lượng (keV)" : "Kênh (bắt đầu từ 0)",new Point(area.Left+area.Width/2,area.Bottom+35),13,TextAlignment.Center);
        Text(dc,LogScale ? "Số đếm / kênh • log (ẩn số đếm ≤ 0)" : "Số đếm / kênh",new Point(area.Left,3));
        dc.PushClip(new RectangleGeometry(area));
        int roiId=0;
        foreach(var roi in Rois)
        {
            roiId++;
            double a=XPixel(roi.Range.Start-.5),b=XPixel(roi.Range.End+.5);
            dc.DrawRectangle(new SolidColorBrush(Color.FromArgb(22,230,146,37)),null,new Rect(a,area.Top,Math.Max(0,b-a),area.Height));
            if(b>=area.Left && a<=area.Right) Text(dc,$"ROI {roiId}",new Point(Math.Max(area.Left,a)+3,area.Top+5),11);
        }
        var geometry=new StreamGeometry();
        using(var ctx=geometry.Open())
        {
            bool active=false;
            for(int i=first;i<=last;i++)
            {
                if(LogScale && Value(i)<=0) { active=false; continue; }
                var point=new Point(XPixel(i),Y(Value(i)));
                if(!active) { ctx.BeginFigure(point,false,false); active=true; }
                else ctx.LineTo(point,true,false);
            }
        }
        geometry.Freeze();dc.DrawGeometry(null,LinePen,geometry);
        foreach(var roi in Rois.Where(r=>r.Success))
        {
            var fit=new StreamGeometry();using(var ctx=fit.Open())
            {
                bool active=false;
                for(int i=Math.Max(first,roi.Range.Start);i<=Math.Min(last,roi.Range.End);i++)
                {
                    double v=roi.Model(i);if(LogScale&&v<=0) {active=false;continue;}
                    var point=new Point(XPixel(i),Y(v));
                    if(!active){ctx.BeginFigure(point,false,false);active=true;}else ctx.LineTo(point,true,false);
                }
            }
            dc.DrawGeometry(null,new Pen(Brushes.IndianRed,1.5),fit);
        }
        if(selection is double start)
        {
            double a=Math.Min(start,selectionEnd), b=Math.Max(start,selectionEnd);
            dc.DrawRectangle(new SolidColorBrush(Color.FromArgb(45,23,101,174)),LinePen,new Rect(a,area.Top,b-a,area.Height));
        }
        dc.Pop();
    }
    protected override void OnMouseWheel(MouseWheelEventArgs e)
    {
        if(Spectrum is null || !Area.Contains(e.GetPosition(this))) return;
        double center=ChannelAt(e.GetPosition(this).X), factor=e.Delta>0 ? .75 : 1.3333333;
        double length=Math.Min(Math.Max(1,Spectrum.Counts.Length-1), Math.Max(2,(right-left)*factor));
        double fraction=(center-left)/(right-left);
        left=Math.Clamp(center-fraction*length,0,Math.Max(0,Spectrum.Counts.Length-1-length));
        right=left+length;InvalidateVisual();e.Handled=true;
    }
    protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
    {
        if(Spectrum is null || !Area.Contains(e.GetPosition(this))) return;
        selection=selectionEnd=e.GetPosition(this).X;CaptureMouse();Focus();
    }
    protected override void OnMouseMove(MouseEventArgs e)
    {
        if(Spectrum is null) return;
        Point pos=e.GetPosition(this);
        if(selection.HasValue) { selectionEnd=Math.Clamp(pos.X,Area.Left,Area.Right);InvalidateVisual(); }
        if(!Area.Contains(pos)) return;
        int i=Math.Clamp((int)Math.Round(ChannelAt(pos.X)),0,Spectrum.Counts.Length-1);
        string energy=Spectrum.Energy(i) is double value ? $"   •   {value:F3} keV" : "";
        HoverChanged?.Invoke($"Kênh {i}{energy}   •   Gốc: {Spectrum.Counts[i]:N0}   •   Đang xem: {Values?[i] ?? Spectrum.Counts[i]:N3} số đếm");
    }
    protected override void OnMouseLeftButtonUp(MouseButtonEventArgs e)
    {
        if(selection is not double start) return;
        selectionEnd=Math.Clamp(e.GetPosition(this).X,Area.Left,Area.Right);
        if(Math.Abs(start-selectionEnd)>8)
        {
            double a=ChannelAt(Math.Min(start,selectionEnd)), b=ChannelAt(Math.Max(start,selectionEnd));
            if(Keyboard.Modifiers.HasFlag(ModifierKeys.Shift))
                RoiSelected?.Invoke(Math.Max(0,(int)Math.Floor(a)),Math.Min(Spectrum!.Counts.Length-1,(int)Math.Ceiling(b)));
            else if(b-a>=1) { left=a;right=b; }
        }
        selection=null;ReleaseMouseCapture();InvalidateVisual();
    }
    protected override void OnRenderSizeChanged(SizeChangedInfo info) { base.OnRenderSizeChanged(info);InvalidateVisual(); }
}
