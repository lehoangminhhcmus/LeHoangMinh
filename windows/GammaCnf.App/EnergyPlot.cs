using System.Globalization;
using System.Windows;
using System.Windows.Media;
using GammaCnf.Core;

namespace GammaCnf.App;

/// <summary>Energy curve for the calibration currently applied to the spectrum.</summary>
public sealed class EnergyPlot : FrameworkElement
{
    public double[]? Coefficients {get;set;}
    public int Channels {get;set;}
    private void Text(DrawingContext dc,string text,double x,double y,TextAlignment alignment=TextAlignment.Left)
    {
        var f=new FormattedText(text,CultureInfo.CurrentCulture,FlowDirection.LeftToRight,new Typeface("Segoe UI"),11,Brushes.DarkSlateGray,VisualTreeHelper.GetDpi(this).PixelsPerDip);
        dc.DrawText(f,new Point(x-(alignment==TextAlignment.Center?f.Width/2:alignment==TextAlignment.Right?f.Width:0),y));
    }
    protected override void OnRender(DrawingContext dc)
    {
        base.OnRender(dc);dc.DrawRectangle(Brushes.White,null,new Rect(RenderSize));
        Text(dc,"Đường chuẩn năng lượng đang áp dụng",8,5);
        if(Coefficients is not {} c||Channels<2){Text(dc,"Mở phổ có hiệu chuẩn năng lượng.",8,35);return;}
        if(ActualWidth<180||ActualHeight<120)return;
        double left=65,top=30,width=ActualWidth-80,height=ActualHeight-65,end=Channels-1;
        double lo=EnergyCalibration.Evaluate(c,0),hi=EnergyCalibration.Evaluate(c,end);
        if(!double.IsFinite(lo)||!double.IsFinite(hi)||hi<=lo)return;
        double X(double ch)=>left+ch/end*width;
        double Y(double e)=>top+height-(e-lo)/(hi-lo)*height;
        var grid=new Pen(Brushes.LightGray,1);
        for(int i=0;i<=4;i++)
        {
            double fraction=i/4d,ch=end*fraction,e=lo+(hi-lo)*fraction;
            dc.DrawLine(grid,new Point(X(ch),top),new Point(X(ch),top+height));
            dc.DrawLine(grid,new Point(left,Y(e)),new Point(left+width,Y(e)));
            Text(dc,ch.ToString("0"),X(ch),top+height+5,i==0?TextAlignment.Left:i==4?TextAlignment.Right:TextAlignment.Center);
            Text(dc,e.ToString("G4"),left-7,Y(e)-6,TextAlignment.Right);
        }
        var curve=new StreamGeometry();using(var ctx=curve.Open())
        {
            for(int i=0;i<=200;i++)
            {
                double ch=end*i/200;var point=new Point(X(ch),Y(EnergyCalibration.Evaluate(c,ch)));
                if(i==0)ctx.BeginFigure(point,false,false);else ctx.LineTo(point,true,false);
            }
        }
        dc.DrawGeometry(null,new Pen(Brushes.SteelBlue,2),curve);
        Text(dc,"keV",3,top-16);Text(dc,"Kênh (từ 0)",left+width/2-30,ActualHeight-15);
    }
    protected override void OnRenderSizeChanged(SizeChangedInfo info){base.OnRenderSizeChanged(info);InvalidateVisual();}
}
