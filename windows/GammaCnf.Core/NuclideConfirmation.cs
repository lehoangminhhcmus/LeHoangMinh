namespace GammaCnf.Core;

public sealed record ConfirmationMarker(string Nuclide,double EnergyKeV,double Channel,double? IntensityPercent);
public static class NuclideConfirmation
{
    public static IReadOnlyList<ConfirmationMarker> Markers(Spectrum spectrum,IEnumerable<NuclideLine> lines)
    {
        if(spectrum.Calibration is not {} calibration||spectrum.Counts.Length<2)return [];
        EnergyCalibration.Validate(calibration,spectrum.Counts.Length);
        double end=spectrum.Counts.Length-1,minimum=EnergyCalibration.Evaluate(calibration,0),maximum=EnergyCalibration.Evaluate(calibration,end);
        var markers=new List<ConfirmationMarker>();
        foreach(var line in lines.Where(l=>double.IsFinite(l.EnergyKeV)&&l.EnergyKeV>0&&l.EnergyKeV>=minimum&&l.EnergyKeV<=maximum&&l.IntensityPercent!=0).GroupBy(l=>l.EnergyKeV).Select(g=>g.OrderByDescending(l=>l.IntensityPercent).First()).OrderBy(l=>l.EnergyKeV))
        {
            double lo=0,hi=end;
            for(int i=0;i<60;i++){double mid=(lo+hi)/2;if(EnergyCalibration.Evaluate(calibration,mid)<line.EnergyKeV)lo=mid;else hi=mid;}
            markers.Add(new(line.Nuclide,line.EnergyKeV,(lo+hi)/2,line.IntensityPercent));
        }
        return markers;
    }
}
