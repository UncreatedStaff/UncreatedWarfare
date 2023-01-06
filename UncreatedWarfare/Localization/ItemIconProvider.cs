using SDG.Unturned;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Uncreated.Framework;
using Uncreated.Framework.Assets;
using Uncreated.SQL;
using Uncreated.Warfare.Kits;
using UnityEngine;

namespace Uncreated.Warfare;
internal static class ItemIconProvider
{
    private const int WHITE = unchecked((int)0xFFFFFFFF);
    private static readonly ItemIconData[] Defaults =
    {
        New("3099f9fe1e9f448e996116f3ff8b03fa", '穄', WHITE), // AKM
        New("f657f18885c14b08baaf13b633fec508", '穅', WHITE), // AKMS
        New("e3858fa5eac5423f80218dca2fcf0386", '穆', WHITE), // AK-74
        New("01490240cf144110b19fdeea8e15efd9", '穆', WHITE), // AK-74M todo get the right model
        New("0cdc5fb5657f47d189ef9e4dc2c67990", '穈', WHITE), // G36C
        New("3b43187a2ef1457193494329781360bf", '穉', WHITE), // AWM
        New("47dc44144d9f4826b18fc359ed2457da", '穋', WHITE), // AKS-74U
        New("bbba052df7ad45469aa50920b0a1d003", '穌', WHITE), // M4A1
        New("cb32957ca1f1464a8ac0b8dd085190e6", '積', WHITE), // Skorpion EVO
        New("3c7f13c0424745709081e37399b120fc", '穏', WHITE), // HK417
        New("176cec2baddf40dfad27ccfbb4ae82f8", '穒', WHITE), // M1A SOCOM
        New("af9d5f6761c242f083b7b1bfb6e0627d", '穓', WHITE), // UMP 45
        New("6f141f16eb8f4845ba0d21f856f58a73", '穕', WHITE), // Remington 870
        New("5ded5febfdfc4e0a9f0fd1c98dac1ad0", '穖', WHITE), // R700
        New("ab9713258dc34af3906fcf7f9819f0d8", '穘', WHITE), // Vityaz-SN
        New("aee713e74a4e465e97ffbf232f89fdc5", '穚', WHITE), // FN FAL
        New("2ebb42e46d2349399fd26c7bf34856b6", '穛', WHITE), // MP5K
        New("accd00a2d2eb4a0683f8f99d02ed9c83", '穞', WHITE), // P226
        New("364ef99af0a242fc83c32b23eeea9a1d", '穠', WHITE), // Mossberg M590A1
        New("65314a943e8d481888e8d40b19575129", '穡', WHITE), // Skorpion vz. 61
        New("ecdaabba3d814c5aa8155a7064ce5e74", '穣', WHITE), // SG552 Commando
        New("df29380f478a47e4abb53ea82bfcbf77", '穤', WHITE), // P90
        New("35befef74e634190860a91866e7597b2", '穦', WHITE), // AUG A3
        New("f5313493de6d4de780c6a824dfb32ec2", '穧', WHITE), // Glock 21
        New("ae47075e0a7a4d298ba35a4b56ed82c8", '穩', WHITE), // S&W .357
        New("21d9abe142ee4793887a0a9af3bb2faf", '穫', WHITE), // SCAR-L STD
        New("d3b5e43326b24ae38ea901eae6d37de8", '窊', WHITE), // G3A3
        New("bf86e8bb0b254c148b4b3305465876bd", '窋', WHITE), // Saiga-12K
        New("3277a66bcf8b46c1b22bb4f41d2af18b", '窏', WHITE), // SV-98
        New("5f1a7fda82114c82a3f94444a4bcf3bf", '窑', WHITE), // MP7A2
        New("3ef34bf124cb41c7960150327a563983", '窕', WHITE), // X95 Micro
        New("99ef872bc54f436eae5f7a31076bf854", '窖', WHITE), // CheyTac M200
        New("00e4b6a1947b44f2ad606ede8ad6039b", '窙', WHITE), // PKM
        New("71b5e1bb346b4d2dbee967140d200122", '窛', WHITE), // Vector .45
        New("67a9a831c6b2401080380d8bafc1bfd8", '窞', WHITE), // MPX
        New("fa1c2a389f944cc09b86723793f37893", '窠', WHITE), // MCX
        New("344ceeb663c54dfd805b611d1251db52", '窲', WHITE), // SVD
        New("a0089dc0d04e4232a3d23e9d9911e814", '窶', WHITE), // M16A4
        New("c53866f9670f4b0e910b763d37d736cc", '窸', WHITE), // Mk 17 (SCAR-H)
        New("c4d3e9a959be41b0b5f02c5a27e543e1", '窹', WHITE), // AS Val
        New("d6c2dde610a34ffd9857b578c4233c34", '窾', WHITE), // PM md. 63
        New("8c691bac4f014c8cbbc12d3b03567d92", '窿', WHITE), // M1014
        New("d401727cd6b846acacf1acc21c411c5d", '竀', WHITE), // M110
        New("85614386d9014f9896602343cbf2cdd2", '竁', WHITE), // AK-12
        New("11dea3472f6d4f32bcbd650b0715f5eb", '竂', WHITE), // 682
        New("c6fbf1f7bb56481cba74cb942a0456fa", '竆', WHITE), // M70 New Haven
        New("229e8b0ef1a04d5b87fd208906ef3ee2", '竇', WHITE), // USP 45
        New("065e84c5ee9c4a4b8c2b2e9332a77ad9", '竈', WHITE), // M1911
        New("1069d0d9ba7e451a8967bcc33a25527e", '竊', WHITE), // P250
        New("c895aa0a642e47dbaf5cf38d89c8b421", '竌', WHITE), // Glock 19
        New("b3a8e771cf1a4f2d82ccdd25e318f394", '竍', WHITE), // Makarov
        New("7d1277bcba06460ab0a21d30bca81377", '竏', WHITE), // AKS-74
        New("02405227a26c4fc5920ab27b5099a07a", '竐', WHITE), // MP5A3
        New("d94a3a2008f547aeb9405d5aae8c342c", '竑', WHITE), // Draco Pistol
        New("202465340fae46268b30c52d2f4d150f", '竕', WHITE), // Mx4 Storm
        New("a1487b3b2f724403b848975c0bc5b3f7", '竘', WHITE), // PP-91 KEDR
        New("12359a610e4c47f6ae986a274ca744ff", '竚', WHITE), // VSS Vintorez
        New("caeef6ae008f4ae5a2e8f4a7f328b8eb", '竜', WHITE), // C-14 Timberwolf
        New("e866e92feb4b4aeb85b1f148a2b4b9b0", '竝', WHITE), // Mini-14
        New("247aca2bdca94f178183d98defa58ad9", '竞', WHITE), // Glock 18C
        New("09aa31634c3846128b3a30911452f052", '章', WHITE), // Five-Seven
        New("bb49f2cfdbdb4a738d8699c5f1cbf016", '竣', WHITE), // SKS
        New("1db7463764624a9aa6541d868e409ece", '竩', WHITE), // Mosin Nagant M1891
        New("59c18de87c4d43608f477f70aa85a56a", '竫', WHITE), // Model 1892
        New("6d5060aeac5e4aa99376cd9a80091024", '竬', WHITE), // AR-15
        New("017708a3d61748e78efc6a1946c29ae0", '竾', WHITE), // Uzi
        New("85e44ca2aa6c4d94935f3f1b1abd6db7", '竿', WHITE), // M9A1
        New("7a9d87af584a40fb8d52a8ebb2c227a7", '笀', WHITE), // M93R
        New("dcacfb2a93c1442b9cf202c2b103ebaf", '笂', WHITE), // Sawed Off 682
        New("c1b7946ca8e04b47ab35487acb55f38c", '笃', WHITE), // AMD-65
        New("911695ceb7c744f79b6da34e95fbacc0", '笄', WHITE), // .357 Snubnose
        New("c4a261c8372d43a789afa2e46a0dd1ca", '笅', WHITE), // AR-9
        New("7744fe2e0cdf4bb0bdb262860c17228d", '笇', WHITE), // AUG A3 9mm
        New("b55175e0ccbd4b6ab97627f645c572b2", '笈', WHITE), // HK416
        New("c81ffa25054d4925be101f5c41601a2f", '笉', WHITE), // MP5SD
        New("633a3b46d04d4c35bd50b61478756344", '笋', WHITE), // MAC-10
        New("e9db2c5326564bad882810c3595d46c0", '縬', WHITE), // AA-12
        New("e66719e9307e42de985f2d8fc3102c36", '縳', WHITE), // OTs-14 Groza-1
        New("2c9521b7a2c846ac8c87e8a61d36b7b0", '縵', WHITE), // OTs-14 Groza-4
        New("29c659108b4f453a82906db667ec2d30", '縷', WHITE), // FB Beryl wz. 96
        New("4d1322dbda454e3795b8c4993e0b652b", '縸', WHITE), // FB Beryl M762
        New("58204804d8664ed497be83bd1b23347e", '縹', WHITE), // GOL-Sniper Magnum
        New("6b93cecf3bb04173bd43be447f368041", '縻', WHITE), // Browning Hi-Power
        New("6db50cbf81a14ff5bde87cba0c7d964d", '縼', WHITE), // PPSh-41
        New("3c2a61f5ada848e386f06165b20d1601", '繀', WHITE), // RPK-74M
        New("6398062e445c47249128cbaec108471b", '繃', WHITE), // AK-105
        New("a98b3bc4fa1540a881ee1e727f5fb5b2", '繄', WHITE), // M249 PIP
        New("eaa2a5b3b88941a09a336207bcfb1cda", '繆', WHITE), // M4
        New("e8d7b34ed15046c0b04209a44736fb08", '繇', WHITE), // M203
        New("7f72a1aa205f48cd9fe7fbcfeb5bd390", '繉', WHITE), // GP-25
        New("34082423bcc04843bb042ddf3519720d", '繋', WHITE), // Colt Python 6"
        New("7cc6a12e7d87466a9cd93c333eedfd36", '繎', WHITE), // M240B
        New("cd99ce5d804c4469bb20233dc744135d", '繐', WHITE), // PKP Pecheneg
        New("e4fef63d83c3495bace28a902df10cd7", '繑', WHITE), // FN MAG
        New("55a7c42a0e16469484509b14bf37bdf1", '繒', WHITE), // MP-443 Grach
        New("4e51ad861de844688d5634258fec758d", '閱', WHITE), // M72 LAW
        New("3ac5dee6ea2a4cc198396facc482ea50", '閳', WHITE), // RPG-26
        New("010de9d7d1fd49d897dc41249a22d436", '閽', WHITE), // Laser Rangefinder
        New("1477d875bedb476daa6ffb2bb5d75c74", '间', WHITE), // M3 MAAWS
        New("1fb0d8134b524c25abf1a4c41d75ec49", '闷', WHITE), // RPG-28
        New("04cf59190c0d4b2184d201631786c174", '闽', WHITE), // RPG-7V2
        New("a396d465c6db4dda8cb57a2d019f9058", '阊', WHITE), // MG3
        New("0193f0ac34514babb410d0b30c27e9c4", '阌', WHITE), // HK33
        New("cd3fefa27cec46fc84a21269b33e1f80", '阍', WHITE), // HK53A2
        New("8ce9d7b03b224aeebcd043a52d295f10", '阎', WHITE), // HK51
        New("e7575a5c9c094d3593432d27c1b3edac", '阑', WHITE), // G3A4
        New("a03780e22f204cad827fc4da23f40551", '阒', WHITE), // AK-101
        New("782de1244a7a468a8daf23c0b23c6911", '阔', WHITE), // G3SG/1
        New("2ff03ce7320040f39356bc059e4f387d", '阘', WHITE), // HK21
        New("3879d9014aca4a17b3ed749cf7a9283e", '阚', WHITE), // Laser Designator
        New("0e15d6ea7fbd4261a1955814244a218c", '队', WHITE), // G36
        New("665a355174ff41ea8181d007ee52fe4c", '阢', WHITE), // G36K
        New("d8bdb80c9f744061a16de20448390ba1", '阤', WHITE), // QBZ-95
        New("039714d20bde4c98885595e8ed83c058", '阧', WHITE), // QBZ-95 (B)
        New("d85deda5f4534b1c8b1b55a516e6d3a6", '阨', WHITE), // QBB-95
        New("b798eba1d1f14e0483f7c767d5acda62", '阪', WHITE), // QBZ-191
        New("e2ccc8c471a74a458cc47d616636ef0b", '阫', WHITE), // QSZ-92
        New("3d392912b122404ca19e89222e581815", '阭', WHITE), // QCW-05
        New("4314f4b4ee92415e9fedd4478941f722", '阱', WHITE), // QBU-88
        New("877eb8579b174752951d8a8ff0feaf68", '阶', WHITE), // NOR982
        New("e3b0a9272ea44da2a66780e184f31a2b", '阷', WHITE), // QJY-88
        New("3d447221ba15438992699c26a15bbf05", '阺', WHITE), // MG4
        New("c19cbeec032d45b5b36d2cc5687c61c1", '阽', WHITE), // QLG-91
        New("0e08c3fee09d4598ac41a80830ef472b", '陀', WHITE), // HK69A1
        New("46eb642a829f44d0ac1ebfd16d739aad", '陃', WHITE), // Panzerfaust 3
        new ItemIconData(RedirectType.EntrenchingTool, '\0', WHITE),  // todo

        
        New("d9b900ec96fe46aeaee7fdb317f41200", "21d9abe142ee4793887a0a9af3bb2faf"), // SCAR-L STD (Black)
    };
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static ItemIconData New(string id, char character, int color = WHITE) =>
        new ItemIconData(new Guid(id), character, color);
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static ItemIconData New(string id, string parentGuid) =>
        new ItemIconData(new Guid(id), new Guid(parentGuid));
    private static readonly Dictionary<Guid, ItemIconData> Data = new Dictionary<Guid, ItemIconData>(Defaults.Length);
    static ItemIconProvider()
    {
        for (int i = 0; i < Defaults.Length; ++i)
        {
            ref ItemIconData data = ref Defaults[i];
            Guid p = data.Parent;
            if (p != default && p != data.Item)
            {
                for (int j = 0; j < Defaults.Length; ++j)
                {
                    ref ItemIconData parent = ref Defaults[j];
                    if (parent.Item == p)
                    {
                        data = new ItemIconData(data.Item, p, parent.Character, parent.PackedColor, true, true);
                    }
                }
            }
        }
    }
    public static char? GetCharacter(ItemAsset asset) => GetCharacter(asset.GUID);
    public static char? GetCharacter(Guid guid)
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        lock (Data)
        {
            return Data.TryGetValue(guid, out ItemIconData data) ? data.Character : null;
        }
    }

    public static string GetIconOrName(ItemAsset asset, bool rich = true, bool tmpro = false)
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        lock (Data)
        {
            if (Data.TryGetValue(asset.GUID, out ItemIconData data))
            {
                string str = data.Character.HasValue ? new string(data.Character.Value, 1) : asset.itemName;
                if (rich)
                    str = tmpro ? str.ColorizeTMPro(data.HexColor, true) : str.Colorize(data.HexColor);

                return str;
            }
        }

        return asset.itemName;
    }

    private static async Task AddDefaults(CancellationToken token = default)
    {
        if (Defaults.Length == 0) return;
        StringBuilder builder = new StringBuilder(
            $"INSERT INTO `{TABLE_MAIN}` (`{COLUMN_ITEM}`,`{COLUMN_ICON}`,`{COLUMN_COLOR}`,`{COLUMN_PARENT}`) VALUES ", 64 + Defaults.Length * 10);
        object[] objs = new object[Defaults.Length * 4];
        lock (Data)
        {
            Data.Clear();
            for (int i = 0; i < Defaults.Length; ++i)
            {
                ItemIconData data = Defaults[i];
                int index = i * 4;
                F.AppendPropertyList(builder, index, 4);
                objs[index] = data.Item.ToString("N");
                objs[index + 1] = data.Character.HasValue && (data.ParentCopyFlag & 2) == 0 ? data.Character.Value : DBNull.Value;
                if ((data.ParentCopyFlag & 1) == 0)
                {
                    string clr = data.HexColor.ToLower();
                    if (clr.Length == 6)
                        clr = "ff" + clr;
                    objs[index + 2] = clr;
                }
                else objs[index + 2] = DBNull.Value;
                objs[index + 3] = data.Parent == default ? DBNull.Value : data.Parent.ToString("N");
                if (Data.ContainsKey(data.Item))
                {
                    L.LogWarning("Duplicate item icon data key: " + data.Item.ToString("N") + ".");
                    continue;
                }
                Data.Add(data.Item, data);
            }
        }

        builder.Append(';');
        await Warfare.Data.AdminSql.NonQueryAsync(builder.ToString(), objs, token).ConfigureAwait(false);
    }
    public static async Task DownloadConfig(CancellationToken token = default)
    {
        int val = await Warfare.Data.AdminSql.VerifyTable(Schema, token).ConfigureAwait(false);
        if (val == 1)
        {
            L.LogWarning("Unable to set up item icon config, using defaults.");
            AddDefaultsToData();
            return;
        }
        if (val == 3)
        {
            L.Log("Loading defaults into newly created item icon config.", ConsoleColor.Magenta);
            await AddDefaults(token).ConfigureAwait(false);
            return;
        }
        
        List<ItemIconData> data2 = new List<ItemIconData>(Defaults.Length);
        await Warfare.Data.AdminSql.QueryAsync(
            $"SELECT `{COLUMN_ITEM}`,`{COLUMN_ICON}`,`{COLUMN_COLOR}`,`{COLUMN_PARENT}` FROM `{TABLE_MAIN}`;", null,
            reader =>
            {
                Guid? item = reader.ReadGuidString(0);
                if (!item.HasValue)
                {
                    L.LogWarning("Invalid item value: " + reader.GetString(0));
                    return;
                }
                char? icon = reader.IsDBNull(1) ? null : reader.GetChar(1);
                string? hexColor = null;
                if (!reader.IsDBNull(2))
                {
                    char[] chars = new char[8];
                    reader.GetChars(2, 0L, chars, 0, 8);
                    hexColor = new string(chars);
                }
                Guid? parent = reader.IsDBNull(3) ? null : reader.ReadGuidString(3);
                if (!icon.HasValue && !parent.HasValue)
                {
                    L.LogWarning("Item does not have character or parent: " + item.Value.ToString("N") + ".");
                    return;
                }
                data2.Add(parent.HasValue
                    ? new ItemIconData(item.Value, parent.Value, icon, hexColor is null ? WHITE : Util.PackHex(hexColor), false, false)
                    : new ItemIconData(item.Value, icon, hexColor is null ? WHITE : Util.PackHex(hexColor)));
            }, token).ConfigureAwait(false);
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        lock (Data)
        {
            Data.Clear();
            for (int i = 0; i < data2.Count; ++i)
            {
                ItemIconData data = data2[i];
                if (Data.ContainsKey(data.Item))
                {
                    L.LogWarning("Duplicate item icon data key: " + data.Item.ToString("N") + ".");
                    continue;
                }
                
                if (data.Parent != default)
                {
                    Guid parent = data.Parent;
                    for (int j = 0; j < data2.Count; ++j)
                    {
                        if (data2[j].Item == parent)
                        {
                            data = new ItemIconData(data.Item, parent,
                                data.Character.HasValue
                                    ? data.Character
                                    : data2[j].Character,
                                data.PackedColor is WHITE or 0
                                    ? data2[j].PackedColor
                                    : data.PackedColor,
                                data.PackedColor is WHITE or 0, !data.Character.HasValue);
                            break;
                        }
                    }
                }
                Data.Add(data.Item, data);
            }
        }
    }
    private static void AddDefaultsToData()
    {
        lock (Data)
        {
            Data.Clear();
            for (int i = 0; i < Defaults.Length; ++i)
            {
                ref ItemIconData data = ref Defaults[i];
                if (Data.ContainsKey(data.Item))
                {
                    L.LogWarning("Duplicate item icon data key: " + data.Item.ToString("N") + ".");
                    continue;
                }
                Data.Add(data.Item, data);
            }
        }
    }

    private const string TABLE_MAIN = "item_icon_config";
    private const string COLUMN_ITEM = "Item";
    private const string COLUMN_ICON = "Icon";
    private const string COLUMN_COLOR = "Color";
    private const string COLUMN_PARENT = "Parent";
    private static readonly Schema Schema = new Schema(TABLE_MAIN, new Schema.Column[]
    {
        new Schema.Column(COLUMN_ITEM, SqlTypes.GUID_STRING)
        {
            PrimaryKey = true
        },
        new Schema.Column(COLUMN_ICON, "char(1)") { Nullable = true },
        new Schema.Column(COLUMN_COLOR, "char(8)") { Nullable = true, Default = "ffffffff" },
        new Schema.Column(COLUMN_PARENT, SqlTypes.GUID_STRING) { Nullable = true }
    }, true, typeof(ItemIconData));
    private readonly struct ItemIconData
    {
        public readonly Guid Item;
        public readonly RedirectType RedirectType;
        public readonly char? Character;
        public readonly Guid Parent;
        public readonly int PackedColor;
        public readonly byte ParentCopyFlag;
        public string HexColor => Util.UnpackHexStr(PackedColor);
        public ItemIconData(Guid item, Guid parent, char? character, int color, bool copiedColor, bool copiedCharacter) : this(item, character, color)
        {
            Parent = parent;
            ParentCopyFlag = (byte)((copiedColor ? 1 : 0) | (copiedCharacter ? 2 : 0));
        }
        public ItemIconData(Guid item, Guid parent) : this(item, default, default)
        {
            this.Parent = parent;
            ParentCopyFlag = 0b11;
        }
        public ItemIconData(RedirectType item, char? character, int color)
        {
            RedirectType = item;
            Character = character;
            PackedColor = color;
            Parent = default;
            ParentCopyFlag = 0;
            Item = default;
        }
        public ItemIconData(Guid item, char? character, int color)
        {
            Item = item;
            Character = character;
            PackedColor = color;
            Parent = default;
            ParentCopyFlag = 0;
            RedirectType = RedirectType.None;
        }
    }
}
