using HashbrownPackages.Games;
using Serilog;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using Windows.Win32;
using Windows.Win32.Foundation;

namespace HashbrownPackages
{
    public struct XAsset64
    {
        public nint Header;
        public nint Temp;
        public nint Next;
        public nint Previous;
    };

    public struct XAssetPool64
    {
        public nint Root;
        public nint End;
        public nint LookupTable;
        public nint HeaderMemory;
        public nint AssetMemory;
    };

    public unsafe class Cordycep
    {
        public static SafeHandle ProcessHandle;
        public static string WorkingEnvironment;

        public static ulong GameID;
        public static nint PoolsAddress;
        public static nint StringsAddress;
        public static string GameDirectory;

        public static string[] Flags;

        static void Main(string[] args)
        {
            Log.Logger = new LoggerConfiguration()
                .WriteTo.Console()
                .CreateLogger();

            var processes = Process.GetProcessesByName("Cordycep.CLI");
            if (processes.Length == 0)
            {
                Log.Error("Cordycep.CLI is not running. Please start the CLI first.");
                Console.ReadKey();
                return;
            }

            if(processes.Length > 1)
            {
                Log.Error("Multiple instances of Cordycep.CLI are running. Close the one that you don't need");
                Console.ReadKey();
                return;
            }

            Process process = processes.First();

            ProcessHandle = process.SafeHandle;
            WorkingEnvironment = System.IO.Path.GetDirectoryName(process.MainModule.FileName);

            string statePath = System.IO.Path.Combine(WorkingEnvironment, "Data\\CurrentHandler.csi");
            if (!System.IO.File.Exists(statePath))
            {
                Log.Error("Can't find CurrentHandler.csi. Uh.. Have Cordycep loaded any game yet..?");
                return;
            }

            BinaryReader reader = new BinaryReader(new MemoryStream(File.ReadAllBytes(statePath)));
            GameID = reader.ReadUInt64();
            PoolsAddress = (nint)reader.ReadUInt64();
            StringsAddress = (nint)reader.ReadUInt64();
            int gameDirectoryLength = reader.ReadInt32();
            GameDirectory = new string(reader.ReadChars(gameDirectoryLength));

            uint flagsCount = reader.ReadUInt32();
            Flags = new string[flagsCount];
            for (int i = 0; i < flagsCount; i++)
            {
                int flagLength = reader.ReadInt32();
                Flags[i] = new string(reader.ReadChars(flagLength));
            }
            reader.Close();

            string gameId = Encoding.UTF8.GetString(BitConverter.GetBytes(Cordycep.GameID));
            Log.Information("{name} is running @ {environment}", "Cordycep.CLI", Cordycep.WorkingEnvironment);
            Log.Information("GameID: {game}", gameId);
            Log.Information("Pools Address: {address:X}", Cordycep.PoolsAddress);
            Log.Information("Strings Address: {address:X}", Cordycep.StringsAddress);
            Log.Information("Game Directory: {directory}", Cordycep.GameDirectory);
            Log.Information("Flag: {flag}", string.Join(", ", Cordycep.Flags));

            HashPackage.Initialize();

            switch (gameId)
            {
                case "YAMYAMOK":
                    break;
                case "BLACKOP6":
                    BlackOps6 blackOps6 = new BlackOps6();
                    blackOps6.Process();
                    break;
                default:
                    Log.Error("Game is not supported. :(");
                    return;
            }
        }

        public static unsafe void EnumerableAssetPool<TEnum>(TEnum poolIdx, Action<XAsset64> action)
        {
            nint poolPtr = (nint)(PoolsAddress + Convert.ToUInt32(poolIdx) * sizeof(XAssetPool64));
            XAssetPool64 pool = ReadMemory<XAssetPool64>(poolPtr);
            for (nint assetPtr = pool.Root; assetPtr != 0; assetPtr = ReadMemory<XAsset64>(assetPtr).Next)
            {
                XAsset64 asset = ReadMemory<XAsset64>(assetPtr);
                if (asset.Header == 0) continue;
                action(asset);
            }
        }

        public static unsafe XAsset64[] GetXAssets<TEnum>(TEnum poolIdx)
        {
            nint poolPtr = (nint)(PoolsAddress + Convert.ToUInt32(poolIdx) * sizeof(XAssetPool64));
            XAssetPool64 pool = ReadMemory<XAssetPool64>(poolPtr);
            List<XAsset64> assets = new List<XAsset64>();
            for (nint assetPtr = pool.Root; assetPtr != 0; assetPtr = ReadMemory<XAsset64>(assetPtr).Next)
            {
                XAsset64 asset = ReadMemory<XAsset64>(assetPtr);
                if (asset.Header == 0) continue;
                assets.Add(asset);
            }
            return assets.ToArray();
        }

        public static unsafe XAsset64 FindAsset<TEnum>(TEnum poolIdx, ulong hash)
        {
            nint poolPtr = (nint)(PoolsAddress + Convert.ToUInt32(poolIdx) * sizeof(XAssetPool64));
            XAssetPool64 pool = ReadMemory<XAssetPool64>(poolPtr);
            for (nint assetPtr = pool.Root; assetPtr != 0; assetPtr = ReadMemory<XAsset64>(assetPtr).Next)
            {
                XAsset64 asset = ReadMemory<XAsset64>(assetPtr);
                if (asset.Header == 0) continue;
                if (hash == ReadMemory<ulong>(asset.Header)) return asset;
            }
            return new XAsset64()
            {
                Header = 0
            };
        }

        public static bool IsSinglePlayer()
        {
            return Flags.Contains("sp");
        }

        public static unsafe T ReadMemory<T>(nint address, bool isExternal = true) where T : unmanaged
        {
            if (!isExternal)
            {
                return *(T*)address;
            }

            T result = new();
            var size = (nuint)Marshal.SizeOf<T>();

            _ = PInvoke.ReadProcessMemory((HANDLE)ProcessHandle.DangerousGetHandle(), (void*)address, &result, size);

            return result;
        }

        public static unsafe byte[] ReadRawMemory(nint address, long size)
        {
            var buffer = new byte[size];

            fixed (void* bufferPtr = buffer)
            {
                _ = PInvoke.ReadProcessMemory((HANDLE)ProcessHandle.DangerousGetHandle(), (void*)address, bufferPtr, (nuint)size);
            }

            return buffer;
        }

        public static unsafe string ReadString(nint address)
        {
            List<byte> bytes = new List<byte>();
            while (true)
            {
                byte c = ReadMemory<byte>(address);
                if (c == 0x00) break;
                bytes.Add(c);
                address += 1;
            }

            return Encoding.UTF8.GetString(bytes.ToArray());
        }

        public static unsafe void WriteMemory<T>(nint address, T value) where T : unmanaged
        {
            var size = (nuint)Marshal.SizeOf<T>();
            _ = PInvoke.WriteProcessMemory((HANDLE)ProcessHandle.DangerousGetHandle(), (void*)address, &value, size);
        }
    }
}