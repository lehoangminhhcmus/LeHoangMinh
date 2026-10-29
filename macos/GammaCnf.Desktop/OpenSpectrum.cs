using System.ComponentModel;
using System.IO;
using GammaCnf.Core;
namespace GammaCnf.App;

public sealed class SpectrumViewState
{
    public string[] Coefficients {get;set;}=[];
    public bool Energy {get;set;}
    public bool Log {get;set;}
    public int TimeBasis {get;set;}
    public bool AllowChannelOnly {get;set;}
    public string RoiStart {get;set;}="0";
    public string RoiEnd {get;set;}="100";
    public (double Left,double Right)? Viewport {get;set;}
    public RoiRange? SelectedRoi {get;set;}
    public string PeakMinimumFwhm {get;set;}="3";
    public string PeakMaximumFwhm {get;set;}="40";
    public string PeakThreshold {get;set;}="5";
    public string PeakPadding {get;set;}="3.5";
    public bool ReplaceAutomatic {get;set;}=true;
}
public sealed class OpenSpectrum : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;
    private string path;
    private bool dirty;
    public string Path {get=>path;set{path=value;PropertyChanged?.Invoke(this,new(nameof(Label)));}}
    public string Label=>System.IO.Path.GetFileName(Path)+(CalibrationDirty?" *":"")+" — "+System.IO.Path.GetDirectoryName(Path);
    public bool CalibrationDirty {get=>dirty;set{dirty=value;PropertyChanged?.Invoke(this,new(nameof(Label)));}}
    public AnalysisSession Session {get;}
    public byte[] OriginalBytes {get;set;}
    public Spectrum? PendingBackground {get;set;}
    public string? PendingBackgroundPath {get;set;}
    public string? BackgroundPath {get;set;}
    public string ScaleMode {get;set;}="None";
    public List<RoiRange> Ranges {get;}=[];
    public Dictionary<RoiRange,DetectedRoi> AutomaticRois {get;}=[];
    public Dictionary<RoiRange,long> AutomaticSearchRevisions {get;}=[];
    public SpectrumViewState View {get;}=new();
    public OpenSpectrum(string path,byte[] bytes,Spectrum spectrum)
    {this.path=System.IO.Path.GetFullPath(path);OriginalBytes=bytes;Session=new(spectrum);View.Energy=spectrum.Calibration is not null;}
}
