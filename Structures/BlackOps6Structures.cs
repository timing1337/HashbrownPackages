using Newtonsoft.Json.Linq;
using Serilog;
using System;
using System.Data.Common;
using System.Runtime.InteropServices;

namespace HashbrownPackages.Structures
{
    [StructLayout(LayoutKind.Explicit)]
    public struct BlackOps6RawFile
    {
        [FieldOffset(0)]
        public ulong Hash;
        [FieldOffset(12)]
        public uint CompressedLen;
        [FieldOffset(16)]
        public uint Len; //use this if size is 0
        [FieldOffset(24)]
        public nint DataPtr;

        public bool IsCompressed => CompressedLen != 0;
        public int Length => IsCompressed ? (int)CompressedLen : (int)Len + 1;
        public byte[] Data => Cordycep.ReadRawMemory(DataPtr, Length);
    }

    [StructLayout(LayoutKind.Explicit, Size = 1704)]
    public struct BlacksOp6AnimPackage
    {
        [FieldOffset(0)]
        public ulong Hash;
        [FieldOffset(16)]
        public nint AnimTreePtr;

        public string Name => HashPackage.GetHash(Hash, BlackOps6XAssetType.ANIMPKG);
        public BlackOps6AnimTree AnimTree => Cordycep.ReadMemory<BlackOps6AnimTree>(AnimTreePtr);
    }

    [StructLayout(LayoutKind.Explicit, Size = 384)]
    public struct BlackOps6AnimTree
    {
        [FieldOffset(0)]
        public ulong Hash;
        [FieldOffset(368)]
        public ulong AnimationCount;
        [FieldOffset(376)]
        public nint Animations;
        public string Name => HashPackage.GetHash(Hash);
        public BlackOps6XAnim[] GetAnimations()
        {
            BlackOps6XAnim[] anims = new BlackOps6XAnim[AnimationCount];
            for (ulong i = 0; i < AnimationCount; i++)
            {
                nint xanimPtr = Cordycep.ReadMemory<nint>(Animations + (nint)i * 0x8);
                anims[i] = Cordycep.ReadMemory<BlackOps6XAnim>(xanimPtr);
            }
            return anims;
        }
    }

    [StructLayout(LayoutKind.Explicit, Size = 120)]
    public struct BlackOps6XAnim
    {
        [FieldOffset(0)]
        public ulong Hash;

        public string Name => HashPackage.GetHash(Hash, BlackOps6XAssetType.XANIM);
    }

    [StructLayout(LayoutKind.Explicit, Size = 56)]
    public unsafe struct BlackOps6Gesture
    {
        [FieldOffset(0)]
        public ulong Hash;
        [FieldOffset(8)]
        public nint Animations;
        [FieldOffset(16)]
        public nint SecondaryAnimations;

        public string Name => HashPackage.GetHash(Hash, BlackOps6XAssetType.GESTURE);

        public BlackOps6XAnim[] GetAnimations()
        {
            List<BlackOps6XAnim> anims = new List<BlackOps6XAnim>();
            for (int i = 0; i < 41; i++)
            {
                nint xanimPtr = Cordycep.ReadMemory<nint>(Animations + (nint)i * 0x8);
                if(xanimPtr == 0)
                    continue;
                anims.Add(Cordycep.ReadMemory<BlackOps6XAnim>(xanimPtr));
            }
            return anims.ToArray();
        }

        public BlackOps6XAnim[] GetSecondaryAnimations()
        {
            List<BlackOps6XAnim> anims = new List<BlackOps6XAnim>();
            for (int i = 0; i < 3; i++)
            {
                nint xanimPtr = Cordycep.ReadMemory<nint>(Animations + (nint)i * 0x8);
                if (xanimPtr == 0)
                    continue;
                anims.Add(Cordycep.ReadMemory<BlackOps6XAnim>(xanimPtr));
            }
            return anims.ToArray();
        }
    }

    [StructLayout(LayoutKind.Explicit, Size = 32)]
    public struct BlackOps6StringTable
    {
        [FieldOffset(0)]
        public ulong Hash;
        [FieldOffset(8)]
        public int ColumnCount;
        [FieldOffset(12)]
        public int RowCount;
        [FieldOffset(24)]
        public nint columnsPtr;

        public string Name => HashPackage.GetHash(Hash, BlackOps6XAssetType.STRINGTABLE);

        public BlackOps6StringTableColumn[] Columns
        {
            get
            {
                BlackOps6StringTableColumn[] columns = new BlackOps6StringTableColumn[ColumnCount];
                for (int i = 0; i < ColumnCount; i++)
                {
                    columns[i] = Cordycep.ReadMemory<BlackOps6StringTableColumn>(columnsPtr + (nint)i * 40);
                }
                return columns;
            }
        }
    }

    [StructLayout(LayoutKind.Explicit, Size = 40)]
    public struct BlackOps6StringTableColumn
    {
        [FieldOffset(0)]
        public byte Type;
        [FieldOffset(4)]
        public uint RowCount;
        [FieldOffset(16)]
        public nint RowPtr;
        [FieldOffset(32)]
        public nint DataPtr;

        public ushort GetRowIndex(int j)
        {
            return Cordycep.ReadMemory<ushort>(RowPtr + (j * 2));
        }

        public object[] GetColumnData()
        {
            object[] data = new object[RowCount];
            if (Type == 5 || ((Type - 6) & 0xFB) == 0 || Type == 7 || Type == 8)
            {
                for (int i = 0; i < RowCount; i++)
                {
                    data[i] = HashPackage.GetHash(Cordycep.ReadMemory<ulong>(DataPtr + (i * 8)));
                }
            }else if(Type == 2)
            {
                for (int i = 0; i < RowCount; i++)
                {
                    data[i] = Cordycep.ReadMemory<ulong>(DataPtr + (i * 8));
                }
            }
            else if (Type == 9)
            {
                for (int i = 0; i < RowCount; i++)
                {
                    data[i] = Cordycep.ReadMemory<uint>(DataPtr + (i * 4));
                }
            }
            else if (Type == 4)
            {
                for (int i = 0; i < RowCount; i++)
                {
                    data[i] = Cordycep.ReadMemory<byte>(DataPtr + i);
                }
            }
            else if (Type == 3)
            {
                for (int i = 0; i < RowCount; i++)
                {
                    data[i] = Cordycep.ReadMemory<float>(DataPtr + (i * 4));
                }
            }
            else
            {
                for (int i = 0; i < RowCount; i++)
                {
                    nint stringPtr = Cordycep.ReadMemory<nint>(DataPtr + (i * 8));
                    data[i] = stringPtr != 0 ? Cordycep.ReadString(stringPtr) : "";
                }
            }
            return data;
        }
    }

    [StructLayout(LayoutKind.Explicit, Size = 208)]
    public struct BlackOps6Execution
    {
        [FieldOffset(0)]
        public ulong Hash;
        [FieldOffset(16)]
        public nint Execution1Ptr; //nested
        [FieldOffset(24)]
        public nint Execution2Ptr; //nested
        [FieldOffset(32)]
        public nint Execution3Ptr; //nested
        [FieldOffset(40)]
        public BlackOps6ExecutionAnimation Animation1;
        [FieldOffset(80)]
        public BlackOps6ExecutionAnimation Animation2;
        [FieldOffset(120)]
        public BlackOps6ExecutionAnimation Animation3;

        public string Name => HashPackage.GetHash(Hash, BlackOps6XAssetType.EXECUTION);

        public BlackOps6Execution Execution1 => Cordycep.ReadMemory<BlackOps6Execution>(Execution1Ptr);
        public BlackOps6Execution Execution2 => Cordycep.ReadMemory<BlackOps6Execution>(Execution2Ptr);
        public BlackOps6Execution Execution3 => Cordycep.ReadMemory<BlackOps6Execution>(Execution3Ptr);
    }

    [StructLayout(LayoutKind.Explicit, Size = 40)]
    public struct BlackOps6ExecutionAnimation
    {
        [FieldOffset(0)]
        public nint XCam;
        [FieldOffset(8)]
        public nint XAnimPtr;
        [FieldOffset(16)]
        public nint XAnim2Ptr;

        public BlackOps6XAnim XAnim1 => Cordycep.ReadMemory<BlackOps6XAnim>(XAnimPtr);
        public BlackOps6XAnim XAnim2 => Cordycep.ReadMemory<BlackOps6XAnim>(XAnim2Ptr);
    }

    public enum BlackOps6XAssetType
    {
        PHYSICSLIBRARY = 0, // 0x0
        PHYSICSSFXEVENTASSET = 1, // 0x1
        PHYSICSVFXEVENTASSET = 2, // 0x2
        PHYSICSASSET = 3, // 0x3
        PHYSICSFXPIPELINE = 4, // 0x4
        PHYSICSFXSHAPE = 5, // 0x5
        PHYSICSDEBUGDATA = 6, // 0x6
        XANIM = 7, // 0x7
        XMODELSURFS = 8, // 0x8
        XMODEL = 9, // 0x9
        MAYHEM = 10, // 0xA
        MATERIAL = 11, // 0xB
        COMPUTESHADER = 12, // 0xC
        TILESHADER = 13, // 0xD
        LIBSHADER = 14, // 0xE
        SHADER = 15, // 0xF
        TECHSET = 16, // 0x10
        IMAGE = 17, // 0x11
        SOUNDGLOBALVOLMOD = 18, // 0x12
        SOUNDGLOBALENTCHANNEL = 19, // 0x13
        SOUNDGLOBALCONTEXT = 20, // 0x14
        SOUNDGLOBALWHIZBY = 21, // 0x15
        SOUNDGLOBALBULLETCRACK = 22, // 0x16
        SOUNDGLOBALPERK = 23, // 0x17
        SOUNDGLOBALOCCLUSION = 24, // 0x18
        SOUNDGLOBALSURFACEINFO = 25, // 0x19
        SOUNDGLOBALCURVE = 26, // 0x1A
        SOUNDGLOBALDOPPLER = 27, // 0x1B
        SOUNDGLOBALNAMETABLE = 28, // 0x1C
        SOUNDBANK = 29, // 0x1D
        SOUNDBANKTRANSIENT = 30, // 0x1E
        COL_MAP = 31, // 0x1F
        COM_MAP = 32, // 0x20
        WORLD_EVENT_DATA = 33, // 0x21
        GLASS_MAP = 34, // 0x22
        AIPATHS = 35, // 0x23
        NAVMESH = 36, // 0x24
        TACGRAPH = 37, // 0x25
        MAP_GAME_OBJECTS = 38, // 0x26
        MAP_FX_OBJECTS = 39, // 0x27
        MAP_ENTS = 40, // 0x28
        MAP_ENTS_TRZONE = 41, // 0x29
        FX_MAP = 42, // 0x2A
        GFX_MAP = 43, // 0x2B
        GFX_MAP_TRZONE = 44, // 0x2C
        IESPROFILE = 45, // 0x2D
        LIGHTDEF = 46, // 0x2E
        GRADINGCLUT = 47, // 0x2F
        FOGSPLINE = 48, // 0x30
        PBGBLOOM = 49, // 0x31
        PBGCURVE = 50, // 0x32
        PBGCOLOR = 51, // 0x33
        PBGEFFECTS = 52, // 0x34
        ATMOSPHERICFOG = 53, // 0x35
        VOLUMETRICFOG = 54, // 0x36
        PBGPOSTFXBUNDLE = 55, // 0x37
        PBGPOSTFXBUNDLENVT = 56, // 0x38
        GESTURE = 57, // 0x39
        LOCALIZE = 58, // 0x3A
        ATTACHMENT = 59, // 0x3B
        WEAPON = 60, // 0x3C
        PARTICLESYSTEM = 61, // 0x3D
        IMPACTFX = 62, // 0x3E
        SURFACEFX = 63, // 0x3F
        AITYPE = 64, // 0x40
        CHARACTER = 65, // 0x41
        XMODELALIAS = 66, // 0x42
        RAWFILE = 67, // 0x43
        GSCOBJ = 68, // 0x44
        GSCGDB = 69, // 0x45
        STRINGTABLE = 70, // 0x46
        DDL = 71, // 0x47
        TRACER = 72, // 0x48
        VEHICLE = 73, // 0x49
        NETCONSTSTRINGS = 74, // 0x4A
        LUAFILE = 75, // 0x4B
        SCRIPTABLE = 76, // 0x4C
        VECTORFIELD = 77, // 0x4D
        PARTICLESIMANIMATION = 78, // 0x4E
        STREAMINGINFO = 79, // 0x4F
        LASER = 80, // 0x50
        GAMEPROPS = 81, // 0x51
        MATERIALSTANDARD = 82, // 0x52
        TTF = 83, // 0x53
        SUIT = 84, // 0x54
        SUITANIMPACKAGE = 85, // 0x55
        CAMERA = 86, // 0x56
        HUDOUTLINE = 87, // 0x57
        RUMBLE = 88, // 0x58
        RUMBLEGRAPH = 89, // 0x59
        ANIMPKG = 90, // 0x5A
        SFXPKG = 91, // 0x5B
        VFXPKG = 92, // 0x5C
        FOOTSTEPVFX = 93, // 0x5D
        BEHAVIORTREE = 94, // 0x5E
        BEHAVIORSEQUENCER = 95, // 0x5F
        SIGHTCONFIG = 96, // 0x60
        SIGHTCONFIGTEMPLATE = 97, // 0x61
        AIANIMSET = 98, // 0x62
        AIASM = 99, // 0x63
        PROCEDURALBONES = 100, // 0x64
        DYNAMICBONES = 101, // 0x65
        RETICLE = 102, // 0x66
        XANIMCURVE = 103, // 0x67
        COVERSELECTOR = 104, // 0x68
        BATTLECHATTERTREE = 105, // 0x69
        BATTLECHATTEREVENT = 106, // 0x6A
        ENEMYSELECTOR = 107, // 0x6B
        CLIENTCHARACTER = 108, // 0x6C
        CLOTHASSET = 109, // 0x6D
        CINEMATICMOTION = 110, // 0x6E
        ACCESSORY = 111, // 0x6F
        LOCDMGTABLE = 112, // 0x70
        BULLET_PENETRATION = 113, // 0x71
        SCRIPTBUNDLE = 114, // 0x72
        BLENDSPACE2D = 115, // 0x73
        XCAM = 116, // 0x74
        CAMO = 117, // 0x75
        XCOMPOSITEMODEL = 118, // 0x76
        XMODELDETAILCOLLISION = 119, // 0x77
        STREAMTREEOVERRIDE = 120, // 0x78
        KEYVALUEPAIRS = 121, // 0x79
        STTERRAIN = 122, // 0x7A
        VHMDATA = 123, // 0x7B
        VTMDATA = 124, // 0x7C
        COLLISIONTILE = 125, // 0x7D
        EXECUTION = 126, // 0x7E
        CARRYOBJECT = 127, // 0x7F
        SOUNDBANKLIST = 128, // 0x80
        WEAPONACCURACY = 129, // 0x81
        DECALVOLUMEMATERIAL = 130, // 0x82
        DECALVOLUMEMASK = 131, // 0x83
        DYNENTITYLIST = 132, // 0x84
        FX_MAP_TRZONE = 133, // 0x85
        VOLUMETRICHEIGHTMAP = 134, // 0x86
        DLOGSCHEMA = 135, // 0x87
        EDGELIST = 136, // 0x88
        STANDALONEUMBRATOME = 137, // 0x89
        XBONESET = 138, // 0x8A
        RAGDOLLASSET = 139, // 0x8B
        PHYSICSBONEGRAPH = 140, // 0x8C
        CURVE = 141, // 0x8D
        SKELETONCONSTRAINTS = 142, // 0x8E
        TRIGGEREFFECT = 143, // 0x8F
        WEAPONTRIGGER = 144, // 0x90
        VOLUMETRICCLOUD = 145, // 0x91
        CODCASTERDATA = 146, // 0x92
        WATERSYSTEM = 147, // 0x93
        WATERBUOYANCY = 148, // 0x94
        KEYBINDS = 149, // 0x95
        CALLOUTMARKERPING = 150, // 0x96
        LIGHTSTATE = 151, // 0x97
        RADIANTTELEMETRY = 152, // 0x98
        AIMARKUP = 153, // 0x99
        AIMARKUP_GENERATED = 154, // 0x9A
        SCENARIO = 155, // 0x9B
        AI_INTERACTION = 156, // 0x9C
        MAPVOXELIZEDSTATEFORMAT = 157, // 0x9D
        WATERVIS = 158, // 0x9E
        GAMETYPE = 159, // 0x9F
        GAMETYPETABLE = 160, // 0xA0
        SNDMODIFIER = 161, // 0xA1
        WEAPONBLUEPRINT = 162, // 0xA2
        ATTACHMENTBLUEPRINT = 163, // 0xA3
        MOVINGPLATFORM = 164, // 0xA4
        HWCONFIG = 165, // 0xA5
        TIMELAPSESKY = 166, // 0xA6
        HWCONFIGURATORINFO = 167, // 0xA7
        OBJECTIVEDATA = 168, // 0xA8
        FONT = 169, // 0xA9
        MOTIONMATCHINGFEATURES = 170, // 0xAA
        MOTIONMATCHINGSET = 171, // 0xAB
        GAMETYPESTATDATA = 172, // 0xAC
        FONTICON = 173, // 0xAD
        CALLOUTMARKERPINGWHEEL = 174, // 0xAE
        HWCONFIGVAR = 175, // 0xAF
        ZIVART = 176, // 0xB0
        MOVIE = 177, // 0xB1
        MAPINFO = 178, // 0xB2
        MAPTABLE = 179, // 0xB3
        GLOBALMAPTABLE = 180, // 0xB4
        ACHIEVEMENT = 181, // 0xB5
        ACHIEVEMENTLIST = 182, // 0xB6
        MATERIALDEBUGDATA = 183, // 0xB7
        SCRIPTABLEVARIANT = 184, // 0xB8
        LEAGUEPLAYSEASONS = 185, // 0xB9
        SETTINGCONTEXT = 186, // 0xBA
        AI_EVENTLIST = 187, // 0xBB
        SOUNDEVENT = 188, // 0xBC
        CALLOUTMARKERPINGMETADATA = 189, // 0xBD
        PROJECT = 190, // 0xBE
        PROJECTTABLE = 191, // 0xBF
        GAMEMODE = 192, // 0xC0
        SNDASSET = 193, // 0xC1
        GFXUMBRATOME = 194, // 0xC2
        AUDIOVISUALIZER = 195, // 0xC3
        MATERIALANIMATIONPARAMS = 196, // 0xC4
        NAMEPLATESETTINGS = 197, // 0xC5
        REACTIVEAUDIOPARAMS = 198, // 0xC6
        REACTIVEVFXPACKAGE = 199, // 0xC7
        MATERIALSFXTABLE = 200, // 0xC8
        FOOTSTEPSFXTABLE = 201, // 0xC9
        REACTIVESTAGESET = 202, // 0xCA
        FOLIAGESFXTABLE = 203, // 0xCB
        IMPACTSFXTABLE = 204, // 0xCC
        AIIMPACTVFXTABLE = 205, // 0xCD
        TYPEINFO = 206, // 0xCE
        HANDPLANTSFXTABLE = 207, // 0xCF
        SNDTABLE = 208, // 0xD0
        EQUIPMENTSFX = 209, // 0xD1
        SOUNDSUBMIX = 210, // 0xD2
        SHOCK = 211, // 0xD3
        STORAGEFILE = 212, // 0xD4
        ECSASSET = 213, // 0xD5
        TRACKERFOOTSTEPFX = 214, // 0xD6
        PLAYERSPAWNSETTINGS = 215, // 0xD7
        PLAYERSPAWNINFLUENCER = 216, // 0xD8
        SOUNDSPEAKERMAP = 217, // 0xD9
        REVERBPRESET = 218, // 0xDA
        AISHOOTSTYLESLIST = 219, // 0xDB
        OPERATORLIST = 220, // 0xDC
        OPERATOR = 221, // 0xDD
        OPERATORSKIN = 222, // 0xDE
        DISMEMBERMENT = 223, // 0xDF
        CONVERSATION = 224, // 0xE0
        XANIMNODE = 225, // 0xE1
        SNDCURVE = 226, // 0xE2
        TTLOS = 227, // 0xE3
        MATERIALTINTANIMATIONPARAMS = 228, // 0xE4
        MATERIALUVANIMATIONPARAMS = 229, // 0xE5
        MATERIALCAMOANIMATIONPARAMS = 230, // 0xE6
        MATERIALANIMATION = 231, // 0xE7
        IMPACTFXTABLE = 232, // 0xE8
        IMPACTTYPETOIMPACTFXTABLE = 233, // 0xE9
        REACTIVEOPERATOR = 234, // 0xEA
        WEATHERVOLUME = 235, // 0xEB
        VEHICLETRICK = 236, // 0xEC
        REACTIVEAUDIOPACKAGE = 237, // 0xED
        AMBIENTSFXPACKAGE = 238, // 0xEE
        OBJECTSTOREPROJECT = 239, // 0xEF
        OBJECTSTOREGAMEMODE = 240, // 0xF0
        PROCEDURALBONELODSETTINGS = 241, // 0xF1
        GENERICBLUEPRINT = 242, // 0xF2
        HWCONFIGVARGROUP = 243, // 0xF3
        HWCONFIGTIEREDGROUP = 244, // 0xF4
        SOUNDCONE = 245, // 0xF5
        SNDMASTERPRESET = 246, // 0xF6
        SNDFILTER = 247, // 0xF7
        SNDFUTZ = 248, // 0xF8
        XANIMTREE = 249, // 0xF9
        SOCIALPLAYERPROFILE = 250, // 0xFA
        LOCALIZEASSETENTRYDEV = 251, // 0xFB
        MLDEFORMER = 252, // 0xFC
        MLDEFORMERBINDING = 253, // 0xFD
        XANIMPARAMMODIFIER = 254, // 0xFE
        AISPLINE = 255, // 0xFF
        EMOTE = 256, // 0x100
        WORLDMODELATTACHMENT = 257, // 0x101
        MATERIALFLIPBOOKPARAMS = 258, // 0x102
        MATERIALUVDISTORTIONPARAMS = 259, // 0x103
        CHROMA = 260, // 0x104
        VEHICLETUNABLES = 261, // 0x105
        SNDWEAPONREFLECTIONDELTATWEAKS = 262, // 0x106
        SNDADSR = 263, // 0x107
        SNDBREATHSTATE = 264, // 0x108
        VEHICLEBLUEPRINT = 265, // 0x109
        CHARCOLLBOUNDS = 266, // 0x10A
        WEATHER = 267, // 0x10B
        WEATHERSTATE = 268, // 0x10C
        WEATHERMAP = 269, // 0x10D
        WEATHERTILE = 270, // 0x10E
        SNDTIMESCALE = 271, // 0x10F
        SNDREVERB = 272, // 0x110
        AIOBSTACLE = 273, // 0x111
        SPRAY = 274, // 0x112
        SNDOCCLUSION = 275, // 0x113
        SNDADSRZONE = 276, // 0x114
        XANIMSET = 277, // 0x115
        XANIMSETSTATE = 278, // 0x116
        XANIMSETALIAS = 279, // 0x117
        XANIMSETADDON = 280, // 0x118
        XANIMSETPARAMETERS = 281, // 0x119
        GROUNDDEFORMATIONVIS = 282, // 0x11A
        AIBCSOUNDALIASTABLE = 283, // 0x11B
        AIROUTINE = 284, // 0x11C
        AIACTIONSETTINGS = 285, // 0x11D
        SNDACOUSTICS = 286, // 0x11E
        SCRIPTBRUSHMODEL = 287, // 0x11F
        WEATHERSURFACETYPEMAPPING = 288, // 0x120
        COMPASSDATA = 289, // 0x121
        SHOCKWAVE = 290, // 0x122
        BONETOGGLEPARAMS = 291, // 0x123
        PLAYERLOOKATPROFILE = 292, // 0x124
        WEATHERENVIRONMENTSFXPACKAGE = 293, // 0x125
        AI_TOKEN_COLLECTIONS = 294, // 0x126
        AI_TOKEN_DEFINITIONS = 295, // 0x127
        AI_TOKEN_TYPES = 296, // 0x128
        POSEMATCHINGTRANSITIONSET = 297, // 0x129
        POSEMATCHINGASMCONFIG = 298, // 0x12A
        DEPLOYABLEWEAPONCONFIG = 299, // 0x12B
        AIANIMSETTINGS = 300, // 0x12C
        PARACHUTEDATA = 301, // 0x12D
        DOWNLOADGROUPSET = 302, // 0x12E
        SNDAMBIENT = 303, // 0x12F
        SNDAMBIENTELEMENT = 304, // 0x130
        SNDFULLOCCLUSION = 305, // 0x131
        WEATHERIMPACTSFXTABLE = 306, // 0x132
        INDEXEDLISTTYPE = 307, // 0x133
        FOOTSTEPSFXOVERRIDE = 308, // 0x134
        VOLUMETRICSMOKESETTINGS = 309, // 0x135
        TIMELINE = 310, // 0x136
        RADAR = 311, // 0x137
        IKPARAMETERS = 312, // 0x138
        INDOOREXCLUSIONTILE = 313, // 0x139
        SNDMASTERPRESETBUNDLE = 314, // 0x13A
        CONTRAILDATA = 315, // 0x13B
        VEHLEANOUTANIMCURVESET = 316, // 0x13C
        VISIONSET = 317, // 0x13D
        OBJECTSTORETITLEIDGROUP = 318, // 0x13E
        SNDZONEWEATHER = 319, // 0x13F
        WEATHERPRECIPITATIONSFXPACKAGE = 320, // 0x140
        GOBO = 321, // 0x141
        FOOTSTEPSURFACETABLE = 322, // 0x142
        WEATHERDISTANTSFXPACKAGE = 323, // 0x143
        HYPERPOSE = 324, // 0x144
        IKTARGET = 325, // 0x145
        SNDMUSICSET = 326, // 0x146
        PHYSICSMATERIALLIBRARY = 327, // 0x147
        PHYSICSBODYQUALITYLIBRARY = 328, // 0x148
        PHYSICSMOTIONPROPERTIESLIBRARY = 329, // 0x149
        PHYSICSGLOBALTYPECOMPENDIUM = 330, // 0x14A
        IMPACTSHAKE = 331, // 0x14B
        WEATHERWIND = 332, // 0x14C
        STRING = 333, // 0x14D
        ASSETLIST = 334, // 0x14E
    };
}
