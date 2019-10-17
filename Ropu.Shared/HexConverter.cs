namespace Ropu.Shared
{
    public class HexConverter
    {
        static readonly byte[] _hexLookup;

        static HexConverter()
        {
            _hexLookup = new byte[128];
            _hexLookup['0'] = 0x00;
            _hexLookup['1'] = 0x01;
            _hexLookup['2'] = 0x02;
            _hexLookup['3'] = 0x03;
            _hexLookup['4'] = 0x04;
            _hexLookup['5'] = 0x05;
            _hexLookup['6'] = 0x06;
            _hexLookup['7'] = 0x07;
            _hexLookup['8'] = 0x08;
            _hexLookup['9'] = 0x09;
            _hexLookup['a'] = 0x0a;
            _hexLookup['b'] = 0x0b;
            _hexLookup['c'] = 0x0c;
            _hexLookup['d'] = 0x0d;
            _hexLookup['e'] = 0x0e;
            _hexLookup['f'] = 0x0f;
            _hexLookup['A'] = 0x0a;
            _hexLookup['B'] = 0x0b;
            _hexLookup['C'] = 0x0c;
            _hexLookup['D'] = 0x0d;
            _hexLookup['E'] = 0x0e;
            _hexLookup['F'] = 0x0f;
        }

        public static byte[] FromHex(string hex)
        {
            var outputLength = hex.Length / 2;
            var output = new byte[outputLength];
            for (var i = 0; i < outputLength; i++)
            {
                int hexIndex = i*2;
                output[i] = (byte)
                    ((_hexLookup[hex[hexIndex]] << 4) +
                    (_hexLookup[hex[hexIndex + 1]]));
            }
            return output;
        }
    }
}