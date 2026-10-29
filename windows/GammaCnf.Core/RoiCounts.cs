namespace GammaCnf.Core;

public sealed record RoiCountSummary(ulong Gross,double ScaledBackground,double Net,double NetSigma)
{
    public static RoiCountSummary Calculate(AnalysisSession session,RoiRange roi)
    {
        if(roi.Start<0||roi.End<roi.Start||roi.End>=session.Values.Length)throw new CnfException("Giới hạn ROI không nằm trong phổ hoặc bị đảo.");
        ulong gross=0;double background=0,net=0,variance=0;
        for(int i=roi.Start;i<=roi.End;i++)
        {
            gross+=session.Sample.Counts[i];background+=session.Scale*(session.Background?.Counts[i]??0);
            net+=session.Values[i];variance+=session.Variances[i];
        }
        return new(gross,background,net,Math.Sqrt(variance));
    }
}
