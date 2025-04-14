using HashbrownPackages.Structures;
using System.IO.Compression;
using Newtonsoft.Json;
using Serilog;
using System.Text;

namespace HashbrownPackages.Games
{
    public class BlackOps6 : BaseGame
    {
        public override string GameName => "BlackOps6";

        public override void Process()
        {
            ProcessRawFile();
            ProcessWeaponAnimPkg();
            ProcessGestures();
            ProcessExecution();
            ProcessStringTable();
        }

        public override void ProcessStringTable()
        {
            string path = GetAssetPath("StringTable");
            XAsset64[] assets = Cordycep.GetXAssets(BlackOps6XAssetType.STRINGTABLE);
            foreach (XAsset64 asset in assets)
            {
                BlackOps6StringTable stringTable = Cordycep.ReadMemory<BlackOps6StringTable>(asset.Header);
                BlackOps6StringTableColumn[] columns = stringTable.Columns;
                object[][] spreadsheet = new object[stringTable.RowCount][];
                for(int i = 0; i < stringTable.RowCount; i++)
                {
                    spreadsheet[i] = new object[stringTable.ColumnCount];
                }

                if (columns.Length == 0) continue;
                for (int i = 0; i < stringTable.ColumnCount; i++)
                {
                    BlackOps6StringTableColumn column = columns[i];
                    object[] columnData = column.GetColumnData();
                    for(int j = 0; j < column.RowCount; j++)
                    {
                        ushort idx = column.GetRowIndex(j);
                        object data = columnData[j];
                        spreadsheet[idx][i] = data;
                    }
                }

                StringBuilder sb = new StringBuilder();

                //format spreadsheet
                for (int i = 0; i < stringTable.RowCount; i++)
                {
                    sb.AppendLine(string.Join(",", spreadsheet[i]));
                }

                File.WriteAllText(Path.Combine(path, $"{stringTable.Name}.csv"), sb.ToString());
            }
        }

        public override void ProcessExecution()
        {
            string path = GetAssetPath("Executions");
            XAsset64[] assets = Cordycep.GetXAssets(BlackOps6XAssetType.EXECUTION);
            Dictionary<string, string[]> executions = new Dictionary<string, string[]>();
            foreach (XAsset64 asset in assets)
            {
                BlackOps6Execution execution = Cordycep.ReadMemory<BlackOps6Execution>(asset.Header);
            }
            File.WriteAllText(Path.Combine(path, "executions.json"), JsonConvert.SerializeObject(executions, Formatting.Indented));
        }

        public override void ProcessWeaponAnimPkg()
        {
            string path = GetAssetPath("WeaponAnimationPackages");
            XAsset64[] assets = Cordycep.GetXAssets(BlackOps6XAssetType.ANIMPKG);
            Dictionary<string, string[]> animPkgs = new Dictionary<string, string[]>();
            foreach (XAsset64 asset in assets)
            {
                BlacksOp6AnimPackage animPkg = Cordycep.ReadMemory<BlacksOp6AnimPackage>(asset.Header);
                string[] anims = new string[animPkg.AnimTree.AnimationCount];
                int i = 0;
                foreach (var animation in animPkg.AnimTree.GetAnimations())
                {
                    anims[i++] = animation.Name;
                }

                animPkgs.Add(animPkg.Name, anims);
            }

            File.WriteAllText(Path.Combine(path, "weapon_anim_pkgs.json"), JsonConvert.SerializeObject(animPkgs, Formatting.Indented));
        }
        public override unsafe void ProcessGestures()
        {
            string path = GetAssetPath("Gestures");
            XAsset64[] assets = Cordycep.GetXAssets(BlackOps6XAssetType.GESTURE);
            Dictionary<string, string[]> gestures = new Dictionary<string, string[]>();
            foreach (XAsset64 asset in assets)
            {
                List<string> anims = new();
                BlackOps6Gesture gesture = Cordycep.ReadMemory<BlackOps6Gesture>(asset.Header);

                foreach (var animation in gesture.GetAnimations())
                {
                    anims.Add(animation.Name);
                }

                foreach(var animation in gesture.GetSecondaryAnimations())
                {
                    anims.Add(animation.Name);
                }

                gestures.Add(gesture.Name, anims.ToArray());
            }

            File.WriteAllText(Path.Combine(path, "gestures.json"), JsonConvert.SerializeObject(gestures, Formatting.Indented));
        }

        public override void ProcessRawFile()
        {
            string path = GetAssetPath("Rawfiles");
            XAsset64[] assets = Cordycep.GetXAssets(BlackOps6XAssetType.RAWFILE);

            foreach (XAsset64 asset in assets)
            {
                BlackOps6RawFile rawFile = Cordycep.ReadMemory<BlackOps6RawFile>(asset.Header);

                if (rawFile.IsCompressed)
                {
                    using (var input = new MemoryStream(rawFile.Data))
                    using (var zlib = new ZLibStream(input, CompressionMode.Decompress))
                    using (var output = new MemoryStream())
                    {
                        zlib.CopyTo(output);
                        File.WriteAllBytes(Path.Combine(path, $"{rawFile.Hash:X}"), output.ToArray());
                    }
                }
                else
                {
                    File.WriteAllBytes(Path.Combine(path, $"{rawFile.Hash:X}"), rawFile.Data);
                }
            }
        }
    }
}
