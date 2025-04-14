using K4os.Compression.LZ4;
using Serilog;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HashbrownPackages
{
    public class HashPackage
    {
        public static Dictionary<ulong, string> Hashes = new();

        public static string GetHash<TEnum>(ulong hash, TEnum assetType)
        {
            if (Hashes.TryGetValue(hash, out string name))
            {
                return name;
            }
            return $"{assetType.ToString().ToLower()}_{hash:X}";
        }

        public static string GetHash(ulong hash)
        {
            if (Hashes.TryGetValue(hash, out string name))
            {
                return name;
            }
            return $"{hash:X}";
        }

        public static unsafe void Initialize()
        {
            string path = Path.Combine(Directory.GetCurrentDirectory(), "hash_package");
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
                return;
            }
            Stopwatch sw = new Stopwatch();
            sw.Start();
            string[] files = Directory.GetFiles(path, "*.wni");
            foreach (string file in files)
            {
                BinaryReader reader = new BinaryReader(new FileStream(file, FileMode.Open));
                uint magic = reader.ReadUInt32();
                if (magic != 0x20494E57)
                {
                    reader.Close();
                    return;
                }

                uint version = reader.ReadUInt16();
                if (version != 1)
                {
                    Log.Information("Hash package {file} has unsupported version {version}", file, version);
                    reader.Close();
                    return;
                }

                uint count = reader.ReadUInt32();
                int compressedSize = reader.ReadInt32();
                int decompressedSize = reader.ReadInt32();

                byte[] compressed = reader.ReadBytes((int)compressedSize);
                byte[] decompressed = new byte[decompressedSize];

                reader.Close();

                fixed (byte* compressedPtr = compressed)
                {

                    fixed (byte* decompressedPtr = decompressed)
                    {
                        int decoded = LZ4Codec.Decode(compressedPtr, compressedSize, decompressedPtr, decompressedSize);
                        if (decoded != decompressedSize)
                        {
                            Log.Error("Can't decompress {file}. Expected {expected}, got {got}", file, decompressedSize, decoded);
                            return;
                        }
                        BinaryReader decompressedReader = new BinaryReader(new MemoryStream(decompressed));
                        for (int j = 0; j < count; j++)
                        {
                            ulong hash = decompressedReader.ReadUInt64() & 0xFFFFFFFFFFFFFFF;
                            //Read null terminated string
                            StringBuilder sb = new StringBuilder();
                            char c;
                            while ((c = decompressedReader.ReadChar()) != '\0')
                            {
                                sb.Append(c);
                            }
                            Hashes[hash] = sb.ToString();
                        }
                    }
                }
            }
            sw.Stop();
            Log.Information("Hash package loaded in {time}ms", sw.ElapsedMilliseconds);
        }
    }
}
