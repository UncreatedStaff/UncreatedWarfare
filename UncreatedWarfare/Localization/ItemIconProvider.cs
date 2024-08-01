using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using Uncreated.Warfare.Kits;
using Uncreated.Warfare.Teams;

namespace Uncreated.Warfare;
internal static class ItemIconProvider
{
    private const int WhiteColor = unchecked((int)0xFFFFFFFF);
    internal static readonly ItemIconData[] Defaults =
    {
        // Block: IPA Extensions

        new ItemIconData(RedirectType.LaserDesignator, '阚', WhiteColor),
        new ItemIconData(RedirectType.EntrenchingTool, 'ɐ', WhiteColor),
        new ItemIconData(RedirectType.Bunker, 'ɑ', WhiteColor),
        new ItemIconData(RedirectType.BunkerBuilt, 'ɑ', WhiteColor),
        new ItemIconData(RedirectType.RepairStation, 'ɒ', WhiteColor),
        new ItemIconData(RedirectType.RepairStationBuilt, 'ɒ', WhiteColor),
        new ItemIconData(RedirectType.AmmoBag, 'ɓ', WhiteColor),
        new ItemIconData(RedirectType.Radio, 'ɔ', WhiteColor),
        new ItemIconData(RedirectType.RadioDamaged, 'ɔ', WhiteColor),
        new ItemIconData(RedirectType.RallyPoint, 'ɕ', WhiteColor),
        new ItemIconData(RedirectType.BuildSupply, 'ɗ', WhiteColor),
        new ItemIconData(RedirectType.AmmoSupply, 'ɘ', WhiteColor),
        new ItemIconData(RedirectType.Glasses, 'ə', WhiteColor),
        new ItemIconData(RedirectType.Backpack, 'ɚ', WhiteColor),
        new ItemIconData(RedirectType.Mask, 'ɛ', WhiteColor),
        new ItemIconData(RedirectType.Hat, 'ɝ', WhiteColor),
        new ItemIconData(RedirectType.Vest, 'ɞ', WhiteColor),
        new ItemIconData(RedirectType.Pants, 'ɟ', WhiteColor),
        new ItemIconData(RedirectType.Shirt, 'ɠ', WhiteColor),
        new ItemIconData(RedirectType.StandardAmmoIcon, 'ɡ', WhiteColor),
        new ItemIconData(RedirectType.StandardMeleeIcon, '¤', WhiteColor),
        new ItemIconData(RedirectType.StandardGrenadeIcon, '¬', WhiteColor),
        new ItemIconData(RedirectType.StandardSmokeGrenadeIcon, 'ɢ', WhiteColor),

        // Block: Spacing Modifier Letters
        
        New("78fefdd23def4ab6ac8301adfcc3b2d4", 'ʰ', "6b563c"),   // Canned Beans
        New("ce28bcc951c645eebae98a881e329316", 'ʰ', "a54749"),   // Canned Tomato Soup
        New("96a6f0ef6a3e4829aa959cccb27d84e8", 'ʰ', "c69d75"),   // Canned Chicken Soup
        New("53fd785db5d1456380ce1418fe42b575", 'ʱ', "a2c0c0"),   // Canned Tuna
        New("acf7e825832f4499bb3b7cbec4f634ca", 'ʲ', "a79272"),   // MRE
        New("2a1350f9ca41402fa0f10297b878cc3c", 'ʲ', "d1ae39"),   // Chips
        New("d80ee9242eaf4cd2bf820e0d68d47bf2", 'ʳ', "604c3e"),   // Chocolate Bar
        New("1ca6017eaa3241f38779f9ee8730899f", 'ʳ', "b27fb1"),   // Candy Bar
        New("a829ea9d0ccb4766a4c761e8a1193361", 'ʳ', "3f6645"),   // Granola Bar
        New("e3d11c5882d3407496c264d272acf886", 'ʳ', "ddcb68"),   // Energy Bar
        New("ddfdcced6bcd4f769c7d335f4e8e5c07", 'ʰ', "cbb794"),   // Canned Pasta
        New("507c042dbf734db99bc5e7762dbde495", 'ʱ', "843929"),   // Canned Baconu
        New("62b295721b3d4ca0a003cf31319bbe1a", 'ʱ', "853b29"),   // Canned Beef
        New("2a52b27ecc6f4cc2920d9c6863b629f4", 'ʱ', "3b5b68"),   // Canned Sardines
        New("775dd3dc88e04529a4a98d63e3b0df3e", 'ʴ', "e08544"),   // Carrot
        New("dfe28176368e41e08038652796dd1216", 'ʵ', "e2c564"),   // Corn
        New("97c48e1bffaa48339e42e788396df94a", 'ʶ', "5da865"),   // Lettuce
        New("fc32f1577f094c8f88f18a10c101edfc", 'ʷ', "ae3c3c"),   // Tomato
        New("2a3bd550485f43b9ac91ae5f78916d97", 'ʸ', "a8917e"),   // Potato
        New("375ee1fb16a848ff8bc5be5d4fafb4fc", 'ʹ', "d5bd7b"),   // Wheat
        New("4880f590a948465891188c5f96559340", 'ʺ', "c6a970"),   // Bread
        New("681e174db6aa450c8c9c3d96cc26bfce", 'ʺ', "c6a970"),   // Tuna Sandwich
        New("a51c979f42b34359bc7df4974ccb8b92", 'ʻ', "e3b451"),   // Cheese
        New("b51b4d6f720640b88bd1459f0739572d", 'ʺ', "c6a970"),   // Grilled Cheese Sandwich
        New("e3f93cb7593c4268bc38bc37214f56ca", 'ʺ', "c6a970"),   // BLT Sandwich
        New("db640db9d3f54c619b4c8ac075667e4a", 'ʺ', "c6a970"),   // Ham Sandwich
        New("e0503457d87b4230bfa78350c79d16ec", 'ʼ', "c1c1c2"),   // Bandage
        New("3e78a9db8cf74f4e830df4c06f2e9273", 'ʼ', "9da08f"),   // Rag
        New("ae46254cfa3b437e9d74a5963e161da4", 'ʼ', "c69696"),   // Dressing
        New("52870b441a0248f985543a9ebc31f61e", 'ʽ', "a83538"),   // Medkit
        New("b2ad25fcad7a4f109e6dcc76a4d285de", 'ʾ', "695445"),   // Splint
        New("81419c5ae34e4f449bfddc9075709ff2", 'ʿ', "238f44"),   // Vaccine
        New("221fbf175e594e68892dede8dbd93aa8", 'ʿ', "e9bf27"),   // Adrenaline
        New("3697845b16fa4bbfb568117fbcf8c57c", 'ʿ', "9f5b90"),   // Morphine
        New("d5fe08c9ad3f4123a636718a15964cc9", 'ˀ', "d47837"),   // Antibiotics
        New("8e232c76cb14432bbe5d38d7d2b22825", 'ˀ', "4054af"),   // Painkillers
        New("eaa7af6f8a874b2e9eb9c6e53498528d", 'ˀ', "4d604b"),   // Vitamins
        New("f2707771eebb4904b1e4b1f58929393b", 'ˀ', "90b4b0"),   // Purification Tablets
        New("c3d97ba7e3964b798b739b5632ef009a", 'ˀ', "583928"),   // Cough Syrup
        New("5e1d521ecb7f4075aaebd344e838c2ca", 'ˁ', "a73537"),   // Bloodbag
        New("7f65a4dcb5dc4bc2b96fe85e6e415808", '˂', "a83538"),   // Suturekit
        New("79271d9bcdd549028fa4b3038b54cb4b", '˃', "a83538"),   // Heatstim

        // Block: CJK Unified Ideographs

        New("7bf622df8cfe4d8c8b740fae3e95b957", 'ɢ', "9ea2a2"), // White Smoke Grenade
        New("7bdd473ac66d43e4b5146c3c74020680", 'ɢ', "54a36e"), // Green Smoke Grenade
        New("c9fadfc1008e477ebb9aeaaf0ad9afb9", 'ɢ', "b74f51"), // Red Smoke Grenade
        New("1344161ee08e4297b64b4dc068c5935e", 'ɢ', "a152ab"), // Violet Smoke Grenade
        New("18713c6d9b8f4980bdee830ca9d667ef", 'ɢ', "ceb856"), // Yellow Smoke Grenade
        New("3099f9fe1e9f448e996116f3ff8b03fa", '穄', WhiteColor), // AKM
        New("f657f18885c14b08baaf13b633fec508", '穅', WhiteColor), // AKMS
        New("e3858fa5eac5423f80218dca2fcf0386", '穆', WhiteColor), // AK-74
        New("01490240cf144110b19fdeea8e15efd9", '穆', WhiteColor), // AK-74M todo get the right model
        New("0cdc5fb5657f47d189ef9e4dc2c67990", '穈', WhiteColor), // G36C
        New("3b43187a2ef1457193494329781360bf", '穉', WhiteColor), // AWM
        New("47dc44144d9f4826b18fc359ed2457da", '穋', WhiteColor), // AKS-74U
        New("bbba052df7ad45469aa50920b0a1d003", '穌', WhiteColor), // M4A1
        New("cb32957ca1f1464a8ac0b8dd085190e6", '積', WhiteColor), // Skorpion EVO
        New("3c7f13c0424745709081e37399b120fc", '穏', WhiteColor), // HK417
        New("176cec2baddf40dfad27ccfbb4ae82f8", '穒', WhiteColor), // M1A SOCOM
        New("af9d5f6761c242f083b7b1bfb6e0627d", '穓', WhiteColor), // UMP 45
        New("6f141f16eb8f4845ba0d21f856f58a73", '穕', WhiteColor), // Remington 870
        New("5ded5febfdfc4e0a9f0fd1c98dac1ad0", '穖', WhiteColor), // R700
        New("ab9713258dc34af3906fcf7f9819f0d8", '穘', WhiteColor), // Vityaz-SN
        New("aee713e74a4e465e97ffbf232f89fdc5", '穚', WhiteColor), // FN FAL
        New("2ebb42e46d2349399fd26c7bf34856b6", '穛', WhiteColor), // MP5K
        New("accd00a2d2eb4a0683f8f99d02ed9c83", '穞', WhiteColor), // P226
        New("364ef99af0a242fc83c32b23eeea9a1d", '穠', WhiteColor), // Mossberg M590A1
        New("65314a943e8d481888e8d40b19575129", '穡', WhiteColor), // Skorpion vz. 61
        New("ecdaabba3d814c5aa8155a7064ce5e74", '穣', WhiteColor), // SG552 Commando
        New("df29380f478a47e4abb53ea82bfcbf77", '穤', WhiteColor), // P90
        New("35befef74e634190860a91866e7597b2", '穦', WhiteColor), // AUG A3
        New("f5313493de6d4de780c6a824dfb32ec2", '穧', WhiteColor), // Glock 21
        New("ae47075e0a7a4d298ba35a4b56ed82c8", '穩', WhiteColor), // S&W .357
        New("21d9abe142ee4793887a0a9af3bb2faf", '穫', WhiteColor), // SCAR-L STD
        New("d3b5e43326b24ae38ea901eae6d37de8", '窊', WhiteColor), // G3A3
        New("bf86e8bb0b254c148b4b3305465876bd", '窋', WhiteColor), // Saiga-12K
        New("3277a66bcf8b46c1b22bb4f41d2af18b", '窏', WhiteColor), // SV-98
        New("5f1a7fda82114c82a3f94444a4bcf3bf", '窑', WhiteColor), // MP7A2
        New("3ef34bf124cb41c7960150327a563983", '窕', WhiteColor), // X95 Micro
        New("99ef872bc54f436eae5f7a31076bf854", '窖', WhiteColor), // CheyTac M200
        New("00e4b6a1947b44f2ad606ede8ad6039b", '窙', WhiteColor), // PKM
        New("71b5e1bb346b4d2dbee967140d200122", '窛', WhiteColor), // Vector .45
        New("67a9a831c6b2401080380d8bafc1bfd8", '窞', WhiteColor), // MPX
        New("fa1c2a389f944cc09b86723793f37893", '窠', WhiteColor), // MCX
        New("344ceeb663c54dfd805b611d1251db52", '窲', WhiteColor), // SVD
        New("a0089dc0d04e4232a3d23e9d9911e814", '窶', WhiteColor), // M16A4
        New("c53866f9670f4b0e910b763d37d736cc", '窸', WhiteColor), // Mk 17 (SCAR-H)
        New("c4d3e9a959be41b0b5f02c5a27e543e1", '窹', WhiteColor), // AS Val
        New("d6c2dde610a34ffd9857b578c4233c34", '窾', WhiteColor), // PM md. 63
        New("8c691bac4f014c8cbbc12d3b03567d92", '窿', WhiteColor), // M1014
        New("d401727cd6b846acacf1acc21c411c5d", '竀', WhiteColor), // M110
        New("85614386d9014f9896602343cbf2cdd2", '竁', WhiteColor), // AK-12
        New("11dea3472f6d4f32bcbd650b0715f5eb", '竂', WhiteColor), // 682
        New("c6fbf1f7bb56481cba74cb942a0456fa", '竆', WhiteColor), // M70 New Haven
        New("229e8b0ef1a04d5b87fd208906ef3ee2", '竇', WhiteColor), // USP 45
        New("065e84c5ee9c4a4b8c2b2e9332a77ad9", '竈', WhiteColor), // M1911
        New("1069d0d9ba7e451a8967bcc33a25527e", '竊', WhiteColor), // P250
        New("c895aa0a642e47dbaf5cf38d89c8b421", '竌', WhiteColor), // Glock 19
        New("b3a8e771cf1a4f2d82ccdd25e318f394", '竍', WhiteColor), // Makarov
        New("7d1277bcba06460ab0a21d30bca81377", '竏', WhiteColor), // AKS-74
        New("02405227a26c4fc5920ab27b5099a07a", '竐', WhiteColor), // MP5A3
        New("d94a3a2008f547aeb9405d5aae8c342c", '竑', WhiteColor), // Draco Pistol
        New("202465340fae46268b30c52d2f4d150f", '竕', WhiteColor), // Mx4 Storm
        New("a1487b3b2f724403b848975c0bc5b3f7", '竘', WhiteColor), // PP-91 KEDR
        New("12359a610e4c47f6ae986a274ca744ff", '竚', WhiteColor), // VSS Vintorez
        New("caeef6ae008f4ae5a2e8f4a7f328b8eb", '竜', WhiteColor), // C-14 Timberwolf
        New("e866e92feb4b4aeb85b1f148a2b4b9b0", '竝', WhiteColor), // Mini-14
        New("247aca2bdca94f178183d98defa58ad9", '竞', WhiteColor), // Glock 18C
        New("09aa31634c3846128b3a30911452f052", '章', WhiteColor), // Five-Seven
        New("bb49f2cfdbdb4a738d8699c5f1cbf016", '竣', WhiteColor), // SKS
        New("1db7463764624a9aa6541d868e409ece", '竩', WhiteColor), // Mosin Nagant M1891
        New("59c18de87c4d43608f477f70aa85a56a", '竫', WhiteColor), // Model 1892
        New("6d5060aeac5e4aa99376cd9a80091024", '竬', WhiteColor), // AR-15
        New("017708a3d61748e78efc6a1946c29ae0", '竾', WhiteColor), // Uzi
        New("85e44ca2aa6c4d94935f3f1b1abd6db7", '竿', WhiteColor), // M9A1
        New("7a9d87af584a40fb8d52a8ebb2c227a7", '笀', WhiteColor), // M93R
        New("dcacfb2a93c1442b9cf202c2b103ebaf", '笂', WhiteColor), // Sawed Off 682
        New("c1b7946ca8e04b47ab35487acb55f38c", '笃', WhiteColor), // AMD-65
        New("911695ceb7c744f79b6da34e95fbacc0", '笄', WhiteColor), // .357 Snubnose
        New("c4a261c8372d43a789afa2e46a0dd1ca", '笅', WhiteColor), // AR-9
        New("7744fe2e0cdf4bb0bdb262860c17228d", '笇', WhiteColor), // AUG A3 9mm
        New("b55175e0ccbd4b6ab97627f645c572b2", '笈', WhiteColor), // HK416
        New("c81ffa25054d4925be101f5c41601a2f", '笉', WhiteColor), // MP5SD
        New("633a3b46d04d4c35bd50b61478756344", '笋', WhiteColor), // MAC-10
        New("e9db2c5326564bad882810c3595d46c0", '縬', WhiteColor), // AA-12
        New("e66719e9307e42de985f2d8fc3102c36", '縳', WhiteColor), // OTs-14 Groza-1
        New("2c9521b7a2c846ac8c87e8a61d36b7b0", '縵', WhiteColor), // OTs-14 Groza-4
        New("29c659108b4f453a82906db667ec2d30", '縷', WhiteColor), // FB Beryl wz. 96
        New("4d1322dbda454e3795b8c4993e0b652b", '縸', WhiteColor), // FB Beryl M762
        New("58204804d8664ed497be83bd1b23347e", '縹', WhiteColor), // GOL-Sniper Magnum
        New("6b93cecf3bb04173bd43be447f368041", '縻', WhiteColor), // Browning Hi-Power
        New("6db50cbf81a14ff5bde87cba0c7d964d", '縼', WhiteColor), // PPSh-41
        New("3c2a61f5ada848e386f06165b20d1601", '繀', WhiteColor), // RPK-74M
        New("6398062e445c47249128cbaec108471b", '繃', WhiteColor), // AK-105
        New("a98b3bc4fa1540a881ee1e727f5fb5b2", '繄', WhiteColor), // M249 PIP
        New("eaa2a5b3b88941a09a336207bcfb1cda", '繆', WhiteColor), // M4
        New("e8d7b34ed15046c0b04209a44736fb08", '繇', WhiteColor), // M203
        New("7f72a1aa205f48cd9fe7fbcfeb5bd390", '繉', WhiteColor), // GP-25
        New("34082423bcc04843bb042ddf3519720d", '繋', WhiteColor), // Colt Python 6"
        New("7cc6a12e7d87466a9cd93c333eedfd36", '繎', WhiteColor), // M240B
        New("cd99ce5d804c4469bb20233dc744135d", '繐', WhiteColor), // PKP Pecheneg
        New("e4fef63d83c3495bace28a902df10cd7", '繑', WhiteColor), // FN MAG
        New("55a7c42a0e16469484509b14bf37bdf1", '繒', WhiteColor), // MP-443 Grach
        New("4e51ad861de844688d5634258fec758d", '閱', WhiteColor), // M72 LAW
        New("3ac5dee6ea2a4cc198396facc482ea50", '閳', WhiteColor), // RPG-26
        New("010de9d7d1fd49d897dc41249a22d436", '閽', WhiteColor), // Laser Rangefinder
        New("1477d875bedb476daa6ffb2bb5d75c74", '间', WhiteColor), // M3 MAAWS
        New("1fb0d8134b524c25abf1a4c41d75ec49", '闷', WhiteColor), // RPG-28
        New("04cf59190c0d4b2184d201631786c174", '闽', WhiteColor), // RPG-7V2
        New("a396d465c6db4dda8cb57a2d019f9058", '阊', WhiteColor), // MG3
        New("0193f0ac34514babb410d0b30c27e9c4", '阌', WhiteColor), // HK33
        New("cd3fefa27cec46fc84a21269b33e1f80", '阍', WhiteColor), // HK53A2
        New("8ce9d7b03b224aeebcd043a52d295f10", '阎', WhiteColor), // HK51
        New("e7575a5c9c094d3593432d27c1b3edac", '阑', WhiteColor), // G3A4
        New("a03780e22f204cad827fc4da23f40551", '阒', WhiteColor), // AK-101
        New("782de1244a7a468a8daf23c0b23c6911", '阔', WhiteColor), // G3SG/1
        New("2ff03ce7320040f39356bc059e4f387d", '阘', WhiteColor), // HK21
        New("3879d9014aca4a17b3ed749cf7a9283e", '阚', WhiteColor), // Laser Designator
        New("0e15d6ea7fbd4261a1955814244a218c", '队', WhiteColor), // G36
        New("665a355174ff41ea8181d007ee52fe4c", '阢', WhiteColor), // G36K
        New("d8bdb80c9f744061a16de20448390ba1", '阤', WhiteColor), // QBZ-95
        New("039714d20bde4c98885595e8ed83c058", '阧', WhiteColor), // QBZ-95 (B)
        New("d85deda5f4534b1c8b1b55a516e6d3a6", '阨', WhiteColor), // QBB-95
        New("b798eba1d1f14e0483f7c767d5acda62", '阪', WhiteColor), // QBZ-191
        New("e2ccc8c471a74a458cc47d616636ef0b", '阫', WhiteColor), // QSZ-92
        New("3d392912b122404ca19e89222e581815", '阭', WhiteColor), // QCW-05
        New("4314f4b4ee92415e9fedd4478941f722", '阱', WhiteColor), // QBU-88
        New("877eb8579b174752951d8a8ff0feaf68", '阶', WhiteColor), // NOR982
        New("e3b0a9272ea44da2a66780e184f31a2b", '阷', WhiteColor), // QJY-88
        New("3d447221ba15438992699c26a15bbf05", '阺', WhiteColor), // MG4
        New("c19cbeec032d45b5b36d2cc5687c61c1", '阽', WhiteColor), // QLG-91
        New("0e08c3fee09d4598ac41a80830ef472b", '陀', WhiteColor), // HK69A1
        New("46eb642a829f44d0ac1ebfd16d739aad", '陃', WhiteColor), // Panzerfaust 3

        
        New("d9b900ec96fe46aeaee7fdb317f41200", "21d9abe142ee4793887a0a9af3bb2faf"), // SCAR-L STD (Black)
    };
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static ItemIconData New(string id, char character, int color = WhiteColor) =>
        new ItemIconData(new Guid(id), character, color);
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static ItemIconData New(string id, char character, string color) =>
        new ItemIconData(new Guid(id), character, Util.PackHex(color));
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static ItemIconData New(string id, string parentGuid) =>
        new ItemIconData(new Guid(id), new Guid(parentGuid));
    private static readonly Dictionary<Guid, ItemIconData> ItemData = new Dictionary<Guid, ItemIconData>(Defaults.Length);
    private static readonly Dictionary<RedirectType, ItemIconData> RedirectData = new Dictionary<RedirectType, ItemIconData>(Defaults.Length);
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
        lock (ItemData)
        {
            return ItemData.TryGetValue(guid, out ItemIconData data) ? data.Character : null;
        }
    }
    public static char? GetCharacter(RedirectType type)
    {
        lock (RedirectData)
        {
            return RedirectData.TryGetValue(type, out ItemIconData data) ? data.Character : null;
        }
    }

    public static string GetIcon(RedirectType type, bool rich = true, bool tmpro = false)
    {
        lock (RedirectData)
        {
            if (RedirectData.TryGetValue(type, out ItemIconData data))
            {
                if (data.Character.HasValue)
                {
                    string str = new string(data.Character.Value, 1);
                    if (rich && data.PackedColor != WhiteColor)
                        str = tmpro ? str.ColorizeTMPro(data.HexColor, true) : str.Colorize(data.HexColor);
                    return str;
                }
            }
        }

        return Class.None.GetIcon().ToString();
    }
    public static bool TryGetIcon(RedirectType type, out string str, bool rich = true, bool tmpro = false)
    {
        lock (RedirectData)
        {
            if (RedirectData.TryGetValue(type, out ItemIconData data))
            {
                if (data.Character.HasValue)
                {
                    str = new string(data.Character.Value, 1);
                    if (rich && data.PackedColor != WhiteColor)
                        str = tmpro ? str.ColorizeTMPro(data.HexColor, true) : str.Colorize(data.HexColor);
                    return true;
                }
            }
        }

        str = Class.None.GetIcon().ToString();
        return false;
    }
    public static bool TryGetIcon(ItemAsset asset, out string str, bool rich = true, bool tmpro = false) => TryGetIcon(asset.GUID, out str, rich, tmpro);
    public static string GetIcon(ItemAsset asset, bool rich = true, bool tmpro = false) => GetIcon(asset.GUID, rich, tmpro);
    public static bool TryGetIcon(Guid guid, out string str, bool rich = true, bool tmpro = false)
    {
        lock (ItemData)
        {
            if (ItemData.TryGetValue(guid, out ItemIconData data))
            {
                if (data.Character.HasValue)
                {
                    str = new string(data.Character.Value, 1);
                    if (rich && data.PackedColor != WhiteColor)
                        str = tmpro ? str.ColorizeTMPro(data.HexColor, true) : str.Colorize(data.HexColor);
                    return true;
                }
            }
        }
        str = Class.None.GetIcon().ToString();
        return false;
    }
    public static string GetIcon(Guid guid, bool rich = true, bool tmpro = false)
    {
        lock (ItemData)
        {
            if (ItemData.TryGetValue(guid, out ItemIconData data))
            {
                if (data.Character.HasValue)
                {
                    string str = new string(data.Character.Value, 1);
                    if (rich && data.PackedColor != WhiteColor)
                        str = tmpro ? str.ColorizeTMPro(data.HexColor, true) : str.Colorize(data.HexColor);
                    return str;
                }
            }
        }

        return Class.None.GetIcon().ToString();
    }
    public static string GetIconOrName(ItemAsset asset, bool rich = true, bool tmpro = false)
    {
        lock (ItemData)
        {
            if (ItemData.TryGetValue(asset.GUID, out ItemIconData data))
            {
                string str = data.Character.HasValue ? new string(data.Character.Value, 1) : asset.itemName;
                if (rich && data.PackedColor != WhiteColor)
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
            $"DELETE FROM `{TABLE_MAIN}`; INSERT INTO `{TABLE_MAIN}` (`{COLUMN_ITEM}`,`{COLUMN_REDIRECT_TYPE}`,`{COLUMN_ICON}`,`{COLUMN_COLOR}`,`{COLUMN_PARENT}`) VALUES ", 64 + Defaults.Length * 10);
        object[] objs = new object[Defaults.Length * 5];
        lock (ItemData)
        {
            ItemData.Clear();
            for (int i = 0; i < Defaults.Length; ++i)
            {
                ItemIconData data = Defaults[i];
                int index = i * 5;
                F.AppendPropertyList(builder, index, 5);
                objs[index] = data.Item == default ? DBNull.Value : data.Item.ToString("N");
                objs[index + 1] = data.RedirectType == RedirectType.None ? DBNull.Value : data.RedirectType.ToString();
                objs[index + 2] = data.Character.HasValue && (data.ParentCopyFlag & 2) == 0 ? data.Character.Value : DBNull.Value;
                if ((data.ParentCopyFlag & 1) == 0)
                {
                    string clr = data.HexColor.ToLower();
                    if (clr.Length == 6)
                        clr = "ff" + clr;
                    objs[index + 3] = clr;
                }
                else objs[index + 3] = DBNull.Value;
                objs[index + 4] = data.Parent == default ? DBNull.Value : data.Parent.ToString("N");
                if (data.Item != default)
                {
                    if (ItemData.ContainsKey(data.Item))
                    {
                        L.LogWarning("Duplicate item icon data key: " + data.Item.ToString("N") + ".");
                        continue;
                    }
                    ItemData.Add(data.Item, data);
                }
                else if (data.RedirectType != RedirectType.None)
                {
                    if (RedirectData.ContainsKey(data.RedirectType))
                    {
                        L.LogWarning("Duplicate redirect icon data key: " + data.RedirectType + ".");
                        continue;
                    }
                    RedirectData.Add(data.RedirectType, data);
                }
            }
        }

        builder.Append(';');

        // disable debug logging because it's a lot
        bool debug = Data.AdminSql.DebugLogging;
        Data.AdminSql.DebugLogging = false;

        await Data.AdminSql.NonQueryAsync(builder.ToString(), objs, token).ConfigureAwait(false);

        Data.AdminSql.DebugLogging = debug;
    }

    public static void UseDefaults() => AddDefaultsToData();
    public static async Task DownloadConfig(CancellationToken token = default)
    {
        int val = await Data.AdminSql.VerifyTable(Schema, token).ConfigureAwait(false);
        if (val == 1)
        {
            L.LogWarning("Unable to set up item icon config, using defaults.");
            AddDefaultsToData();
            return;
        }
#if !DEBUG
        if (val == 3)
        {
            L.Log("Loading defaults into newly created item icon config.", ConsoleColor.Magenta);
#endif
            await AddDefaults(token).ConfigureAwait(false);
#if !DEBUG
            return;
        }
#else
        return;
#endif

        List<ItemIconData> data2 = new List<ItemIconData>(Defaults.Length);
        await Data.AdminSql.QueryAsync(
            $"SELECT `{COLUMN_ITEM}`,`{COLUMN_REDIRECT_TYPE}`,`{COLUMN_ICON}`,`{COLUMN_COLOR}`,`{COLUMN_PARENT}` FROM `{TABLE_MAIN}`;", null,
            reader =>
            {
                Guid? item = reader.IsDBNull(0) ? null : reader.ReadGuidString(0);
                RedirectType? type = null;
                if (!item.HasValue)
                {
                    type = reader.IsDBNull(1) ? null : reader.ReadStringEnum<RedirectType>(1);
                    if (!type.HasValue)
                    {
                        L.LogWarning("Invalid item or redirect value for " +
                                     (reader.IsDBNull(0)
                                         ? reader.IsDBNull(1)
                                             ? "(column 0 and 1 are empty, no id)"
                                             : reader.GetString(1)
                                         : reader.GetString(0)) + ".");
                        return;
                    }
                }
                char? icon = reader.IsDBNull(2) ? null : reader.GetChar(2);
                string? hexColor = null;
                if (!reader.IsDBNull(3))
                {
                    char[] chars = new char[8];
                    reader.GetChars(3, 0L, chars, 0, 8);
                    hexColor = new string(chars);
                }
                Guid? parent = reader.IsDBNull(4) ? null : reader.ReadGuidString(4);
                if (!icon.HasValue && !parent.HasValue)
                {
                    if (item.HasValue)
                        L.LogWarning("Item does not have character or parent: " + item.Value.ToString("N") + ".");
                    else
                        L.LogWarning("Redirect does not have character or parent: " + type!.Value + ".");
                    return;
                }

                if (parent.HasValue && item.HasValue)
                {
                    data2.Add(new ItemIconData(item.Value, parent.Value, icon, hexColor is null
                        ? WhiteColor
                        : Util.PackHex(hexColor)
                        , false, false));
                }
                else if (item.HasValue)
                    data2.Add(new ItemIconData(item.Value, icon, hexColor is null ? WhiteColor : Util.PackHex(hexColor)));
                else
                    data2.Add(new ItemIconData(type!.Value, icon, hexColor is null ? WhiteColor : Util.PackHex(hexColor)));
            }, token).ConfigureAwait(false);
        lock (RedirectData)
        {
            lock (ItemData)
            {
                ItemData.Clear();
                RedirectData.Clear();
                for (int i = 0; i < data2.Count; ++i)
                {
                    ItemIconData data = data2[i];
                    if (data.Item != default)
                    {
                        if (ItemData.ContainsKey(data.Item))
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
                                        data.PackedColor is WhiteColor or 0
                                            ? data2[j].PackedColor
                                            : data.PackedColor,
                                        data.PackedColor is WhiteColor or 0, !data.Character.HasValue);
                                    break;
                                }
                            }
                        }
                        ItemData.Add(data.Item, data);
                    }
                    else if (data.RedirectType != RedirectType.None)
                    {
                        if (RedirectData.ContainsKey(data.RedirectType))
                        {
                            L.LogWarning("Duplicate redirect icon data key: " + data.RedirectType + ".");
                            continue;
                        }
                        RedirectData.Add(data.RedirectType, data);
                    }
                }
            }
        }
    }
    private static void AddDefaultsToData()
    {
        lock (RedirectData)
        {
            lock (ItemData)
            {
                ItemData.Clear();
                RedirectData.Clear();
                for (int i = 0; i < Defaults.Length; ++i)
                {
                    ref ItemIconData data = ref Defaults[i];
                    if (data.Item != default)
                    {
                        if (ItemData.ContainsKey(data.Item))
                        {
                            L.LogWarning("Duplicate item icon data key: " + data.Item.ToString("N") + ".");
                            continue;
                        }
                        ItemData.Add(data.Item, data);
                    }
                    else if (data.RedirectType != RedirectType.None)
                    {
                        if (RedirectData.ContainsKey(data.RedirectType))
                        {
                            L.LogWarning("Duplicate redirect icon data key: " + data.RedirectType + ".");
                            continue;
                        }
                        RedirectData.Add(data.RedirectType, data);
                    }
                }
            }
        }
    }

    private const string TABLE_MAIN = "item_icon_config";
    private const string COLUMN_ITEM = "Item";
    private const string COLUMN_REDIRECT_TYPE = "Redirect";
    private const string COLUMN_ICON = "Icon";
    private const string COLUMN_COLOR = "Color";
    private const string COLUMN_PARENT = "Parent";
    private static readonly Schema Schema = new Schema(TABLE_MAIN, new Schema.Column[]
    {
        new Schema.Column(COLUMN_ITEM, SqlTypes.GUID_STRING)
        {
            Nullable = true
        },
        new Schema.Column(COLUMN_REDIRECT_TYPE, SqlTypes.Enum(RedirectType.None))
        {
            Nullable = true
        },
        new Schema.Column(COLUMN_ICON, "char(1)") { Nullable = true },
        new Schema.Column(COLUMN_COLOR, "char(8)") { Nullable = true, Default = "'ffffffff'" },
        new Schema.Column(COLUMN_PARENT, SqlTypes.GUID_STRING) { Nullable = true }
    }, true, typeof(ItemIconData));
    internal readonly struct ItemIconData
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
            Parent = parent;
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
