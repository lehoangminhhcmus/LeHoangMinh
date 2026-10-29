using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;
using GammaCnf.Core;

namespace GammaCnf.App;
public sealed class EfficiencyPlot : Control
{
    public EfficiencyProfile? Profile {get;set;}
    private static readonly Brush Ink=new SolidColorBrush(Color.FromRgb(45,65,90));
    private void Text(DrawingContext dc,string text,double x,double y,double size=12,TextAlignment alignment=TextAlignment.Left)
    {
        var f=new FormattedText(text,CultureInfo.CurrentCulture,FlowDirection.LeftToRight,new Typeface(FontFamily.Default),size,Ink){TextAlignment=TextAlignment.Left};dc.DrawText(f,new Point(x-(alignment==TextAlignment.Center?f.Width/2:alignment==TextAlignment.Right?f.Width:0),y));
    }
    public override void Render(DrawingContext dc)
    {
        base.Render(dc);dc.DrawRectangle(Brushes.White,null,new Rect(Bounds.Size));
        if(Profile is not {} p){Text(dc,"Thêm điểm chuẩn rồi bấm Fit để xem đường cong và phần dư.",30,40,16);return;}
        if(Bounds.Width<180||Bounds.Height<220)return;
        double left=85,width=Math.Max(1,Bounds.Width-120),height=Math.Max(60,(Bounds.Height-145)*.65),top=35,resTop=top+height+65,resHeight=Math.Max(45,Bounds.Height-resTop-40);
        double x0=Math.Log(p.MinEnergy),x1=Math.Log(p.MaxEnergy);
        var logValues=p.Definition.Points.SelectMany(o=>new[]{Math.Log(o.Efficiency)-o.TotalRelativeSigma,Math.Log(o.Efficiency)+o.TotalRelativeSigma}).ToList();
        for(int i=0;i<=120;i++)logValues.Add(Math.Log(p.Evaluate(Math.Clamp(Math.Exp(x0+(x1-x0)*i/120),p.MinEnergy,p.MaxEnergy)).Value));
        double y0=logValues.Min()-.35,y1=logValues.Max()+.35;
        double X(double energy)=>left+(Math.Log(energy)-x0)/(x1-x0)*width;
        double Y(double log)=>top+height-(log-y0)/(y1-y0)*height;
        double rmax=Math.Max(3.5,p.ResidualSigma.Select(Math.Abs).Max()*1.15);
        double R(double value)=>resTop+resHeight/2-value/(2*rmax)*resHeight;
        var grid=new Pen(new SolidColorBrush(Color.FromRgb(227,233,242)),1);
        for(int i=0;i<=5;i++)
        {
            double f=i/5.0,x=left+f*width,log=y0+f*(y1-y0);
            dc.DrawLine(grid,new Point(x,top),new Point(x,top+height));dc.DrawLine(grid,new Point(left,Y(log)),new Point(left+width,Y(log)));
            Text(dc,Math.Exp(log).ToString("G3"),left-8,Y(log)-8,12,TextAlignment.Right);
            Text(dc,Math.Exp(x0+f*(x1-x0)).ToString("0.##"),x,resTop+resHeight+8,12,TextAlignment.Center);
        }
        Text(dc,"Hiệu suất đỉnh (fraction), trục log; điểm ±1σ • đường fit GLS",left,8);
        Text(dc,"Phần dư ln(ε đo/ε fit) / σ_lnε đầu vào (không phải phần dư studentized)",left,resTop-25,11);
        foreach(double value in new[]{-3d,0,3d})
        {
            dc.DrawLine(new Pen(value==0?Brushes.Gray:Brushes.Orange,1),new Point(left,R(value)),new Point(left+width,R(value)));Text(dc,value.ToString("0"),left-8,R(value)-8,11,TextAlignment.Right);
        }
        var curve=new StreamGeometry();using(var c=curve.Open())
        {
            for(int i=0;i<=160;i++)
            {
                double e=Math.Clamp(Math.Exp(x0+(x1-x0)*i/160),p.MinEnergy,p.MaxEnergy);var point=new Point(X(e),Y(Math.Log(p.Evaluate(e).Value)));
                if(i==0)c.BeginFigure(point,false);else c.LineTo(point);
            }
            c.EndFigure(false);
        }
        dc.DrawGeometry(null,new Pen(Brushes.SteelBlue,2),curve);
        int index=0;foreach(var point in p.Definition.Points)
        {
            double x=X(point.Gamma.EnergyKeV),y=Math.Log(point.Efficiency),sigma=point.TotalRelativeSigma;
            IBrush color=Math.Abs(p.ResidualSigma[index])>3?Brushes.Firebrick:Brushes.DarkBlue;
            var pen=new Pen(color,1.2);dc.DrawLine(pen,new Point(x,Y(y-sigma)),new Point(x,Y(y+sigma)));
            dc.DrawLine(pen,new Point(x-4,Y(y-sigma)),new Point(x+4,Y(y-sigma)));dc.DrawLine(pen,new Point(x-4,Y(y+sigma)),new Point(x+4,Y(y+sigma)));
            dc.DrawEllipse(color,null,new Point(x,Y(y)),3.5,3.5);dc.DrawEllipse(color,null,new Point(x,R(p.ResidualSigma[index])),3.5,3.5);index++;
        }
        Text(dc,"Năng lượng (keV), trục log",left+width/2,Bounds.Height-20,12,TextAlignment.Center);
    }
    protected override void OnSizeChanged(SizeChangedEventArgs e){base.OnSizeChanged(e);InvalidateVisual();}
}
