using System;
using System.Globalization;
using System.Runtime.CompilerServices;
using Uncreated.Warfare.Kits;
using Uncreated.Warfare.Teams;

namespace Uncreated.Warfare.Util.Inventory;

/// <summary>
/// Stores information about pre-made font icons for different items or item redirect types.
/// </summary>
public class ItemIconProvider
{
    private const int WhiteColor = unchecked( (int)0xFFFFFFFF );
    internal readonly ItemIconData[] Defaults =
    [
        New(RedirectType.LaserDesignator,           "阚"),
        New(RedirectType.EntrenchingTool,           "ɐ"),
        New(RedirectType.Bunker,                    "˅"), // new bunker
        New(RedirectType.BunkerBuilt,               "˅"),
        New(RedirectType.RepairStation,             "ɒ"),
        New(RedirectType.RepairStationBuilt,        "ɒ"),
        New(RedirectType.AmmoBag,                   "ɓ"),
        New(RedirectType.Radio,                     "ɔ"),
        New(RedirectType.RadioDamaged,              "ɔ"),
        New(RedirectType.RallyPoint,                "˄"),
        New(RedirectType.BuildSupply,               "ɗ"),
        New(RedirectType.AmmoSupply,                "ɘ"),
        New(RedirectType.Glasses,                   "ə"),
        New(RedirectType.Backpack,                  "ɚ"),
        New(RedirectType.Mask,                      "ɛ"),
        New(RedirectType.Hat,                       "ɝ"),
        New(RedirectType.Vest,                      "ɞ"),
        New(RedirectType.Pants,                     "ɟ"),
        New(RedirectType.Shirt,                     "ɠ"),
        New(RedirectType.StandardAmmoIcon,          "ˊ"), // ɡ (larger)
        New(RedirectType.StandardMeleeIcon,         "ˏ"),
        New(RedirectType.StandardGrenadeIcon,       "↖"),
        New(RedirectType.StandardSmokeGrenadeIcon,  "ɢ"),
        New(RedirectType.StandardBuildable,         "ˎ"),

        New("78fefdd23def4ab6ac8301adfcc3b2d4", "ʰ", "6b563c"),   // Canned Beans
        New("ce28bcc951c645eebae98a881e329316", "ʰ", "a54749"),   // Canned Tomato Soup
        New("96a6f0ef6a3e4829aa959cccb27d84e8", "ʰ", "c69d75"),   // Canned Chicken Soup
        New("53fd785db5d1456380ce1418fe42b575", "ʱ", "a2c0c0"),   // Canned Tuna
        New("acf7e825832f4499bb3b7cbec4f634ca", "ʲ", "a79272"),   // MRE
        New("2a1350f9ca41402fa0f10297b878cc3c", "ʲ", "d1ae39"),   // Chips
        New("d80ee9242eaf4cd2bf820e0d68d47bf2", "ʳ", "604c3e"),   // Chocolate Bar
        New("1ca6017eaa3241f38779f9ee8730899f", "ʳ", "b27fb1"),   // Candy Bar
        New("a829ea9d0ccb4766a4c761e8a1193361", "ʳ", "3f6645"),   // Granola Bar
        New("e3d11c5882d3407496c264d272acf886", "ʳ", "ddcb68"),   // Energy Bar
        New("ddfdcced6bcd4f769c7d335f4e8e5c07", "ʰ", "cbb794"),   // Canned Pasta
        New("507c042dbf734db99bc5e7762dbde495", "ʱ", "843929"),   // Canned Baconu
        New("62b295721b3d4ca0a003cf31319bbe1a", "ʱ", "853b29"),   // Canned Beef
        New("2a52b27ecc6f4cc2920d9c6863b629f4", "ʱ", "3b5b68"),   // Canned Sardines
        New("775dd3dc88e04529a4a98d63e3b0df3e", "ʴ", "e08544"),   // Carrot
        New("dfe28176368e41e08038652796dd1216", "ʵ", "e2c564"),   // Corn
        New("97c48e1bffaa48339e42e788396df94a", "ʶ", "5da865"),   // Lettuce
        New("fc32f1577f094c8f88f18a10c101edfc", "ʷ", "ae3c3c"),   // Tomato
        New("2a3bd550485f43b9ac91ae5f78916d97", "ʸ", "a8917e"),   // Potato
        New("375ee1fb16a848ff8bc5be5d4fafb4fc", "ʹ", "d5bd7b"),   // Wheat
        New("4880f590a948465891188c5f96559340", "ʺ", "c6a970"),   // Bread
        New("681e174db6aa450c8c9c3d96cc26bfce", "ʺ", "c6a970"),   // Tuna Sandwich
        New("a51c979f42b34359bc7df4974ccb8b92", "ʻ", "e3b451"),   // Cheese
        New("b51b4d6f720640b88bd1459f0739572d", "ʺ", "c6a970"),   // Grilled Cheese Sandwich
        New("e3f93cb7593c4268bc38bc37214f56ca", "ʺ", "c6a970"),   // BLT Sandwich
        New("db640db9d3f54c619b4c8ac075667e4a", "ʺ", "c6a970"),   // Ham Sandwich
        New("e0503457d87b4230bfa78350c79d16ec", "ʼ", "c1c1c2"),   // Bandage
        New("3e78a9db8cf74f4e830df4c06f2e9273", "ʼ", "9da08f"),   // Rag
        New("ae46254cfa3b437e9d74a5963e161da4", "ʼ", "c69696"),   // Dressing
        New("52870b441a0248f985543a9ebc31f61e", "ʽ", "a83538"),   // Medkit
        New("b2ad25fcad7a4f109e6dcc76a4d285de", "ʾ", "695445"),   // Splint
        New("81419c5ae34e4f449bfddc9075709ff2", "ʿ", "238f44"),   // Vaccine
        New("221fbf175e594e68892dede8dbd93aa8", "ʿ", "e9bf27"),   // Adrenaline
        New("3697845b16fa4bbfb568117fbcf8c57c", "ʿ", "9f5b90"),   // Morphine
        New("d5fe08c9ad3f4123a636718a15964cc9", "ˀ", "d47837"),   // Antibiotics
        New("8e232c76cb14432bbe5d38d7d2b22825", "ˀ", "4054af"),   // Painkillers
        New("eaa7af6f8a874b2e9eb9c6e53498528d", "ˀ", "4d604b"),   // Vitamins
        New("f2707771eebb4904b1e4b1f58929393b", "ˀ", "90b4b0"),   // Purification Tablets
        New("c3d97ba7e3964b798b739b5632ef009a", "ˀ", "583928"),   // Cough Syrup
        New("5e1d521ecb7f4075aaebd344e838c2ca", "ˁ", "a73537"),   // Bloodbag
        New("7f65a4dcb5dc4bc2b96fe85e6e415808", "˂", "a83538"),   // Suturekit
        New("79271d9bcdd549028fa4b3038b54cb4b", "˃", "a83538"),   // Heatstim
        New("f260c581cf504098956f424d62345982", "ˋ"),             // Binoculars
        New("1f80a9e0c86047d38b72e08e267885f6", "ˌ", "cd2222"),   // Carjack
        New("d5b9f19e2f2a4ee2ab4dc666f32f7df3", "ˍ", "c72121"),   // Portable Gas Can
        New("6c0962c45f874fec9373765cf8c29976", "ˍ", "2f682f"),   // Industrial Gas Can
        New("7d11aa2b07e641cb8362e207327937ec", "ˍ", "634b30"),   // Maple Jerrycan
        New("52e635dbf3b641069759247cdf8336ad", "ˍ", "4c3d2a"),   // Pine Jerrycan
        New("0078ced43dc1454b98c4d0246a4c18ab", "ˍ", "bcbcbc"),   // Birch Jerrycan
        New("47097f72d56c4bfb83bb8947e66396d5", "ˏ"),             // Military Knife
        New("8d80a12a6f564850ab2478927648dd7a", "ː"),             // Detonator
        New("cca8301927e049149fcee2b157a59da1", "↠"),             // Military Nightvision
        New("8c81fbb134aa42e38b7eda8ebede5463", "↠"),             // Civilian Nightvision
        
        New("92df865d6d534bc1b20b7885fddb8af3", "粞"),            // Anti-Tank Mine
        New("830ba3616d434914b682007b7f309d68", "粢"),            // Tripwire Claymore
        New("784bb6878b654d20a6ba48184c997b2f", "竴"),            // Frag Grenade Trap
        New("b01e414db03747509e87ebc515744216", "竳"),            // Frag Grenade
        // New("a2a8a01a58454816a6c9a047df0558ad", "闖"),         // Razorwire (prefer generic buildable icon for now)
        New("010de9d7d1fd49d897dc41249a22d436", "閽"),            // Laser Rangefinder
        New("3879d9014aca4a17b3ed749cf7a9283e", "阚"),            // Laser Designator
        New("ffcf0144431542a0b41c0b5202ce9d17", "笙"),            // Combat Knife
        New("618d0402c0724f1582fffd69f4cc0868", "粬"),            // Detonator (C4_Detonator)
        New("21cbdcec7a8a4c7d93b578d596fc623c", "粠"),            // Cellphone
        New("12baf44a3f154c86a1b320cbfcc7e2d7", "粧"),            // C-4 1-Pack Charge
        New("84c211711ee74f6e90a02c59edf39328", "粨"),            // C-4 2-Pack Charge
        New("85bcbd5ee63d49c19c3c86b4e0d115d6", "粩"),            // C-4 4-Pack Charge
        New("5dcdd5293e31475a9d9207a792fdc3e8", "粪"),            // C-4 8-Pack Charge
        
        New("7bf622df8cfe4d8c8b740fae3e95b957", "ɢ", "9ea2a2"),   // White Smoke Grenade
        New("7bdd473ac66d43e4b5146c3c74020680", "ɢ", "54a36e"),   // Green Smoke Grenade
        New("c9fadfc1008e477ebb9aeaaf0ad9afb9", "ɢ", "b74f51"),   // Red Smoke Grenade
        New("1344161ee08e4297b64b4dc068c5935e", "ɢ", "a152ab"),   // Violet Smoke Grenade
        New("18713c6d9b8f4980bdee830ca9d667ef", "ɢ", "ceb856"),   // Yellow Smoke Grenade
        New("3099f9fe1e9f448e996116f3ff8b03fa", "穄"),            // AKM
        New("f657f18885c14b08baaf13b633fec508", "穅"),            // AKMS
        New("e3858fa5eac5423f80218dca2fcf0386", "穆"),            // AK-74
        New("01490240cf144110b19fdeea8e15efd9", "窴"),            // AK-74M todo get the right model
        New("0cdc5fb5657f47d189ef9e4dc2c67990", "穈"),            // G36C
        New("3b43187a2ef1457193494329781360bf", "穉"),            // AWM
        New("47dc44144d9f4826b18fc359ed2457da", "穋"),            // AKS-74U
        New("bbba052df7ad45469aa50920b0a1d003", "穌"),            // M4A1
        New("cb32957ca1f1464a8ac0b8dd085190e6", "積"),            // Skorpion EVO
        New("3c7f13c0424745709081e37399b120fc", "穏"),            // HK417
        New("176cec2baddf40dfad27ccfbb4ae82f8", "穒"),            // M1A SOCOM
        New("af9d5f6761c242f083b7b1bfb6e0627d", "穓"),            // UMP 45
        New("6f141f16eb8f4845ba0d21f856f58a73", "穕"),            // Remington 870
        New("5ded5febfdfc4e0a9f0fd1c98dac1ad0", "穖"),            // R700
        New("ab9713258dc34af3906fcf7f9819f0d8", "穘"),            // Vityaz-SN
        New("aee713e74a4e465e97ffbf232f89fdc5", "穚"),            // FN FAL
        New("2ebb42e46d2349399fd26c7bf34856b6", "穛"),            // MP5K
        New("accd00a2d2eb4a0683f8f99d02ed9c83", "穞"),            // P226
        New("364ef99af0a242fc83c32b23eeea9a1d", "穠"),            // Mossberg M590A1
        New("65314a943e8d481888e8d40b19575129", "穡"),            // Skorpion vz. 61
        New("ecdaabba3d814c5aa8155a7064ce5e74", "穣"),            // SG552 Commando
        New("df29380f478a47e4abb53ea82bfcbf77", "穤"),            // P90
        New("35befef74e634190860a91866e7597b2", "穦"),            // AUG A3
        New("f5313493de6d4de780c6a824dfb32ec2", "穧"),            // Glock 21
        New("ae47075e0a7a4d298ba35a4b56ed82c8", "穩"),            // S&W .357
        New("d9b900ec96fe46aeaee7fdb317f41200", "穬"),            // SCAR-L STD (Black)
        New("21d9abe142ee4793887a0a9af3bb2faf", "穫"),            // SCAR-L STD
        New("d3b5e43326b24ae38ea901eae6d37de8", "窊"),            // G3A3
        New("bf86e8bb0b254c148b4b3305465876bd", "窋"),            // Saiga-12K
        New("3277a66bcf8b46c1b22bb4f41d2af18b", "窏"),            // SV-98
        New("5f1a7fda82114c82a3f94444a4bcf3bf", "窑"),            // MP7A2
        New("3ef34bf124cb41c7960150327a563983", "窕"),            // X95 Micro
        New("99ef872bc54f436eae5f7a31076bf854", "窖"),            // CheyTac M200
        New("00e4b6a1947b44f2ad606ede8ad6039b", "窙"),            // PKM
        New("71b5e1bb346b4d2dbee967140d200122", "窛"),            // Vector .45
        New("67a9a831c6b2401080380d8bafc1bfd8", "窞"),            // MPX
        New("fa1c2a389f944cc09b86723793f37893", "窠"),            // MCX
        New("344ceeb663c54dfd805b611d1251db52", "窲"),            // SVD
        New("a0089dc0d04e4232a3d23e9d9911e814", "窶"),            // M16A4
        New("c53866f9670f4b0e910b763d37d736cc", "窸"),            // Mk 17 (SCAR-H)
        New("c4d3e9a959be41b0b5f02c5a27e543e1", "窹"),            // AS Val
        New("d6c2dde610a34ffd9857b578c4233c34", "窾"),            // PM md. 63
        New("8c691bac4f014c8cbbc12d3b03567d92", "窿"),            // M1014
        New("d401727cd6b846acacf1acc21c411c5d", "竀"),            // M110
        New("85614386d9014f9896602343cbf2cdd2", "竁"),            // AK-12
        New("11dea3472f6d4f32bcbd650b0715f5eb", "竂"),            // 682
        New("c6fbf1f7bb56481cba74cb942a0456fa", "竆"),            // M70 New Haven
        New("229e8b0ef1a04d5b87fd208906ef3ee2", "竇"),            // USP 45
        New("065e84c5ee9c4a4b8c2b2e9332a77ad9", "竈"),            // M1911
        New("1069d0d9ba7e451a8967bcc33a25527e", "竊"),            // P250
        New("c895aa0a642e47dbaf5cf38d89c8b421", "竌"),            // Glock 19
        New("b3a8e771cf1a4f2d82ccdd25e318f394", "竍"),            // Makarov
        New("7d1277bcba06460ab0a21d30bca81377", "竏"),            // AKS-74
        New("02405227a26c4fc5920ab27b5099a07a", "竐"),            // MP5A3
        New("d94a3a2008f547aeb9405d5aae8c342c", "竑"),            // Draco Pistol
        New("202465340fae46268b30c52d2f4d150f", "竕"),            // Mx4 Storm
        New("a1487b3b2f724403b848975c0bc5b3f7", "竘"),            // PP-91 KEDR
        New("12359a610e4c47f6ae986a274ca744ff", "竚"),            // VSS Vintorez
        New("caeef6ae008f4ae5a2e8f4a7f328b8eb", "竜"),            // C-14 Timberwolf
        New("e866e92feb4b4aeb85b1f148a2b4b9b0", "竝"),            // Mini-14
        New("247aca2bdca94f178183d98defa58ad9", "竞"),            // Glock 18C
        New("09aa31634c3846128b3a30911452f052", "章"),            // Five-Seven
        New("bb49f2cfdbdb4a738d8699c5f1cbf016", "竣"),            // SKS
        New("1db7463764624a9aa6541d868e409ece", "竩"),            // Mosin Nagant M1891
        New("59c18de87c4d43608f477f70aa85a56a", "竫"),            // Model 1892
        New("6d5060aeac5e4aa99376cd9a80091024", "竬"),            // AR-15
        New("017708a3d61748e78efc6a1946c29ae0", "竾"),            // Uzi
        New("85e44ca2aa6c4d94935f3f1b1abd6db7", "竿"),            // M9A1
        New("7a9d87af584a40fb8d52a8ebb2c227a7", "笀"),            // M93R
        New("dcacfb2a93c1442b9cf202c2b103ebaf", "笂"),            // Sawed Off 682
        New("c1b7946ca8e04b47ab35487acb55f38c", "笃"),            // AMD-65
        New("911695ceb7c744f79b6da34e95fbacc0", "笄"),            // .357 Snubnose
        New("c4a261c8372d43a789afa2e46a0dd1ca", "笅"),            // AR-9
        New("7744fe2e0cdf4bb0bdb262860c17228d", "笇"),            // AUG A3 9mm
        New("b55175e0ccbd4b6ab97627f645c572b2", "笈"),            // HK416
        New("c81ffa25054d4925be101f5c41601a2f", "笉"),            // MP5SD
        New("633a3b46d04d4c35bd50b61478756344", "笋"),            // MAC-10
        New("e9db2c5326564bad882810c3595d46c0", "縬"),            // AA-12
        New("e66719e9307e42de985f2d8fc3102c36", "縳"),            // OTs-14 Groza-1
        New("2c9521b7a2c846ac8c87e8a61d36b7b0", "縵"),            // OTs-14 Groza-4
        New("29c659108b4f453a82906db667ec2d30", "縷"),            // FB Beryl wz. 96
        New("4d1322dbda454e3795b8c4993e0b652b", "縸"),            // FB Beryl M762
        New("58204804d8664ed497be83bd1b23347e", "縹"),            // GOL-Sniper Magnum
        New("6b93cecf3bb04173bd43be447f368041", "縻"),            // Browning Hi-Power
        New("6db50cbf81a14ff5bde87cba0c7d964d", "縼"),            // PPSh-41
        New("3c2a61f5ada848e386f06165b20d1601", "繀"),            // RPK-74M
        New("6398062e445c47249128cbaec108471b", "繃"),            // AK-105
        New("a98b3bc4fa1540a881ee1e727f5fb5b2", "繄"),            // M249 PIP
        New("eaa2a5b3b88941a09a336207bcfb1cda", "繆"),            // M4
        New("e8d7b34ed15046c0b04209a44736fb08", "繇"),            // M203
        New("7f72a1aa205f48cd9fe7fbcfeb5bd390", "繉"),            // GP-25
        New("34082423bcc04843bb042ddf3519720d", "繋"),            // Colt Python 6"
        New("7cc6a12e7d87466a9cd93c333eedfd36", "繎"),            // M240B
        New("cd99ce5d804c4469bb20233dc744135d", "繐"),            // PKP Pecheneg
        New("e4fef63d83c3495bace28a902df10cd7", "繑"),            // FN MAG
        New("55a7c42a0e16469484509b14bf37bdf1", "繒"),            // MP-443 Grach
        New("4e51ad861de844688d5634258fec758d", "閱"),            // M72 LAW
        New("3ac5dee6ea2a4cc198396facc482ea50", "閳"),            // RPG-26
        New("1477d875bedb476daa6ffb2bb5d75c74", "间"),            // M3 MAAWS
        New("1fb0d8134b524c25abf1a4c41d75ec49", "闷"),            // RPG-28
        New("04cf59190c0d4b2184d201631786c174", "闽"),            // RPG-7V2
        New("a396d465c6db4dda8cb57a2d019f9058", "阊"),            // MG3
        New("0193f0ac34514babb410d0b30c27e9c4", "阌"),            // HK33
        New("cd3fefa27cec46fc84a21269b33e1f80", "阍"),            // HK53A2
        New("8ce9d7b03b224aeebcd043a52d295f10", "阎"),            // HK51
        New("e7575a5c9c094d3593432d27c1b3edac", "阑"),            // G3A4
        New("a03780e22f204cad827fc4da23f40551", "阒"),            // AK-101
        New("782de1244a7a468a8daf23c0b23c6911", "阔"),            // G3SG/1
        New("2ff03ce7320040f39356bc059e4f387d", "阘"),            // HK21
        New("0e15d6ea7fbd4261a1955814244a218c", "队"),            // G36
        New("665a355174ff41ea8181d007ee52fe4c", "阢"),            // G36K
        New("d8bdb80c9f744061a16de20448390ba1", "阤"),            // QBZ-95
        New("039714d20bde4c98885595e8ed83c058", "阧"),            // QBZ-95B
        New("d85deda5f4534b1c8b1b55a516e6d3a6", "阨"),            // QBB-95
        New("b798eba1d1f14e0483f7c767d5acda62", "阪"),            // QBZ-191
        New("e2ccc8c471a74a458cc47d616636ef0b", "阫"),            // QSZ-92
        New("3d392912b122404ca19e89222e581815", "阭"),            // QCW-05
        New("4314f4b4ee92415e9fedd4478941f722", "阱"),            // QBU-88
        New("877eb8579b174752951d8a8ff0feaf68", "阶"),            // NOR982
        New("e3b0a9272ea44da2a66780e184f31a2b", "阷"),            // QJY-88
        New("3d447221ba15438992699c26a15bbf05", "阺"),            // MG4
        New("c19cbeec032d45b5b36d2cc5687c61c1", "阽"),            // QLG-91
        New("0e08c3fee09d4598ac41a80830ef472b", "陀"),            // HK69A1
        New("46eb642a829f44d0ac1ebfd16d739aad", "陃"),            // Panzerfaust 3
        New("15226fcade85443fa3c5444332360a70", "竺"),            // SVD-M
        New("42f3aa4bc58e4b6e8c71108e3aab6402", "竻"),            // SR-3M
        New("a1b0623db7704506971e774dd46e4e69", "笑"),            // Scar SSR
        New("a7f401f339464124aa8a76cbf534ebdf", "绹"),            // R4-C
        New("83886491d7554e919ef83152056e2f1c", "绷"),            // R4
        New("40212074e9d148669f2465e4d61a7705", "笞"),            // PP-19
        New("cc51d4356db94c6ea6e81261e72db04d", "绵"),            // LVOA-C
        New("bfeb2875d1304f95915b99e0bbe85694", "竢"),            // G28
        New("0feb6b1bfff341efb2f4a1ce36ef6746", "笜"),            // Famas
        New("fe89f34c100c49f1aeb24017a291387e", "穙"),            // CS/LR4
        New("75676aaa6aa54e53a190b8fb994295b7", "繖"),            // C9A2
        New("70324be195fd43baac4203b4f0a9bd01", "穪"),            // C8A4
        New("8068954b06ed40b19a684b580a4d43d3", "窷"),            // C7A2
        New("79225c97e4594153a05cbef7ba419780", "繕"),            // C6A1 Flex
        New("2a0c1e3f6d0d4b75aa9c422b6c200773", "童"),            // C20 DMR
        New("a5cb3f67948840c19bc5e008df533ffc", "竬"),            // AR-15
    ];

    private static ItemIconData New(string id, string character, int color = WhiteColor) => new ItemIconData(new Guid(id), character, Unsafe.As<int, Color32>(ref color));

    private static ItemIconData New(string id, string character, string color) => new ItemIconData(new Guid(id), character, HexStringHelper.TryParseColor32(color, CultureInfo.InvariantCulture, out Color32 c) ? c : new Color32(255, 255, 255, 255));

    private static ItemIconData New(RedirectType type, string character, int color = WhiteColor) => new ItemIconData(type, character, Unsafe.As<int, Color32>(ref color));

    private readonly Dictionary<Guid, ItemIconData> _itemData;
    private readonly Dictionary<RedirectType, ItemIconData> _redirectData;

    public ItemIconProvider()
    {
        _itemData = new Dictionary<Guid, ItemIconData>(128);
        _redirectData = new Dictionary<RedirectType, ItemIconData>(24);

        for (int i = 0; i < Defaults.Length; ++i)
        {
            ref ItemIconData data = ref Defaults[i];
            Guid p = data.Item;
            if (p != Guid.Empty)
                _itemData[p] = data;
            else
                _redirectData[data.RedirectType] = data;
        }
    }

    public string? GetCharacter(ItemAsset asset)
    {
        return asset == null ? throw new ArgumentNullException(nameof(asset)) : GetCharacter(asset.GUID);
    }

    public string? GetCharacter(Guid guid)
    {
        return _itemData.TryGetValue(guid, out ItemIconData data) ? data.Character : null;
    }

    public string? GetCharacter(RedirectType type)
    {
        return _redirectData.TryGetValue(type, out ItemIconData data) ? data.Character : null;
    }

    public string GetIcon(RedirectType type, bool rich = true, bool tmpro = true)
    {
        return _redirectData.TryGetValue(type, out ItemIconData data)
            ? GetString(in data, rich, tmpro)
            : Class.None.GetIconString();
    }

    public string? GetIconOrNull(RedirectType type, bool rich = true, bool tmpro = true)
    {
        return _redirectData.TryGetValue(type, out ItemIconData data)
            ? GetString(in data, rich, tmpro)
            : null;
    }

    public bool TryGetIcon(RedirectType type, out string str, bool rich = true, bool tmpro = true)
    {
        if (_redirectData.TryGetValue(type, out ItemIconData data))
        {
            str = GetString(in data, rich, tmpro);
            return true;
        }

        str = Class.None.GetIconString();
        return false;
    }

    public bool TryGetIcon(ItemAsset asset, out string str, bool rich = true, bool tmpro = true)
    {
        return TryGetIcon(asset.GUID, out str, rich, tmpro);
    }

    public string GetIcon(ItemAsset asset, bool rich = true, bool tmpro = true)
    {
        return GetIcon(asset.GUID, rich, tmpro);
    }

    public string? GetIconOrNull(ItemAsset asset, bool rich = true, bool tmpro = true)
    {
        return GetIconOrNull(asset.GUID, rich, tmpro);
    }

    public bool TryGetIcon(Guid guid, out string str, bool rich = true, bool tmpro = true)
    {
        if (_itemData.TryGetValue(guid, out ItemIconData data))
        {
            str = GetString(in data, rich, tmpro);
            return true;
        }

        str = Class.None.GetIconString();
        return false;
    }

    public string GetIcon(Guid guid, bool rich = true, bool tmpro = true)
    {
        return _itemData.TryGetValue(guid, out ItemIconData data)
            ? GetString(in data, rich, tmpro)
            : Class.None.GetIconString();
    }

    public string? GetIconOrNull(Guid guid, bool rich = true, bool tmpro = true)
    {
        return _itemData.TryGetValue(guid, out ItemIconData data)
            ? GetString(in data, rich, tmpro)
            : null;
    }

    private static string GetString(in ItemIconData data, bool rich, bool tmpro)
    {
        if (!rich || Unsafe.As<Color32, int>(ref Unsafe.AsRef(in data.Color)) == WhiteColor)
        {
            return data.Character;
        }

        if (tmpro)
        {
            return string.Create(18, data, (span, state) =>
            {
                span[0] = '<';
                span[1] = '#';
                HexStringHelper.FormatHexColor(state.Color, span.Slice(2, 6));
                span[8] = '>';
                span[9] = state.Character[0];
                ReadOnlySpan<char> color = [ '<', '/', 'c', 'o', 'l', 'o', 'r', '>' ];
                color.CopyTo(span[10..]);
            });
        }
        
        return string.Create(24, data, (span, state) =>
        {
            ReadOnlySpan<char> startColor = [ '<', 'c', 'o', 'l', 'o', 'r', '=', '#' ];
            startColor.CopyTo(span);
            HexStringHelper.FormatHexColor(state.Color, span.Slice(8, 6));
            span[14] = '>';
            span[15] = state.Character[0];
            ReadOnlySpan<char> endColor = [ '<', '/', 'c', 'o', 'l', 'o', 'r', '>' ];
            endColor.CopyTo(span[16..]);
        });
    }

    internal readonly struct ItemIconData
    {
        public readonly Guid Item;
        public readonly RedirectType RedirectType;
        public readonly string Character;
        public readonly Color32 Color;

        public ItemIconData(RedirectType item, string character, Color32 color)
        {
            RedirectType = item;
            Character = character;
            Color = color;
            Item = Guid.Empty;
        }

        public ItemIconData(Guid item, string character, Color32 color)
        {
            Item = item;
            Character = character;
            Color = color;
            RedirectType = RedirectType.None;
        }
    }
}
