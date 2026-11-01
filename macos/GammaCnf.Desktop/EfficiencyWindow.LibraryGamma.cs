using Avalonia.Interactivity;
using Avalonia.Controls;
using GammaCnf.Core;
namespace GammaCnf.App;
public partial class EfficiencyWindow
{
    private readonly IReadOnlyList<ConfirmedGammaLine> confirmedLines=[];
    private bool probabilityReady;
    private string[]? manualGamma,manualSampleGamma;
    private TextBox[] ProbabilityFields(bool sample)=>sample
        ?[SampleNuclide,SampleEnergy,SampleProbability,SampleNuclearReference,SampleProbabilitySigma]
        :[SourceNuclide,GammaEnergy,GammaProbability,NuclearReference,GammaSigma];
    private void InitializeLibraryProbability()
    {
        probabilityReady=true;
        RoiChoice.SelectionChanged+=(_,_)=>RefreshLibraryProbability();
        RefreshLibraryProbability();
    }
    private void LibraryProbabilityModeChanged(object? sender,RoutedEventArgs e)
    {
        if(!probabilityReady)return;
        RefreshLibraryProbability();
    }
    private void RefreshLibraryProbability()
    {
        if(!probabilityReady)return;
        foreach(bool sample in new[]{false,true})
        {
            bool enabled=(sample?UseLibrarySampleGamma:UseLibraryGamma).IsChecked==true;
            var fields=ProbabilityFields(sample);var saved=sample?manualSampleGamma:manualGamma;
            if(enabled&&saved is null)
            {
                saved=fields.Select(f=>f.Text??"").ToArray();
                if(sample)manualSampleGamma=saved;else manualGamma=saved;
                fields[4].Text=sample?"0":"";
            }
            if(!enabled&&saved is not null)
            {
                for(int i=0;i<fields.Length;i++)fields[i].Text=saved[i];
                if(sample)manualSampleGamma=null;else manualGamma=null;
            }
            for(int i=0;i<4;i++)fields[i].IsReadOnly=enabled;
            var summary=sample?LibrarySampleGammaSummary:LibraryGammaSummary;
            if(!enabled){summary.Text="Nhập tay: dùng Pγ và sai số theo nguồn dữ liệu của người dùng.";continue;}
            try{ApplyLibraryProbability(sample);}
            catch(CnfException error)
            {
                for(int i=0;i<4;i++)fields[i].Text="";
                summary.Text=error.Message;
            }
        }
    }
    private void ApplyLibraryProbability(bool sample)
    {
        if(!probabilityReady||(sample?UseLibrarySampleGamma:UseLibraryGamma).IsChecked!=true)return;
        var roi=SelectedRoi().Result;
        var confirmed=confirmedLines.SingleOrDefault(c=>c.IsCurrent(session,roi))
            ??throw new CnfException("Chưa xác nhận vạch cho ROI này hoặc phép phân tích đã thay đổi. Xác nhận trong tab Gợi ý đồng vị, hoặc bỏ chọn để nhập tay.");
        var emission=confirmed.Emission(session,roi);var fields=ProbabilityFields(sample);
        fields[0].Text=confirmed.Line.Nuclide;fields[1].Text=F(emission.EnergyKeV);fields[2].Text=F(100*emission.Probability);
        fields[3].Text=emission.Reference;
        if(EfficiencyInput.Number(fields[4].Text,"sai số Pγ",0)>0)
            fields[3].Text=emission.Reference.Replace("thư viện chưa cung cấp sai số Pγ","sai số Pγ do người dùng nhập");
        (sample?LibrarySampleGammaSummary:LibraryGammaSummary).Text=$"{confirmed.Line.Nuclide} • {emission.EnergyKeV:G9} keV • Pγ={100*emission.Probability:G9}% • {confirmed.Line.FileName}. Thư viện không cung cấp sai số Pγ; có thể tự nhập sai số. "+confirmed.Line.Reference;
    }
    internal void RunLibraryGammaSmoke(string directory)
    {
        automatedSmoke=true;Show();
        var roi=SelectedRoi().Result;
        GammaProbability.Text="25";GammaEnergy.Text="";SourceNuclide.Text="MANUAL";NuclearReference.Text="MANUAL SOURCE";
        UseLibraryGamma.IsChecked=true;
        ApplyLibraryProbability(false);
        if(Num(GammaProbability)!=50||!GammaProbability.IsReadOnly||SourceNuclide.Text!="SYNTHETIC-ONLY")
            throw new Exception("Confirmed library gamma did not populate efficiency input.");
        ActivityText.Text="10000";MeasuredAt.Text="";RealText.Text="";
        AddPoint();
        var point=workspace.Points.Single();
        double expected=roi.PeakArea!.Value/(10000*session.Sample.LiveTime!.Value*.5);
        if(Math.Abs(point.Efficiency/expected-1)>1e-9||!point.MissingInputs!.Contains("sai số xác suất gamma chưa cung cấp"))
            throw new Exception("Library gamma efficiency or missing uncertainty metadata failed.");
        var file=new EfficiencyFile(2,"Library Pgamma test",point.Geometry,DateTimeOffset.UtcNow,1,[point]);
        string json=System.IO.Path.Combine(directory,"library-probability.json");EfficiencyProfile.SaveDefinition(json,file);
        var loaded=EfficiencyProfile.ReadDefinition(json).Points.Single();
        if(loaded.Gamma.Probability!=.5||!loaded.Gamma.Reference.Contains("synthetic.csv"))throw new Exception("Library gamma provenance JSON roundtrip failed.");
        UseLibraryGamma.IsChecked=false;
        if(GammaProbability.IsReadOnly||Num(GammaProbability)!=25||SourceNuclide.Text!="MANUAL"||NuclearReference.Text!="MANUAL SOURCE")
            throw new Exception("Switching back to manual gamma did not restore user values.");
        var original=RoiChoice.ItemsSource;
        RoiChoice.ItemsSource=new[]{new EfficiencyRoi(1,roi),new EfficiencyRoi(2,roi with{Range=new(roi.Range.Start+1,roi.Range.End)})};
        RoiChoice.SelectedIndex=0;UseLibraryGamma.IsChecked=true;RoiChoice.SelectedIndex=1;
        bool rejected=false;try{AddPoint();}catch(CnfException){rejected=true;}
        if(!rejected||!string.IsNullOrEmpty(GammaProbability.Text))throw new Exception("Unconfirmed ROI retained previous library probability.");
        UseLibraryGamma.IsChecked=false;RoiChoice.ItemsSource=original;RoiChoice.SelectedIndex=0;
        System.IO.File.WriteAllText(System.IO.Path.Combine(directory,"library-probability-success.txt"),"PASS: explicit ROI confirmation, library/manual modes, efficiency calculation, unknown sigma, provenance JSON, unconfirmed ROI clears and blocks stale probability");
    }
}
