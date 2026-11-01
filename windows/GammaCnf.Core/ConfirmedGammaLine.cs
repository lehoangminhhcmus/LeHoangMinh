namespace GammaCnf.Core;

/// <summary>An explicit user choice tied to one ROI and one analysis revision, never an automatic identification.</summary>
public sealed record ConfirmedGammaLine(RoiRange Roi,long Revision,Guid SessionId,NuclideLine Line)
{
    public static ConfirmedGammaLine Create(AnalysisSession session,RoiResult roi,NuclideSuggestion suggestion)
    {
        if(!roi.Success||roi.EnergyKeV is not double energy||!double.IsFinite(energy)
            ||!double.IsFinite(suggestion.MeasuredKeV)||!double.IsFinite(suggestion.LibraryKeV)||suggestion.LibraryKeV<=0
            ||Math.Abs(energy-suggestion.MeasuredKeV)>1e-8||!double.IsFinite(suggestion.ToleranceKeV)
            ||suggestion.ToleranceKeV<=0||Math.Abs(energy-suggestion.LibraryKeV)>suggestion.ToleranceKeV)
            throw new CnfException("ROI hoặc gợi ý đã thay đổi; cập nhật gợi ý rồi xác nhận lại vạch.");
        return new(roi.Range,session.Revision,session.Id,new(suggestion.Nuclide,suggestion.LibraryKeV,suggestion.IntensityPercent,suggestion.Reference,suggestion.FileName));
    }
    public bool IsCurrent(AnalysisSession session,RoiResult roi)=>Revision==session.Revision&&SessionId==session.Id&&Roi==roi.Range&&roi.Success&&roi.EnergyKeV is double e&&double.IsFinite(e);
    public GammaEmission Emission(AnalysisSession session,RoiResult roi)
    {
        if(!IsCurrent(session,roi))throw new CnfException("Chưa xác nhận vạch cho ROI này hoặc phép phân tích đã thay đổi. Xác nhận lại trong tab Gợi ý đồng vị.");
        if(Line.IntensityPercent is not double p||!double.IsFinite(p)||p<=0||p>100)
            throw new CnfException("Vạch thư viện thiếu Pγ hợp lệ trong (0,100]%. Chọn nhập tay từ nguồn dữ liệu phù hợp.");
        return new(Line.EnergyKeV,p/100,0,$"{Line.Reference} | {Line.FileName} | Pγ từ vạch {Line.Nuclide} đã được người dùng xác nhận; thư viện chưa cung cấp sai số Pγ",true);
    }
}
