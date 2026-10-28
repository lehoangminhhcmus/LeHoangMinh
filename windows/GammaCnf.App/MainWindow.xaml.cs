using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Microsoft.Win32;
using GammaCnf.Core;

namespace GammaCnf.App;
public partial class MainWindow : Window
{
    private Spectrum? spectrum;
    private string? currentPath;
    public MainWindow()
    {
        InitializeComponent();
        Plot.HoverChanged += text => Status.Text = text;
        Loaded += (_, _) =>
        {
            string[] args = Environment.GetCommandLineArgs();
            if (args.Length > 1) LoadFile(args[1]);
        };
    }
    private void OpenClick(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog { Filter = "Canberra CNF (*.cnf)|*.cnf|Tất cả file (*.*)|*.*" };
        if (dialog.ShowDialog(this) == true) LoadFile(dialog.FileName);
    }
    private void LoadFile(string path)
    {
        try
        {
            Spectrum loaded = CnfReader.Read(path);
            spectrum = loaded; currentPath = path;
            FileLabel.Text = $"{Path.GetFileName(path)}   |   {loaded.Title}";
            Title = $"{Path.GetFileName(path)} — Gamma CNF Reader";
            string Seconds(double? t) => t?.ToString("N3", CultureInfo.CurrentCulture) + (t.HasValue ? " s" : "Không có");
            string started = loaded.Started?.ToString("dd/MM/yyyy HH:mm:ss") ?? "Không có";
            Metadata.Text = $"{loaded.Counts.Length:N0} kênh   •   {loaded.Total:N0} số đếm   •   Bắt đầu (theo file): {started}\nThời gian thực: {Seconds(loaded.RealTime)}   •   Thời gian sống: {Seconds(loaded.LiveTime)}";
            if (loaded.RealTime > 0 && loaded.LiveTime.HasValue && loaded.LiveTime <= loaded.RealTime)
                Metadata.Text += $"   •   Thời gian chết: {(1 - loaded.LiveTime.Value/loaded.RealTime.Value)*100:F2}%";
            CalibrationLabel.Text = loaded.Calibration is null ? "Không có hiệu chuẩn: chỉ hiển thị kênh." :
                "Hiệu chuẩn E(kênh), keV: " + string.Join("   ", loaded.Calibration.Select((c, i) => $"a{i} = {c:G9}"));
            Plot.Spectrum = loaded;
            EnergyCheck.IsEnabled = loaded.Calibration is not null;
            EnergyCheck.IsChecked = loaded.Calibration is not null;
            CsvButton.IsEnabled = PngButton.IsEnabled = true;
            Plot.EnergyAxis = EnergyCheck.IsChecked == true;
            Plot.LogScale = LogCheck.IsChecked == true;
            Plot.Reset();
            Warnings.Text = string.Join(" • ", loaded.Warnings);
            Status.Text = "Đã đọc phổ • Lăn chuột để zoom, kéo chuột chọn vùng, Home để xem toàn phổ.";
        }
        catch (Exception e) when (e is CnfException or IOException or UnauthorizedAccessException or ArgumentException or NotSupportedException)
        { MessageBox.Show(this, e.Message, "Không mở được file CNF", MessageBoxButton.OK, MessageBoxImage.Warning); }
    }
    private void CsvClick(object sender, RoutedEventArgs e)
    {
        if (spectrum is null) return;
        var dialog = new SaveFileDialog { Filter = "CSV (*.csv)|*.csv", FileName = Path.GetFileNameWithoutExtension(currentPath) + ".csv" };
        if (dialog.ShowDialog(this) != true) return;
        Save(() => spectrum.ExportCsv(dialog.FileName), dialog.FileName);
    }
    private void PngClick(object sender, RoutedEventArgs e)
    {
        var dialog = new SaveFileDialog { Filter = "PNG (*.png)|*.png", FileName = Path.GetFileNameWithoutExtension(currentPath) + ".png" };
        if (dialog.ShowDialog(this) != true) return;
        Save(() =>
        {
            var bitmap = new RenderTargetBitmap(Math.Max(1, (int)Plot.ActualWidth * 2), Math.Max(1, (int)Plot.ActualHeight * 2), 192, 192, PixelFormats.Pbgra32);
            bitmap.Render(Plot);
            var encoder = new PngBitmapEncoder(); encoder.Frames.Add(BitmapFrame.Create(bitmap));
            using var output = File.Create(dialog.FileName); encoder.Save(output);
        }, dialog.FileName);
    }
    private void Save(Action action, string path)
    {
        try { action(); Status.Text = "Đã lưu: " + path; }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        { MessageBox.Show(this, e.Message, "Không lưu được file", MessageBoxButton.OK, MessageBoxImage.Warning); }
    }
    private void ModeChanged(object sender, RoutedEventArgs e)
    {
        if (Plot is null) return;
        Plot.EnergyAxis = EnergyCheck.IsChecked == true;
        Plot.LogScale = LogCheck.IsChecked == true;
        Plot.Reset();
    }
    private void ResetClick(object sender, RoutedEventArgs e) => Plot.Reset();
    private void OnDragOver(object sender, DragEventArgs e)
    { e.Effects = e.Data.GetDataPresent(DataFormats.FileDrop) ? DragDropEffects.Copy : DragDropEffects.None; e.Handled = true; }
    private void OnDrop(object sender, DragEventArgs e)
    { if (e.Data.GetData(DataFormats.FileDrop) is string[] { Length: > 0 } paths) LoadFile(paths[0]); }
    private void OnKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.O && Keyboard.Modifiers.HasFlag(ModifierKeys.Control)) { OpenClick(sender, e); e.Handled = true; }
        if (e.Key == Key.Home) { Plot.Reset(); e.Handled = true; }
    }
}
