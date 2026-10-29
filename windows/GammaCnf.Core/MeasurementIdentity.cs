using System.Buffers.Binary;
using System.Security.Cryptography;
namespace GammaCnf.Core;
public static class MeasurementIdentity
{
    // Calibration, file name and title are deliberately excluded: editing a CNF's
    // energy curve must not turn the same measurement into an independent sample.
    public static string Create(Spectrum spectrum)
    {
        var bytes=new byte[24+spectrum.Counts.Length*4];
        BinaryPrimitives.WriteInt64LittleEndian(bytes.AsSpan(0,8),spectrum.Started?.Ticks??-1);
        BinaryPrimitives.WriteInt64LittleEndian(bytes.AsSpan(8,8),BitConverter.DoubleToInt64Bits(spectrum.LiveTime??-1));
        BinaryPrimitives.WriteInt64LittleEndian(bytes.AsSpan(16,8),BitConverter.DoubleToInt64Bits(spectrum.RealTime??-1));
        for(int i=0;i<spectrum.Counts.Length;i++)BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(24+4*i,4),spectrum.Counts[i]);
        return Convert.ToHexString(SHA256.HashData(bytes));
    }
}
