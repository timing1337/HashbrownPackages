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
            ProcessXCamData();
        }

        public override void ProcessXCamData()
        {
            string path = GetAssetPath("XCams");
            XAsset64[] assets = Cordycep.GetXAssets(BlackOps6XAssetType.XCAM);
            foreach (XAsset64 asset in assets)
            {
                Dictionary<string, BlackOps6XCamFrame[]> cameras = new();
                BlackOps6XCam xCam = Cordycep.ReadMemory<BlackOps6XCam>(asset.Header);
                bool added = true;
                foreach (var camera in xCam.GetCameras())
                {
                    if (camera.Name.Contains("victim"))
                    {
                        added = false;
                        break;
                    }
                    cameras.Add(camera.Name, camera.GetAnimationFrames());
                }
                if(!added) continue;
                File.WriteAllText(Path.Combine(path, $"{xCam.Name}.json"), JsonConvert.SerializeObject(cameras, Formatting.Indented));
            }
        }

        public override void ProcessSoundGlobalNameTable()
        {
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
                    for(int j = 0; j < column.RowCount; j++)
                    {
                        spreadsheet[j][i] = column.GetRowData(j);
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

        public override void ProcessDDL()
        {
            string path = GetAssetPath("DDL");
            XAsset64[] assets = Cordycep.GetXAssets(BlackOps6XAssetType.DDL);
            foreach (XAsset64 asset in assets)
            {

            }
        }

        public override void ProccessNetConstString()
        {
            string path = GetAssetPath("NetConstStrings");
            XAsset64[] assets = Cordycep.GetXAssets(BlackOps6XAssetType.NETCONSTSTRINGS);
            Dictionary<string, object[]> netConsts = new();
            foreach (XAsset64 asset in assets)
            {
                BlackOps6NetConstString netConst = Cordycep.ReadMemory<BlackOps6NetConstString>(asset.Header);
                netConsts.Add(netConst.Name, netConst.GetEntries());
            }
            File.WriteAllText(Path.Combine(path, "netconststrings.json"), JsonConvert.SerializeObject(netConsts, Formatting.Indented));
        }

        public override void ProcessXAnimTree()
        {

            string path = GetAssetPath("XAnimTrees");
            XAsset64[] assets = Cordycep.GetXAssets(BlackOps6XAssetType.XANIMTREE);
            Dictionary<string, string[]> animTress = new Dictionary<string, string[]>();
            foreach (XAsset64 asset in assets)
            {
                BlackOps6XAnimTree xAnimTree = Cordycep.ReadMemory<BlackOps6XAnimTree>(asset.Header);
                List<string> anims = new List<string>();
                foreach (var entry in xAnimTree.GetEntries())
                {
                    Log.Information("ParentTree: {0}, xanim {1}, node {2}", entry.XAnimTreeParentPtr, entry.XAnimPtr, entry.XAnimNodePtr);
                }
                animTress.Add(xAnimTree.Name, anims.ToArray());
            }
            File.WriteAllText(Path.Combine(path, "xanimtrees.json"), JsonConvert.SerializeObject(animTress, Formatting.Indented));
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
