using System.Globalization;
using System.Text;
namespace GammaCnf.Core;

public sealed record ActivityRequest(string Nuclide,GammaEmission Gamma,MeasurementGeometry Geometry,string MeasurementId,
    DateTimeOffset MeasuredAt,DateTimeOffset ReportDate,double LiveSeconds,double RealSeconds,
    double HalfLifeDays,double HalfLifeSigmaDays,double Correction,double CorrectionSigma,string CorrectionNotes,
    double? MassKg,double MassSigmaKg,string HalfLifeReference="");
public sealed record ActivityResult(double BqAtStart,double SigmaBqAtStart,double BqAtReportDate,double SigmaBqAtReportDate,
    double? BqPerKg,double? SigmaBqPerKg,EfficiencyEstimate Efficiency,double DecayAverageFactor,string Notes);

public static class ActivityAnalysis
{
    public static ActivityResult Calculate(EfficiencyProfile profile,RoiResult roi,ActivityRequest request)
    {
        request.Geometry.Validate();request.Gamma.Validate();RadioactiveDecay.ValidateTimes(request.LiveSeconds,request.RealSeconds);
        if(!profile.Definition.Geometry.Matches(request.Geometry))throw new CnfException("Detector/hình học/khoảng cách/ghi chú mẫu khác bộ chuẩn. Không tự chuyển đổi hiệu suất.");
        if(string.IsNullOrWhiteSpace(request.Nuclide)||string.IsNullOrWhiteSpace(request.MeasurementId))throw new CnfException("Nhập đồng vị và thông tin phép đo.");
        if(profile.Definition.Points.Any(p=>p.MeasurementId==request.MeasurementId))throw new CnfException("Đây là phổ đã dùng để tạo đường chuẩn. Tính hoạt độ yêu cầu phép đo mẫu độc lập.");
        if(!roi.Success||!(roi.PeakArea>0)||!(roi.AreaError>0)||roi.ReducedChiSquare is null||roi.EnergyKeV is null||roi.FwhmKeV is null)
            throw new CnfException("Chọn ROI fit hợp lệ với hiệu chuẩn năng lượng.");
        if(Math.Abs(roi.EnergyKeV.Value-request.Gamma.EnergyKeV)>Math.Max(.2,roi.FwhmKeV.Value/2))throw new CnfException("Vạch gamma không khớp ROI.");
        Guard.Positive(request.HalfLifeDays,"Chu kỳ bán rã");Guard.Nonnegative(request.HalfLifeSigmaDays,"Sai số bán rã");
        if(request.HalfLifeSigmaDays/request.HalfLifeDays>.2)throw new CnfException("Sai số bán rã vượt 20%; ngoài phạm vi lan truyền tuyến tính.");
        Guard.Positive(request.Correction,"K hiệu chỉnh hoạt độ");Guard.Nonnegative(request.CorrectionSigma,"Sai số K");
        if((request.Correction!=1||request.CorrectionSigma!=0)&&string.IsNullOrWhiteSpace(request.CorrectionNotes))throw new CnfException("Ghi rõ căn cứ của hệ số hiệu chỉnh K.");
        if(request.MassKg.HasValue) {Guard.Positive(request.MassKg.Value,"Khối lượng kg");Guard.Nonnegative(request.MassSigmaKg,"Sai số khối lượng");}
        double energy=request.Gamma.EnergyKeV;var estimate=profile.Evaluate(energy);
        double pgRel=request.Gamma.ProbabilitySigma/request.Gamma.Probability;
        string key=request.Gamma.Key(request.Nuclide);
        var matching=key.Length==0?[]:profile.Definition.Points.Where(p=>p.Gamma.Key(p.Source.Nuclide)==key).ToArray();
        if(matching.Any(p=>(p.Gamma with{OptionalMetadata=request.Gamma.OptionalMetadata})!=request.Gamma))throw new CnfException("Dữ liệu xác suất gamma của mẫu khác dữ liệu cùng vạch trong bộ chuẩn.");
        double covarianceTerm=matching.Length==0?0:2*profile.SharedLogResponse(energy,key)*pgRel;
        double baseVariance=Math.Pow(roi.AreaError.Value/roi.PeakArea.Value,2)+Math.Pow(estimate.Sigma/estimate.Value,2)+pgRel*pgRel+covarianceTerm+Math.Pow(request.CorrectionSigma/request.Correction,2);
        double Factor(double half,DateTimeOffset at)=>-Math.Log(RadioactiveDecay.AverageFactor(half,request.RealSeconds))+Math.Log(2)*(request.MeasuredAt-at).TotalDays/half;
        double HalfSigma(DateTimeOffset at)
        {
            if(request.HalfLifeSigmaDays==0)return 0;
            double d=request.HalfLifeDays*1e-5;
            return (Factor(request.HalfLifeDays+d,at)-Factor(request.HalfLifeDays-d,at))/(2*d)*request.HalfLifeSigmaDays;
        }
        string halfKey=$"half-life:{request.Nuclide.Trim().ToUpperInvariant()}:{request.HalfLifeReference.Trim().ToUpperInvariant()}";
        var matchingHalf=string.IsNullOrWhiteSpace(request.HalfLifeReference)?[]:profile.Definition.Points.Where(p=>p.HalfLifeKey==halfKey).ToArray();
        if(matchingHalf.Any(p=>p.Source.HalfLifeDays!=request.HalfLifeDays||p.Source.HalfLifeSigmaDays!=request.HalfLifeSigmaDays))throw new CnfException("Bán rã/sai số của mẫu khác dữ liệu cùng tham chiếu trong bộ chuẩn.");
        double halfResponse=matchingHalf.Length==0?0:profile.SharedLogResponse(energy,halfKey);
        double raw=roi.PeakArea.Value*request.Correction/(request.LiveSeconds*estimate.Value*request.Gamma.Probability);
        double start=raw*Math.Exp(Factor(request.HalfLifeDays,request.MeasuredAt));
        double reported=raw*Math.Exp(Factor(request.HalfLifeDays,request.ReportDate));
        Guard.Positive(start,"Hoạt độ tại đầu phép đo");Guard.Positive(reported,"Hoạt độ tại ngày quy chiếu");
        double hs=HalfSigma(request.MeasuredAt),hr=HalfSigma(request.ReportDate);
        double vs=baseVariance+hs*hs-2*halfResponse*hs,vr=baseVariance+hr*hr-2*halfResponse*hr;
        if(!double.IsFinite(vs)||!double.IsFinite(vr)||vs< -1e-10||vr< -1e-10)throw new CnfException("Sai số hoạt độ không hợp lệ.");
        vs=Math.Max(0,vs);vr=Math.Max(0,vr);
        double? specific=request.MassKg is double mass?reported/mass:null;
        double? specificSigma=request.MassKg is double mass2?specific*Math.Sqrt(vr+Math.Pow(request.MassSigmaKg/mass2,2)):null;
        Guard.Nonnegative(start*Math.Sqrt(vs),"Sai số hoạt độ");Guard.Nonnegative(reported*Math.Sqrt(vr),"Sai số hoạt độ quy chiếu");
        if(specific.HasValue){Guard.Positive(specific.Value,"Bq/kg");Guard.Nonnegative(specificSigma!.Value,"Sai số Bq/kg");}
        return new(start,start*Math.Sqrt(vs),reported,reported*Math.Sqrt(vr),specific,specificSigma,estimate,
            RadioactiveDecay.AverageFactor(request.HalfLifeDays,request.RealSeconds),
            (profile.QualitySummary+" "+(roi.ReducedChiSquare>3?"CẢNH BÁO: χ²/ν ROI mẫu >3. ":"")+"Sai số 1σ tuyến tính: diện tích ROI, covariance đường chuẩn, xác suất gamma (có tương quan nếu dùng cùng vạch/tham chiếu), bán rã (có tương quan khi cùng tham chiếu), K và khối lượng. Không tự hiệu chỉnh tự hấp thụ/trùng phùng tổng. Giả định tỷ lệ live/real đều theo thời gian; sai số thời điểm/live time chưa được cung cấp. Chưa gộp các vạch thành hoạt độ chung. "+profile.InputSummary));
    }
}
public static class EfficiencyReports
{
    private static string F(double? x)=>x?.ToString("G12",CultureInfo.InvariantCulture)??"NA";
    private static string OptionalValue(EfficiencyObservation o,string missingLabel,double value)=>o.MissingInputs?.Contains(missingLabel)==true?"NA":F(value);
    private static string Clean(string? s)=>(s??"").Replace('\t',' ').Replace('\r',' ').Replace('\n',' ');
    private static void ProfileHeader(StreamWriter w,EfficiencyProfile p)
    {
        w.WriteLine("Gamma CNF Reader 0.9.1 — full-energy peak efficiency");
        w.WriteLine("Profile\t"+Clean(p.Definition.Name));w.WriteLine("Created\t"+p.Definition.CreatedAt.ToString("O"));
        var g=p.Definition.Geometry;
        w.WriteLine($"Detector\t{Clean(g.Detector)}\nGeometry\t{Clean(g.Configuration)}\nDistance_mm\t{(g.DistanceSpecified?F(g.DistanceMm):"NA")}\nConfiguration_notes\t{Clean(g.Notes)}");
        w.WriteLine("Quality_warnings\t"+Clean(p.QualitySummary));
        w.WriteLine("Input_completeness\t"+Clean(p.InputSummary));
        w.WriteLine("Model\tln(epsilon) = sum c_j [ln(E_keV/1000)]^j; generalized least squares, absolute efficiencies (fraction).");
        w.WriteLine("Coefficients\t"+string.Join("\t",p.Coefficients.Select(c=>F(c))));
        w.WriteLine($"Energy_range_keV\t{F(p.MinEnergy)}\t{F(p.MaxEnergy)}\nChi2_per_dof\t{F(p.ChiSquarePerDof)}");
        for(int i=0;i<p.Coefficients.Length;i++)w.WriteLine("Covariance_row_"+i+"\t"+string.Join("\t",Enumerable.Range(0,p.Coefficients.Length).Select(j=>F(p.Covariance[i,j]))));
        w.WriteLine("Uncertainty\t1-sigma linearized log-space GLS. Source activity correlated only for explicit source IDs (measurement-scoped for activity-at-measurement mode); auto-ID activity errors treated as independent. Half-life and gamma correlated only by known identity/reference; unidentified gamma errors independent. ROI and supplied per-point correction errors treated as independent. Timing/live time assumed exact. Reused-background or shared-correction covariance between observations is not modeled. No extrapolation.");
    }
    public static void Calibration(string path,EfficiencyProfile p)
    {
        using var w=new StreamWriter(path,false,new UTF8Encoding(true));ProfileHeader(w,p);
        w.WriteLine("ID\tSource_ID\tNuclide\tCertificate\tActivity_Bq\tActivity_sigma\tCertificate_U_Bq\tCoverage_k\tRef_date\tHalf_life_days\tHalf_life_sigma\tHalf_life_reference\tMeasurement_date\tLive_s\tReal_s\tEnergy_keV\tPg\tPg_sigma\tNuclear_reference\tArea\tArea_sigma\tEpsilon\tEpsilon_sigma\tFitted\tResidual_log_sigma\tSpectrum\tBackground\tBackground_scale\tROI_start\tROI_end\tMeasurement_ID\tStandard_peak_K\tStandard_K_sigma\tStandard_K_notes\tActivity_basis\tMissing_inputs");
        int id=0;foreach(var o in p.Definition.Points)
        {
            var fitted=p.Evaluate(o.Gamma.EnergyKeV);
            w.WriteLine(string.Join("\t",++id,Clean(o.Source.Id),Clean(o.Source.Nuclide),Clean(o.Source.Certificate),F(o.Source.ActivityBq),OptionalValue(o,"sai số hoạt độ nguồn chưa cung cấp",o.Source.ActivitySigmaBq),OptionalValue(o,"sai số hoạt độ nguồn chưa cung cấp",o.Source.ActivitySigmaBq*o.Source.CoverageFactor),F(o.Source.CoverageFactor),(o.Source.ActivityAtMeasurement?"NA":o.Source.ReferenceDate.ToString("O")),(o.Source.HalfLifeDays==0?"NA":F(o.Source.HalfLifeDays)),OptionalValue(o,"sai số bán rã chưa cung cấp",o.Source.HalfLifeSigmaDays),Clean(o.Source.HalfLifeReference),(o.MeasuredAt==DateTimeOffset.MinValue?"NA":o.MeasuredAt.ToString("O")),F(o.LiveSeconds),(o.RealSeconds==0?"NA":F(o.RealSeconds)),F(o.Gamma.EnergyKeV),F(o.Gamma.Probability),OptionalValue(o,"sai số xác suất gamma chưa cung cấp",o.Gamma.ProbabilitySigma),Clean(o.Gamma.Reference),F(o.Area),F(o.AreaSigma),F(o.Efficiency),F(o.Efficiency*o.TotalRelativeSigma),F(fitted.Value),F(p.ResidualSigma[id-1]),Clean(o.SpectrumPath),Clean(o.BackgroundPath),F(o.BackgroundScale),o.RoiStart,o.RoiEnd,Clean(o.MeasurementId),F(o.Correction),OptionalValue(o,"sai số K chưa cung cấp",o.CorrectionSigma),Clean(o.CorrectionNotes),o.Source.ActivityAtMeasurement?"Activity at measurement start":"Certificate decay correction",Clean(string.Join("; ",o.MissingInputs??[]))));
        }
    }
    public static void Activity(string path,EfficiencyProfile p,RoiResult roi,ActivityRequest request,ActivityResult result,string spectrum,string? background,double backgroundScale)
    {
        using var w=new StreamWriter(path,false,new UTF8Encoding(true));ProfileHeader(w,p);
        w.WriteLine("Activity_measurement\t"+Clean(spectrum));w.WriteLine("Measurement_ID\t"+Clean(request.MeasurementId));
        w.WriteLine("Background\t"+Clean(background));w.WriteLine("Background_scale\t"+F(backgroundScale));
        w.WriteLine("Nuclide\t"+Clean(request.Nuclide));w.WriteLine($"Gamma_keV\t{F(request.Gamma.EnergyKeV)}\nPg\t{F(request.Gamma.Probability)}\nPg_sigma\t{F(request.Gamma.ProbabilitySigma)}\nNuclear_reference\t{Clean(request.Gamma.Reference)}");
        w.WriteLine($"ROI_channels\t{roi.Range.Start}\t{roi.Range.End}\nPeak_area\t{F(roi.PeakArea)}\nArea_sigma\t{F(roi.AreaError)}");
        w.WriteLine($"Measured_at\t{request.MeasuredAt:O}\nReport_date\t{request.ReportDate:O}\nLive_s\t{F(request.LiveSeconds)}\nReal_s\t{F(request.RealSeconds)}\nHalf_life_days\t{F(request.HalfLifeDays)}\nHalf_life_sigma_days\t{F(request.HalfLifeSigmaDays)}\nHalf_life_reference\t{Clean(request.HalfLifeReference)}");
        w.WriteLine($"Epsilon\t{F(result.Efficiency.Value)}\nEpsilon_sigma\t{F(result.Efficiency.Sigma)}\nDecay_average\t{F(result.DecayAverageFactor)}\nCorrection_K\t{F(request.Correction)}\nCorrection_sigma\t{F(request.CorrectionSigma)}\nCorrection_notes\t{Clean(request.CorrectionNotes)}");
        w.WriteLine($"Activity_start_Bq\t{F(result.BqAtStart)}\nStart_sigma_Bq\t{F(result.SigmaBqAtStart)}\nActivity_report_Bq\t{F(result.BqAtReportDate)}\nReport_sigma_Bq\t{F(result.SigmaBqAtReportDate)}\nMass_kg\t{F(request.MassKg)}\nMass_sigma_kg\t{F(request.MassSigmaKg)}\nBq_per_kg\t{F(result.BqPerKg)}\nSigma_Bq_per_kg\t{F(result.SigmaBqPerKg)}");
        w.WriteLine("Limitations\t"+result.Notes);
    }
}
