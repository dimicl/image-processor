using System.Text;

namespace Shared.MSI;

public static class HuffmanCodec
{
    private class Node
    {
        public byte? Symbol;
        public int Freq;
        public Node? Left, Right;
    }

    public static (byte[] compressed, byte[] meta) Compress(byte[] data)
    {
        if (data.Length == 0)
            return (Array.Empty<byte>(), Encoding.UTF8.GetBytes("HUF0:0"));

        var freq = new int[256];
        foreach (byte b in data) freq[b]++;

        var nodes = new List<Node>();
        for (int i = 0; i < 256; i++)
        {
            if (freq[i] > 0)
                nodes.Add(new Node { Symbol = (byte)i, Freq = freq[i] });
        }

        while (nodes.Count > 1)
        {
            nodes.Sort((a, b) => a.Freq.CompareTo(b.Freq));
            var left = nodes[0];
            var right = nodes[1];
            nodes.RemoveRange(0, 2);
            nodes.Add(new Node { Freq = left.Freq + right.Freq, Left = left, Right = right });
        }

        var codes = new Dictionary<byte, string>();
        BuildCodes(nodes[0], "", codes);

        var bits = new StringBuilder(data.Length * 8);
        foreach (byte b in data)
            bits.Append(codes[b]);

        int pad = (8 - bits.Length % 8) % 8;
        for (int i = 0; i < pad; i++) bits.Append('0');

        var compressed = new byte[bits.Length / 8];
        for (int i = 0; i < compressed.Length; i++)
        {
            byte val = 0;
            for (int j = 0; j < 8; j++)
                if (bits[i * 8 + j] == '1') val |= (byte)(1 << (7 - j));
            compressed[i] = val;
        }

        var metaSb = new StringBuilder("HUF0:");
        metaSb.Append(pad).Append(':');
        foreach (var kv in codes)
            metaSb.Append(kv.Key).Append('=').Append(kv.Value).Append(';');

        return (compressed, Encoding.UTF8.GetBytes(metaSb.ToString()));
    }

    public static byte[] Decompress(byte[] compressed, byte[] meta, int originalLength)
    {
        if (originalLength == 0) return Array.Empty<byte>();

        string metaStr = Encoding.UTF8.GetString(meta);
        if (!metaStr.StartsWith("HUF0:"))
            throw new InvalidDataException("Invalid Huffman metadata");

        var parts = metaStr[5..].Split(':', 2);
        int pad = int.Parse(parts[0]);
        var codeMap = new Dictionary<string, byte>();
        foreach (var entry in parts[1].Split(';', StringSplitOptions.RemoveEmptyEntries))
        {
            var eq = entry.IndexOf('=');
            byte sym = byte.Parse(entry[..eq]);
            string code = entry[(eq + 1)..];
            codeMap[code] = sym;
        }

        var bits = new StringBuilder(compressed.Length * 8);
        foreach (byte b in compressed)
        {
            for (int j = 7; j >= 0; j--)
                bits.Append((b & (1 << j)) != 0 ? '1' : '0');
        }
        if (pad > 0 && bits.Length >= pad)
            bits.Length -= pad;

        var result = new List<byte>(originalLength);
        var current = "";
        for (int i = 0; i < bits.Length && result.Count < originalLength; i++)
        {
            current += bits[i];
            if (codeMap.TryGetValue(current, out byte sym))
            {
                result.Add(sym);
                current = "";
            }
        }

        return result.ToArray();
    }

    private static void BuildCodes(Node node, string code, Dictionary<byte, string> codes)
    {
        if (node.Symbol.HasValue)
        {
            codes[node.Symbol.Value] = code.Length == 0 ? "0" : code;
            return;
        }
        BuildCodes(node.Left!, code + "0", codes);
        BuildCodes(node.Right!, code + "1", codes);
    }
}
