using System.Buffers.Binary;
using System.Globalization;
using System.Text;

namespace GammaCnf.Core;

public sealed record Spectrum(uint[] Counts, string Title, double[]? Calibration,
    double? RealTime, double? LiveTime, DateTime? Started, IReadOnlyList<string> Warnings)
{
    public ulong Total => Counts.Aggregate(0UL, (sum, value) => sum + value);
    public double? Energy(double channel)
    {
        if (Calibration is null) return null;
        double energy = 0;
        for (int i = Calibration.Length - 1; i >= 0; i--) energy = energy * channel + Calibration[i];
        return energy;
    }
    public void ExportCsv(string path)
    {
        using var writer = new StreamWriter(path, false, new UTF8Encoding(true));
        writer.WriteLine("channel,energy_keV,counts");
        for (int i = 0; i < Counts.Length; i++)
            writer.WriteLine($"{i},{Energy(i)?.ToString("G17", CultureInfo.InvariantCulture) ?? ""},{Counts[i]}");
    }
}

public sealed class CnfException(string message) : Exception(message);

public static class CnfReader
{
    public static Spectrum Read(string path)
    {
        using var stream = File.OpenRead(path);
        if (stream.Length > 64 * 1024 * 1024) throw new CnfException("File vượt giới hạn 64 MB.");
        var data = new byte[checked((int)stream.Length)];
        stream.ReadExactly(data);
        return Decode(data);
    }

    public static Spectrum Decode(byte[] data)
    {
        void Bounds(int pos, int size)
        {
            if (pos < 0 || size < 0 || pos > data.Length - size)
                throw new CnfException("File bị cắt ngắn hoặc địa chỉ khối không hợp lệ.");
        }
        ushort U16(int p) { Bounds(p, 2); return BinaryPrimitives.ReadUInt16LittleEndian(data.AsSpan(p, 2)); }
        uint U32(int p) { Bounds(p, 4); return BinaryPrimitives.ReadUInt32LittleEndian(data.AsSpan(p, 4)); }
        ulong U64(int p) { Bounds(p, 8); return BinaryPrimitives.ReadUInt64LittleEndian(data.AsSpan(p, 8)); }
        int Address(uint p)
        {
            if (p > int.MaxValue) throw new CnfException("Địa chỉ khối ngoài phạm vi.");
            return (int)p;
        }
        double CamFloat(int p)
        {
            uint swapped = ((uint)U16(p) << 16) | U16(p + 2);
            return BitConverter.Int32BitsToSingle(unchecked((int)swapped)) / 4.0;
        }
        double Duration(int p)
        {
            Bounds(p, 8);
            if (data[p + 7] == 0x80)
                return Math.Abs((double)unchecked((int)U32(p))) * (data[p + 4] == 1 ? 1e6 : 1) * 31557600;
            return Math.Abs((double)unchecked((long)U64(p))) / 1e7;
        }
        if (data.Length < 0x70 + 28 * 0x30 || U16(0) != 0x400)
            throw new CnfException("Không phải file Canberra CNF được hỗ trợ.");
        var blocks = new Dictionary<uint, List<int>>();
        for (int i = 0; i < 28; i++)
        {
            int entry = 0x70 + i * 0x30;
            uint id = U32(entry);
            if (id == 0) continue;
            int p = Address(U32(entry + 10));
            Bounds(p, 0x30);
            if (U32(p) != id) throw new CnfException("Mã khối không khớp bảng chỉ mục CNF.");
            if (!blocks.TryGetValue(id, out var list)) blocks[id] = list = [];
            list.Add(p);
        }
        if (!blocks.TryGetValue(0x12005, out var specs) || specs.Count != 1)
            throw new CnfException("Cần đúng một khối phổ SPEC; biến thể này chưa được hỗ trợ.");
        int spec = specs[0], channels = U16(spec + 0x2a), header = U16(spec + 0x10);
        if (channels is < 1 or > 32768 || header < 0x30)
            throw new CnfException("Số kênh hoặc đầu khối phổ chưa được hỗ trợ.");
        int start = spec + header + U16(spec + 0x28);
        Bounds(start, channels * 4);
        var counts = new uint[channels];
        for (int i = 0; i < channels; i++) counts[i] = U32(start + 4 * i);
        string title = "";
        var warnings = new List<string>();
        double[]? calibration = null;
        double? real = null, live = null;
        DateTime? started = null;
        if (blocks.TryGetValue(0x12001, out var samples))
        {
            try
            {
                int p = samples[0] + U16(samples[0] + 0x10);
                Bounds(p, 64);
                // Western legacy CAM titles. Encoding bytes preserved as Latin-1.
                title = Encoding.Latin1.GetString(data, p, 64).Split('\0')[0].Trim();
            }
            catch (CnfException) { warnings.Add("Không đọc được tên mẫu."); }
        }
        if (blocks.TryGetValue(0x12000, out var acquisition))
        foreach (int p in acquisition)
        {
            int common = p + U16(p + 0x10);
            uint declared = U32(common + 0x89);
            if (declared != 0 && declared != channels)
                throw new CnfException("Số kênh ACQP và SPEC không khớp; không thể đọc an toàn.");
            try
            {
                int time = p + 0x30 + U16(p + 0x24);
                real = Duration(time + 9);
                live = Duration(time + 17);
                ulong ticks = U64(common + U16(p + 0x24) + 1);
                if (ticks != 0 && ticks <= long.MaxValue)
                    started = new DateTime(1858, 11, 17, 0, 0, 0, DateTimeKind.Unspecified).AddTicks((long)ticks);
            }
            catch (Exception e) when (e is CnfException or ArgumentOutOfRangeException)
            { warnings.Add("Thời gian thiếu hoặc không hợp lệ."); }
            try
            {
                int flag = U16(p + 4);
                int record = flag is 0x700 or 0x300 ? 0 : U16(p + 0x22);
                double[] c = Enumerable.Range(0, 4).Select(i => CamFloat(p + 0x30 + record + 0x44 + 4*i)).ToArray();
                if (c.Any(v => v != 0) && c.All(double.IsFinite))
                {
                    var candidate = new Spectrum(counts, title, c, real, live, started, []);
                    double previous = double.NegativeInfinity;
                    bool valid = true;
                    for (int i = 0; i < channels; i++)
                    {
                        double energy = candidate.Energy(i)!.Value;
                        if (!double.IsFinite(energy) || energy <= previous) { valid = false; break; }
                        previous = energy;
                    }
                    if (valid) calibration = c;
                    else warnings.Add("Hiệu chuẩn không tăng đơn điệu; hiển thị theo kênh.");
                }
            }
            catch (CnfException) { warnings.Add("Không đọc được hiệu chuẩn."); }
        }
        if (calibration is null) warnings.Add("Không có hiệu chuẩn năng lượng hợp lệ.");
        if (live > real) warnings.Add("Thời gian sống lớn hơn thời gian thực; kiểm tra file gốc.");
        return new Spectrum(counts, title, calibration, real, live, started, warnings);
    }
}
