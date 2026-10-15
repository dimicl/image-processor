using Shared.MSI;

namespace Tests;

public class MsiFuzzTests
{
    [Fact]
    public void Decode_RandomCorruption_DoesNotCrash()
    {
        using var bmp = new System.Drawing.Bitmap(16, 16, System.Drawing.Imaging.PixelFormat.Format24bppRgb);
        byte[] encoded = MsiEncoder.Encode(bmp);
        var rng = new Random(42);
        int exceptions = 0;

        for (int i = 0; i < 50; i++)
        {
            byte[] corrupted = (byte[])encoded.Clone();
            int mutations = rng.Next(1, 8);
            for (int m = 0; m < mutations; m++)
                corrupted[rng.Next(corrupted.Length)] = (byte)rng.Next(256);

            try
            {
                MsiDecoder.Decode(corrupted);
            }
            catch (InvalidDataException)
            {
                exceptions++;
            }
            catch (NotSupportedException)
            {
                exceptions++;
            }
        }

        Assert.True(exceptions > 0, "Fuzz mutations should be rejected, not silently accepted");
    }
}
