using System.Globalization;
using Microsoft.VisualBasic.FileIO;

namespace GammaCnf.Core;

public sealed record NuclideLine(string Nuclide,double EnergyKeV,double? IntensityPercent,string Reference,string FileName);
public sealed record NuclideSuggestion(int Roi,string Nuclide,double MeasuredKeV,double LibraryKeV,double DeltaKeV,double ToleranceKeV,double? IntensityPercent,int SupportingRois,string Reference,string FileName)
{
    public string MeasuredText=>MeasuredKeV.ToString("0.###");
    public string LibraryText=>LibraryKeV.ToString("0.###");
    public string DeltaText=>DeltaKeV.ToString("+0.###;-0.###;0");
    public string ToleranceText=>ToleranceKeV.ToString("0.###");
    public string IntensityText=>IntensityPercent?.ToString("0.###")??"—";
    public string Support=>SupportingRois==0?"Chỉ khớp một đỉnh":$"{SupportingRois} ROI khác khớp vạch cùng đồng vị";
}
public sealed class NuclideLibrary
{
    public IReadOnlyList<NuclideLine> Lines {get;}
    public string DirectoryPath {get;}
    public string[] Warnings {get;}
    public const string CsvHeader="Nuclide,EnergyKeV,IntensityPercent,Reference";
    public const string Template=CsvHeader+"\n";
    private NuclideLibrary(string directory,List<NuclideLine> lines,string[] warnings){DirectoryPath=directory;Lines=lines;Warnings=warnings;}
    public static NuclideLibrary LoadDirectory(string directory)
    {
        if(!Directory.Exists(directory))throw new CnfException("Thư mục thư viện chưa tồn tại. Chọn thư mục chứa CSV dữ liệu đồng vị.");
        var files=Directory.EnumerateFiles(directory).Where(p=>string.Equals(Path.GetExtension(p),".csv",StringComparison.OrdinalIgnoreCase)||string.Equals(Path.GetExtension(p),".nlb",StringComparison.OrdinalIgnoreCase)).OrderBy(p=>p,StringComparer.Ordinal).ToArray();
        if(files.Length==0||files.Length>100)throw new CnfException("Thư mục cần từ 1 đến 100 file CSV/NLB thư viện đồng vị.");
        var lines=new List<NuclideLine>();long bytes=0;
        foreach(string path in files)
        {
            bytes+=new FileInfo(path).Length;if(bytes>32*1024*1024)throw new CnfException("Tổng CSV thư viện vượt 32 MB.");
            if(string.Equals(Path.GetExtension(path),".nlb",StringComparison.OrdinalIgnoreCase)){lines.AddRange(NlbLibraryReader.Read(path));if(lines.Count>200000)throw new CnfException("Thư viện vượt 200.000 vạch.");continue;}
            using var parser=new TextFieldParser(path,System.Text.Encoding.UTF8,true){TextFieldType=FieldType.Delimited,HasFieldsEnclosedInQuotes=true,TrimWhiteSpace=true};parser.SetDelimiters(",");
            try
            {
                string[] headers=parser.ReadFields()??[];
                int Column(string name)=>Array.FindIndex(headers,h=>string.Equals(h.Trim(),name,StringComparison.OrdinalIgnoreCase));
                int nuc=Column("Nuclide"),energy=Column("EnergyKeV"),intensity=Column("IntensityPercent"),reference=Column("Reference");
                if(nuc<0||energy<0||intensity<0||reference<0||headers.Distinct(StringComparer.OrdinalIgnoreCase).Count()!=headers.Length)throw new CnfException($"{Path.GetFileName(path)}: cần các cột {CsvHeader} (CSV phân cách bằng dấu phẩy).");
                while(!parser.EndOfData)
                {
                    long row=parser.LineNumber;var fields=parser.ReadFields()??[];
                    if(fields.Length!=headers.Length)throw new CnfException($"{Path.GetFileName(path)}, dòng {row}: số cột không khớp.");
                    double Number(string value,string label)
                    {
                        if(!double.TryParse(value,NumberStyles.Float,CultureInfo.InvariantCulture,out double result)||!double.IsFinite(result))throw new CnfException($"{Path.GetFileName(path)}, dòng {row}: {label} không hợp lệ; dùng dấu chấm thập phân.");return result;
                    }
                    string name=fields[nuc].Trim(),source=fields[reference].Trim();double e=Number(fields[energy],"EnergyKeV");
                    double? probability=fields[intensity].Length==0?null:Number(fields[intensity],"IntensityPercent");
                    if(name.Length is <1 or >80||source.Length>2000||e<=0||e>100000||probability is <0 or >100)throw new CnfException($"{Path.GetFileName(path)}, dòng {row}: đồng vị/năng lượng/xác suất không hợp lệ. IntensityPercent cần 0–100 hoặc để trống; không dùng yield >100 như xác suất gamma.");
                    lines.Add(new(name,e,probability,source,Path.GetFileName(path)));
                    if(lines.Count>200000)throw new CnfException("Thư viện vượt 200.000 vạch.");
                }
            }
            catch(MalformedLineException ex){throw new CnfException($"CSV lỗi: {Path.GetFileName(path)}, dòng {parser.ErrorLineNumber}: {ex.Message}");}
        }
        if(lines.Count==0)throw new CnfException("CSV chỉ có tiêu đề, chưa có dữ liệu đồng vị. Điền dữ liệu có nguồn tham chiếu rồi nạp lại.");
        var warnings=new List<string>();
        if(files.Any(p=>string.Equals(Path.GetExtension(p),".nlb",StringComparison.OrdinalIgnoreCase)))warnings.Add("NLB: chưa xác minh nguồn dữ liệu hạt nhân/ngày cập nhật. Yield >100 được giữ năng lượng nhưng không dùng làm xác suất phát.");
        if(lines.Any(l=>string.IsNullOrWhiteSpace(l.Reference)))warnings.Add("Có vạch chưa ghi nguồn tham chiếu.");
        if(lines.Any(l=>l.IntensityPercent is null))warnings.Add("Có vạch chưa cung cấp xác suất phát.");
        int count=lines.Count;lines=lines.DistinctBy(l=>(l.Nuclide.ToUpperInvariant(),l.EnergyKeV,l.IntensityPercent,l.Reference)).OrderBy(l=>l.EnergyKeV).ToList();
        if(lines.Count<count)warnings.Add($"Đã gộp {count-lines.Count} vạch trùng dữ liệu.");
        return new(Path.GetFullPath(directory),lines,warnings.ToArray());
    }
    public IReadOnlyList<NuclideSuggestion> Suggest(IReadOnlyList<RoiResult> rois,double minimumKeV=2,double fwhmFactor=.5)
    {
        if(!double.IsFinite(minimumKeV)||minimumKeV<=0||minimumKeV>100||!double.IsFinite(fwhmFactor)||fwhmFactor<0||fwhmFactor>2)throw new CnfException("Dung sai cần trong (0,100] keV; hệ số FWHM từ 0 đến 2.");
        var hits=new List<(int Index,NuclideLine Line,double Energy,double Tolerance)>();
        for(int i=0;i<rois.Count;i++)
        {
            var roi=rois[i];if(!roi.Success||roi.EnergyKeV is not double e||!double.IsFinite(e)||e<=0)continue;
            double tolerance=Math.Max(minimumKeV,roi.FwhmKeV is double f&&double.IsFinite(f)&&f>0?fwhmFactor*f:0);
            // Binary search avoids scanning all library lines for each ROI.
            int lo=0,hi=Lines.Count;
            while(lo<hi){int mid=(lo+hi)/2;if(Lines[mid].EnergyKeV<e-tolerance)lo=mid+1;else hi=mid;}
            for(int j=lo;j<Lines.Count&&Lines[j].EnergyKeV<=e+tolerance;j++)
            {
                if(Lines[j].IntensityPercent==0)continue;
                hits.Add((i,Lines[j],e,tolerance));
                if(hits.Count>50000)throw new CnfException("Quá 50.000 kết quả khớp; giảm dung sai hoặc dùng thư viện chọn lọc hơn.");
            }
        }
        var supports=hits.GroupBy(h=>h.Line.Nuclide,StringComparer.OrdinalIgnoreCase).ToDictionary(g=>g.Key,g=>g.GroupBy(h=>h.Index).Select(r=>(Index:r.Key,Energies:r.Select(h=>h.Line.EnergyKeV).ToHashSet())).ToArray(),StringComparer.OrdinalIgnoreCase);
        int Supporting((int Index,NuclideLine Line,double Energy,double Tolerance) h)=>supports[h.Line.Nuclide].Count(other=>other.Index!=h.Index&&(rois[other.Index].Range.End<rois[h.Index].Range.Start||rois[h.Index].Range.End<rois[other.Index].Range.Start)&&(other.Energies.Count>1||!other.Energies.Contains(h.Line.EnergyKeV)));
        return hits.Select(h=>new NuclideSuggestion(h.Index+1,h.Line.Nuclide,h.Energy,h.Line.EnergyKeV,h.Energy-h.Line.EnergyKeV,h.Tolerance,h.Line.IntensityPercent,Supporting(h),h.Line.Reference,h.Line.FileName))
            .OrderBy(h=>h.Roi).ThenByDescending(h=>h.SupportingRois).ThenBy(h=>Math.Abs(h.DeltaKeV)).ThenByDescending(h=>h.IntensityPercent).ThenBy(h=>h.Nuclide,StringComparer.Ordinal).ToArray();
    }
}
