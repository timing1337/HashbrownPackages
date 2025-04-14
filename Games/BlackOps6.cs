using HashbrownPackages.Structures;
using Mappie;
using System.IO.Compression;
using Newtonsoft.Json;

namespace HashbrownPackages.Games
{
    public class BlackOps6 : BaseGame
    {
        public override string GameName => "BlackOps6";

        public override void Process()
        {
            //ProcessRawFile();
            ProcessAnimPkg();
        }

        public override void ProcessAnimPkg()
        {
            string path = GetAssetPath("AnimationPackages");
            XAsset64[] assets = Cordycep.GetXAssets(BlackOps6XAssetType.ANIMPKG);
            Dictionary<string, string[]> animPkgs = new Dictionary<string, string[]>();
            foreach (XAsset64 asset in assets)
            {
                BlacksOp6AnimPackage animPkg = Cordycep.ReadMemory<BlacksOp6AnimPackage>(asset.Header);
                BlackOps6AnimTree animTree = Cordycep.ReadMemory<BlackOps6AnimTree>(animPkg.AnimTree);
                string[] anims = new string[animTree.AnimationCount];

                for (ulong i = 0; i < animTree.AnimationCount; i++)
                {
                    nint xanimPtr = Cordycep.ReadMemory<nint>(animTree.Animations + (nint)i * 0x8);
                    BlackOps6XAnim xanim = Cordycep.ReadMemory<BlackOps6XAnim>(xanimPtr);
                    anims[i] = $"xanim_{xanim.Hash:X}";
                }

                animPkgs.Add($"xanim_pkg_{animPkg.Hash:X}", anims);
            }

            File.WriteAllText(Path.Combine(path, "anim_pkgs.json"), JsonConvert.SerializeObject(animPkgs, Formatting.Indented));
        }

        public override void ProcessRawFile()
        {
            string path = GetAssetPath("Rawfiles");
            XAsset64[] assets = Cordycep.GetXAssets(BlackOps6XAssetType.RAWFILE);

            foreach (XAsset64 asset in assets)
            {
                BlackOps6RawFile rawFile = Cordycep.ReadMemory<BlackOps6RawFile>(asset.Header);

                byte[] data = Cordycep.ReadRawMemory(rawFile.Data, rawFile.GetLength());
                if (rawFile.IsCompressed())
                {
                    using (var input = new MemoryStream(data))
                    using (var zlib = new ZLibStream(input, CompressionMode.Decompress))
                    using (var output = new MemoryStream())
                    {
                        zlib.CopyTo(output);
                        File.WriteAllBytes(Path.Combine(path, $"{rawFile.Hash:X}"), output.ToArray());
                    }
                }
                else
                {
                    File.WriteAllBytes(Path.Combine(path, $"{rawFile.Hash:X}"), data);
                }
            }
        }
    }
}
