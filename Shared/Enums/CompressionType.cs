using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shared.Enums
{
    public enum CompressionType
    {
        None = 0,
        ShannonFano = 1,
        Huffman = 2,
        MPEG420_V1 = 3,
        MPEG420_V2 = 4
    }
}
