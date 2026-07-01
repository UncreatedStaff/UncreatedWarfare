# Getting Started

This guide helps you get set up with a basic Uncreated server. Please read the [license](https://github.com/UncreatedStaff/UncreatedWarfare/blob/master/LICENSE.md) before continuing to make sure that your use case is within it's allowed scope.

A test server can be used for testing, contributing code changes, creating plugins, or hosting private events. Again, see the [license](https://github.com/UncreatedStaff/UncreatedWarfare/blob/master/LICENSE.md) for a complete list of approved uses.

> [!NOTE]
> This guide is for Windows users. Mac OSX does not have a dedicated server build. If you're on Linux... figure it out.

## Requirements

To set up a test server, you need the following things

* [Git](https://git-scm.com/install/windows)
* [Visual Studio](https://visualstudio.microsoft.com/vs/community/) (not VS Code) or the [.NET 10 SDK](https://dotnet.microsoft.com/en-us/download/dotnet/10.0) if you're comfortable with using the command line.
    * If you're installing Visual Studio, ensure you've selected the `.NET desktop development` workload.
* A working MySQL database, at least v8.0.
    * Databases can be made available either through a forwarded port via SSH or directly via an IP address.
    * [Installation guide for MySQL Community](https://dev.mysql.com/doc/refman/8.4/en/windows-installation.html)
    * Create a database for the module to use.
* A map configured for Uncreated using [UncreatedZoneEditor](https://github.com/UncreatedStaff/UncreatedZoneEditor).
    * All official Uncreated maps are configured already.
    * Configuration can be found in the `Uncreated` folder within the map.
    * The map should use the following configuration to match the live server:
    ```json
	"PlayerUI_VirusVisible": false,
	"Max_Walkable_Slope": 60,
	"Has_Global_Electricity": true,
    ```
* Various configurations. Samples are available later on.

### Optional Requirements

The following items are optional.

* A Steam API key, which can be created at [Steam's Website](https://steamcommunity.com/dev/apikey).
    * Without this, many features involving players (including profile pictures) will appear incomplete.
* A plugin featuring a non-seeding gamemode. See FreeTDM for an example of a gamemode plugin.
    * Our gamemodes are not released publicly.
* API key for [UCS](https://restoremonarchy.com/ucs) ban list integration.
* [Zipkin](https://zipkin.io/) telemetry tracking setup.

### Missing

The following items are missing from the publicly available Uncreated build.

* Discord bot and website integration.
    * This is commonly referred to as 'homebase' throughout the code.
* Stripe integration.
* Nitro-enabled features.
* Audio recording functionality.
    * Our implementation of this was adapted from research done by **[Senior S](https://github.com/Senior-S)**, who asked us to keep it private. 
* Anti-cheat
    * Our server contains a very basic anti-cheat that may be expanded in the future.
* Player data
    * We do not release sample player data publicly.


## Building Uncreated Warfare

### Install U3DS via SteamCMD

If you don't already have it, download SteamCMD from [here](https://developer.valvesoftware.com/wiki/SteamCMD#_Windows) and extract it into `C:\SteamCMD`, so that `steamcmd.exe` is in `C:\SteamCMD`.

SteamCMD should be installed at `C:\SteamCMD\steamcmd.exe`. If it's installed elsewhere, you can change the `ServerPath` property in the `UncreatedWarfare.csproj` file to the correct location of U3DS.

Create the following batch file to install and later update the server. You can put this anywhere, such as your desktop.
```powershell
C:\SteamCMD\steamcmd.exe +login anonymous +app_update 1110390 -beta preview validate +quit
```
> [!NOTE]
> This installs the preview branch. To install the live branch, replace `preview` with `none`.

Double-click the file to install U3DS. This may take a little while.

### Create the Server

Navigate to `C:\SteamCMD\steamapps\common\U3DS`.

Copy/paste `ExampleServer.bat` to another file, name it whatever you want, something like `Start Uncreated Warfare.bat`.

Right click the new file, choose `Edit in Notepad` (on windows 10, Open With -> Notepad),

Scroll down until you see a line beginning in `start ...` and edit the name of the server after the '/'. It should say `Example`, which you should change to `UncreatedSeason4` (build scripts rely on this name). Additionally, you should change `LanServer` to `InternetServer` if you want to allow people to join from other networks.

It should look something like this:
```powershell
@echo off
rem ...
rem ...

start "" "%~dp0ServerHelper.bat" +InternetServer/UncreatedSeason4
exit
```
Run the file to start the server for the first time and create the necessary files.

If you are using Uncreated mods, update the following in the `WorkshopDownloadConfig.json`.
```json
{
  "File_IDs": [ 2839462324, 2407740920 ],
  "Ignore_Children_File_IDs": []
}
```

Optionally, edit the `Config.txt` to match the live server.
```properties
Version 1

Server
{
    BattlEye_Secure true
    Max_Ping_Milliseconds 2000
}
Items
{
    Spawn_Chance 1
    Despawn_Natural_Time 600
    Respawn_Time 0.1
    Quality_Full_Chance 1
    Gun_Bullets_Full_Chance 1
    Gun_Bullets_Multiplier 1
    Magazine_Bullets_Full_Chance 1
    Magazine_Bullets_Multiplier 1
    Crate_Bullets_Full_Chance 1
    Has_Durability false
}
Vehicles
{
    Unlocked_After_Seconds_In_Safezone 604800
    Gun_Lowcal_Damage_Multiplier 0.07
    Melee_Damage_Multiplier 0.05
}
Barricades
{
    Decay_Time 0
}
Structures
{
    Decay_Time 0
}
Players
{
    Health_Regen_Min_Food 80
    Health_Regen_Min_Water 80
    Bleed_Damage_Ticks 30
    Bleed_Regen_Ticks 15000
    Armor_Multiplier 1.28
    Experience_Multiplier 0
    Lose_Items_PvP 0
    Lose_Items_PvE 0
    Lose_Clothes_PvP false
    Lose_Clothes_PvE false
    Lose_Weapons_PvP false
    Lose_Weapons_PvE false
    Can_Break_Legs false
    Spawn_With_Stamina_Skills true
    Allow_Per_Character_Saves true
}
Objects
{
    Fuel_Reset_Multiplier 0.2
}
Events
{
    Airdrop_Frequency_Min 0.6
    Airdrop_Frequency_Max 1.5
    Use_Airdrops false
}
Gameplay
{
    Hitmarkers false
    Crosshair false
    Satellite true
    Compass true
    Can_Suicide false
    Timer_Exit 0
    Timer_Respawn 0
    Timer_Home 0
    Timer_Leave_Group 0
    Explosion_Launch_Speed_Multiplier 0.02
    Viewmodel_AimingMisalignmentMultiplier 1
}
```
(just replace the file it'll regenerate with comments on restart)

Set up your `Server\Commands.dat` file:
```properties
Name Uncreated Test Server
Perspective Vehicle
Cheats Enabled
Mode Normal
Port 27015
Map Yellowknife
MaxPlayers 64
```

### Clone the repository

Download the Modules.zip folder from this [drive link](https://drive.google.com/drive/folders/1BveKLd5MuLySaldDG64O22GoDolO_wP3?usp=sharing) and extract it into `U3DS\Modules` so that the Uncreated.Warfare folder is directly within `Modules`:
```
C:\SteamCMD\steamapps\common\U3DS
  Modules
    Uncreated.Warfare
      Bin
      Libraries
        bunch of DLLs
      WorkshopUploader
      UncreatedWarfare.module
```

Clone the repository into a folder somewhere, recommend into `C:\Users\[user]\source\repos`, either using Visual Studio, GitHub Desktop, or with this command:
```powershell
git clone https://github.com/UncreatedStaff/UncreatedWarfare.git
```

You can update it later with
```powershell
git pull
```

### Build the Solution

If using Visual Studio, double-click the .slnx file, then go to **Build** at the top and click **Build Solution**.

Otherwise, open a terminal in the UncreatedWarfare folder and run this command:
```powershell
dotnet build -c Release
```

Double check that `C:\SteamCMD\steamapps\common\U3DS\Modules\Uncreated.Warfare\Bin` and `C:\SteamCMD\steamapps\common\U3DS\Servers\UncreatedSeason4\Warfare\Plugins` now have files in them.

### Configuration

#### Files

Clone the configuration repository into the `Warfare` folder within your server folder. This should be located at `C:\SteamCMD\steamapps\common\U3DS\Servers\UncreatedSeason4\Warfare`.

```powershell
cd C:\SteamCMD\steamapps\common\U3DS\Servers\UncreatedSeason4\Warfare
```

```powershell
git clone https://github.com/UncreatedStaff/Uncreated.Warfare.Configuration .
```

Open `System Config.yml` and update anything that looks out of date. The main thing that needs updating is the connection string at the bottom.

#### Database

---

Run the server to populate the database.

If you're using Visual Studio, you can press the `Start Without Debugging` button (looks like a hollow green play button) at the top to launch the server, otherwise run the batch file again.

You might have to right click on `Unturned` in the solution explorer and choose **Set as Startup Project** if you get an error message trying to start it.

---

Next, use a tool like HeidiSQL to insert configuration data into the database.

The following tables will need sample data:
* `factions`
* `faction_assets`
* `lang_info`
    * Needs a default language such as `en-us` (will be configured later).
* `seasons`
* `maps`
* `maps_dependencies`

---

If you want to use the configuration from the live server, the following SQL script will work to import necessary information.

```sql
SET SESSION sql_mode='NO_AUTO_VALUE_ON_ZERO';

INSERT INTO `factions` (`pk`, `Id`, `Name`, `ShortName`, `Abbreviation`, `HexColor`, `FlagImageUrl`, `SpriteIndex`, `Emoji`, `UnarmedKitId`, `KitPrefix`) VALUES
	(1, 'admins', 'Admins', 'Admins', 'ADMIN', '0099ff', 'https://i.imgur.com/z0HE5P3.png', 0, NULL, NULL, ''),
	(2, 'usa', 'United States', 'USA', 'USA', '78b2ff', 'https://i.imgur.com/P4JgkHB.png', 1, '🇺🇸', NULL, 'us'),
	(3, 'russia', 'Russia', 'Russia', 'RU', 'f53b3b', 'https://i.imgur.com/YMWSUZC.png', 2, '🇷🇺', NULL, 'ru'),
	(4, 'mec', 'Middle Eastern Coalition', 'MEC', 'MEC', 'ffcd8c', 'https://i.imgur.com/rPmpNzz.png', 3, '938653900913901598|938654469518950410', NULL, 'me'),
	(5, 'germany', 'Germany', 'Germany', 'DE', 'ffcc00', 'https://i.imgur.com/91Apxc5.png', 4, '🇩🇪', NULL, 'ge'),
	(6, 'china', 'China', 'China', 'CN', 'ee1c25', 'https://i.imgur.com/Yns89Yk.png', 5, '🇨🇳', NULL, 'ch'),
	(7, 'usmc', 'US Marine Corps', 'U.S.M.C.', 'USMC', '004481', 'https://i.imgur.com/MO9nPmf.png', 6, '989069549817171978|989032657834885150', NULL, 'usmc'),
	(8, 'soviet', 'Soviet', 'Soviet', 'SOV', 'cc0000', 'https://i.imgur.com/vk8gBBm.png', 7, '989037438972334091|989037438972334091', NULL, 'sov'),
	(9, 'poland', 'Poland', 'Poland', 'PL', 'dc143c', 'https://i.imgur.com/fu3nCS3.png', 8, '🇵🇱', NULL, 'pl'),
	(10, 'militia', 'Militia', 'Militia', 'MIL', '526257', 'https://i.imgur.com/z0HE5P3.png', 9, NULL, NULL, 'mi'),
	(11, 'israel', 'Israel Defense Forces', 'IDF', 'IDF', '005eb8', 'https://i.imgur.com/Wzdspd3.png', 10, '🇮🇱', NULL, 'idf'),
	(12, 'france', 'France', 'France', 'FR', '002654', 'https://i.imgur.com/TYY0kwp.png', 11, '🇫🇷', NULL, 'fr'),
	(13, 'canada', 'Canadian Armed Forces', 'Canada', 'CAF', '80aaff', 'https://i.imgur.com/zs81UMe.png', 12, '🇨🇦', NULL, 'caf'),
	(14, 'southafrica', 'South Africa', 'S. Africa', 'ZA', '007749', 'https://i.imgur.com/2orfzTh.png', 13, '🇿🇦', NULL, 'sa'),
	(15, 'mozambique', 'Mozambique', 'Mozambique', 'MZ', 'ffd100', 'https://i.imgur.com/9nXhlMH.png', 14, '🇲🇿', NULL, 'mz');

INSERT INTO `faction_assets` (`pk`, `Faction`, `Redirect`, `Asset`, `VariantKey`) VALUES
	(1, 2, 'AmmoSupply', '51e1e372bf5341e1b4b16a0eacce37eb', NULL),
	(2, 11, 'Shirt', '77dc77768d8f4d6b921bbe9a876432d0', 'jacket'),
	(3, 11, 'Backpack', '67e14c9892b4459bb0d5b7f394f7f91d', 'ruggedpack'),
	(4, 9, 'Mask', '9d849c3f75ac405ca471fd65af4010b6', 'balaclava'),
	(5, 9, 'Hat', 'ece14052a9d64994a3ef2ab1dc27a073', 'base'),
	(6, 9, 'Vest', '44bc4c4333564c61a2e86bd4c2809203', 'tact_rig'),
	(7, 9, 'Pants', 'bf302a8dda994fc08897ed372d8c8cd7', 'pants'),
	(8, 9, 'Shirt', '71d35bb681f34b7196bb0e6685106ec4', 'jacket'),
	(9, 9, 'Backpack', '90f7aa3817834edd82c6458fffbc2780', 'ruggedpack'),
	(10, 8, 'Hat', 'd8c9b02f6ad74216ae25ddd4a98d721c', 'base'),
	(11, 11, 'Pants', 'bc16600f78d248c7b108c912ee6a759f', 'pants'),
	(12, 8, 'Vest', 'b9b61f2d8b1d472d8430991e08e9450e', 'tact_rig'),
	(13, 8, 'Shirt', '157148a3ebfb447e948b04cdd83d9335', 'jacket'),
	(14, 8, 'Backpack', '118c5783814847e7bfe6eac1caa11568', 'ruggedpack'),
	(15, 7, 'Mask', '3a7ff1898393450187e970abfc3efbf1', 'bandana'),
	(16, 7, 'Glasses', '588933b9da0043d6896d3f6d3f2105b4', 'sunglasses'),
	(17, 7, 'Hat', '9b14747d30c94b168898b14b3b03cbdd', 'base'),
	(18, 7, 'Vest', '5a7753b4801948c6b875d6589a2c4398', 'tact_rig'),
	(19, 7, 'Pants', '1a1c1a0065f64543b069e3784f58d5a7', 'pants'),
	(20, 7, 'Shirt', '1d8c612e186b4f1588099c663d9d7a44', 'jacket'),
	(21, 7, 'Backpack', '7971e03a140149f5bbad7d1c51bc7731', 'molle'),
	(22, 8, 'Pants', 'ef9852b99d9e4591904fb42ab9f46134', 'pants'),
	(24, 11, 'Vest', '5fbd2fdc5b454606993afff708244e20', 'tact_rig'),
	(25, 11, 'Mask', '9d849c3f75ac405ca471fd65af4010b6', 'balaclava'),
	(26, 15, 'Pants', '3f0ad0fd305f4deea96a84d4c9ebaae0', 'pants'),
	(27, 15, 'Shirt', 'b9d5f63ed6f84a5c8c339a86828e0642', 'jacket'),
	(28, 15, 'Backpack', '68170172cf2a4dff8ecbd83964a0c13f', 'ruggedpack'),
	(29, 14, 'Mask', '9c2b4e15517e434fac0cf0f4bdf0c278', 'bandana'),
	(30, 14, 'Hat', '1fb9ad79c8d14168bdbcdcb33ed50064', 'base'),
	(31, 14, 'Vest', '060cc097e5a642ff85bedaca7a46c188', 'tact_rig'),
	(32, 14, 'Pants', 'b1ca137776964c1f9bb2cd4f19b4d7b5', 'pants'),
	(33, 14, 'Shirt', '760f1e854d904bcf902b42c22015aa2a', 'jacket'),
	(34, 14, 'Backpack', '0cd247d2c01643e49945ab37b16a6a0a', 'ruggedpack'),
	(35, 11, 'Hat', '6fa1828a5db147bca1c598e5b41fa319', 'base'),
	(36, 13, 'Glasses', '588933b9da0043d6896d3f6d3f2105b4', 'sunglasses'),
	(37, 13, 'Vest', '4626fb373ab648d0b2a67d3fe58017cc', 'tact_rig'),
	(38, 13, 'Pants', '573275f5925c452c96805e9fc5e52d37', 'pants'),
	(39, 13, 'Shirt', 'ae976b9a82ba48a488ae71e4ca3cee55', 'jacket'),
	(40, 13, 'Backpack', 'efb51b45aca34676a5d45ce8f28b7ed7', 'molle'),
	(41, 12, 'Hat', 'b53b694277184045a01ce82c55f81029', 'base'),
	(42, 12, 'Vest', '5ead83aa50984bc085e1dcf34afc606c', 'tact_rig'),
	(43, 12, 'Pants', 'af4625a9a5e04aa8b9105e08c869998f', 'pants'),
	(44, 12, 'Shirt', 'e301b323c52d4feba57fe31e8dea2bca', 'jacket'),
	(45, 12, 'Backpack', 'a5d911ba6c464f89a9913cf198316c53', 'molle'),
	(46, 13, 'Hat', '6e25bcbc24f047698a26d1da3831068f', 'base'),
	(47, 15, 'Vest', '5ead83aa50984bc085e1dcf34afc606c', 'tact_rig'),
	(48, 6, 'Hat', '4dbefaaad6fd4e728912bd929c16c2c6', 'goggles'),
	(49, 6, 'Pants', 'f3a1a4f1f333486480716c42cd5471e9', 'pants'),
	(50, 3, 'Mask', '9d849c3f75ac405ca471fd65af4010b6', 'balaclava'),
	(51, 3, 'Hat', 'e495734ebe274a0085d8b299b5897cb4', 'goggles'),
	(52, 3, 'Vest', '8bcb7b352fe841d88cf421f2d7aa760e', 'tact_rig'),
	(53, 3, 'Pants', 'cede4da725eb4749b66b9d138b0e557d', 'pants'),
	(54, 3, 'Shirt', 'f5c88106d5324175815e730b3b1b897e', 'jacket'),
	(55, 3, 'Backpack', '21f6dd73c756470d9be43aaf694a3632', 'ruggedpack'),
	(56, 3, 'Radio', 'fb910102ad954169abd4b0cb06a112c8', NULL),
	(57, 3, 'RallyPoint', '0d7895360c80440fbe4a45eba28b2007', NULL),
	(58, 3, 'BuildSupply', '6a8b8b3c79604aeea97f53c235947a1f', NULL),
	(59, 4, 'AmmoSupply', 'bfc9aed75a3245acbfd01bc78fcfc875', NULL),
	(60, 3, 'AmmoSupply', '8dd66da5affa480ba324e270e52a46d7', NULL),
	(61, 2, 'Glasses', '588933b9da0043d6896d3f6d3f2105b4', 'sunglasses'),
	(62, 2, 'Hat', '0cd25f11b5864c0e99c1ad7ca4f8ad7d', 'band'),
	(63, 2, 'Vest', 'b5c9c2284ac547b59bad4bf7ad23b602', 'tact_rig'),
	(64, 2, 'Pants', 'ad3740ed150040edafef80594b89357d', 'pants'),
	(65, 2, 'Shirt', 'ee5ecff41ebd4ee082bea183db01193c', 'jacket'),
	(66, 2, 'Backpack', '83075cc3512f4f209a0b32d309c22f56', 'molle'),
	(67, 2, 'Radio', '7715ad81f1e24f60bb8f196dd09bd4ef', NULL),
	(68, 2, 'RallyPoint', '5e1db525179341d3b0c7576876212a81', NULL),
	(69, 2, 'BuildSupply', 'a70978a0b47e4017a0261e676af57042', NULL),
	(70, 2, 'Mask', '3a7ff1898393450187e970abfc3efbf1', 'bandana'),
	(71, 6, 'Vest', 'b74265e7af1c4d52866907e489206f86', 'tact_rig'),
	(72, 4, 'BuildSupply', '9c7122f7e70e4a4da26a49b871087f9f', NULL),
	(73, 4, 'Radio', 'c7754ac78083421da73006b12a56811a', NULL),
	(74, 6, 'Shirt', '2c1a9c62b30a49e7bda2ef6a2727eb8c', 'jacket'),
	(75, 6, 'Backpack', '5ac771b71bb7496bb2042d3e8cc2015c', 'assaultpack'),
	(76, 6, 'Radio', '7bde55f70c494418bdd81926fb7d6359', NULL),
	(77, 6, 'RallyPoint', '7720ced42dba4c1eac16d14453cd8bc4', NULL),
	(78, 6, 'BuildSupply', 'de7c4cafd0304848a7141e3860b2248a', NULL),
	(79, 6, 'AmmoSupply', '2f3cfa9c6bb645fbab8f49ce556d1a1a', NULL),
	(80, 5, 'Hat', '835dc9e72f46431a9bed591bcbbfb081', 'base'),
	(81, 5, 'Vest', '2499cebdfc6646c59103a48f06c4838a', 'tact_rig'),
	(82, 5, 'Pants', '31ed5cd8918e4693bc7431483b130e05', 'pants'),
	(83, 4, 'RallyPoint', 'c03352d9e6bb4e2993917924b604ee76', NULL),
	(84, 5, 'Shirt', 'fc4a2a49f335489a84e294ca03031a82', 'jacket'),
	(85, 5, 'Radio', '439c32cced234f358e101294ea0ce3e4', NULL),
	(86, 5, 'RallyPoint', '49663078b594410b98b8a51e8eff3609', NULL),
	(87, 5, 'BuildSupply', '35eabf178e4e4d82aac34fcbf8e690e3', NULL),
	(88, 5, 'AmmoSupply', '15857c3f693b4209b7b92a0b8438be34', NULL),
	(89, 4, 'Hat', 'f10b4420b7c74fa49e09c69ec27709f6', 'base'),
	(90, 4, 'Vest', 'b9b61f2d8b1d472d8430991e08e9450e', 'tact_rig'),
	(91, 4, 'Pants', '3c0e787a6f034545800023ac3aa589e4', 'pants'),
	(92, 4, 'Shirt', '16d972440c704ad284155369cd5f1e13', 'jacket'),
	(93, 4, 'Backpack', '2f077bfd25074bad9d8e24d5af29fab4', 'assaultpack'),
	(94, 5, 'Backpack', 'd77a232ad1fb4cf78dde280fd7c14a0b', 'assaultpack'),
	(95, 15, 'Hat', '8f30d92410f94318912b8a09f3ccdb9d', 'beret'),
	(96, 2, 'Backpack', '187f80da72a34138be98e0d47da9565f', 'ruggedpack'),
	(97, 2, 'Backpack', 'e15ec137bf23452a8ae045681581714d', 'dufflebag'),
	(98, 2, 'Vest', 'aedbc45bb0554e54867d871f703c2f1b', 'plate_carrier'),
	(99, 3, 'Backpack', '5288b670fc2449cfae945ba7692df45f', 'dufflebag'),
	(100, 3, 'Backpack', 'dc157420d3294c58b358d8d764d2d1a3', 'stowpack'),
	(101, 3, 'Hat', 'bd1e37ece7854683a0e157877b411eea', 'base'),
	(102, 3, 'Hat', 'f12b6c24f3634dfeb5a81f22f8ca8aca', 'medic'),
	(103, 3, 'Hat', '64b0d3b82df3461795a87639f205cab9', 'cap'),
	(104, 2, 'Hat', 'ad3953faea8f4fb5a16bc5df8c2196aa', 'goggles'),
	(105, 2, 'Hat', 'd69038a8854c440b9954649804d473c6', 'medic'),
	(106, 2, 'Hat', '673d555ef57e4ec9941cb227b59d6a7c', 'cap'),
	(107, 2, 'Hat', '05e3c4d7ecd34df19306f84cce5e39f3', 'base'),
	(108, 3, 'Vest', '53739b5b9ca2415b92471493a7b8e281', 'plate_carrier'),
	(109, 4, 'Vest', 'ff109681ff93457b84cdafb815ae7b79', 'plate_carrier'),
	(110, 4, 'Hat', '0de443ddb14e4ba099710078400a9783', 'goggles'),
	(111, 4, 'Hat', '55507c0c5f7e4e15abb4bd8380962df1', 'cap'),
	(112, 4, 'Hat', '508bb1a5a3654fae88f8f87705386471', 'medic'),
	(113, 4, 'Backpack', 'aee6a50b0d134ec4a2866f44a9769d50', 'ruggedpack'),
	(114, 5, 'Vest', '8054f85c70ac428699153ef5395d074d', 'plate_carrier'),
	(115, 5, 'Hat', 'b71473d6e2b44b62aa5aa0911a19c2b0', 'cap'),
	(116, 5, 'Hat', '2813e9e1f9f5430482313cbd09c338a4', 'goggles'),
	(117, 5, 'Hat', 'a28b9fce9d4949a3a0bb74f85a02617d', 'medic'),
	(118, 5, 'Backpack', 'b07cb55b781f453ba1c09edfefa36d09', 'molle'),
	(119, 5, 'Backpack', '9addb6f54920495cb4826cb5c000ed4b', 'ruggedpack'),
	(120, 5, 'Backpack', '006e53351ee74ebc8a228423ae26adf2', 'dufflebag'),
	(121, 6, 'Backpack', '170534fda21046a494c218902e41d410', 'pouches'),
	(122, 6, 'Vest', '6a7d24ff819e430282a06d8cd1de9288', 'plate_carrier'),
	(123, 6, 'Hat', '461ec3cc4c374e44950e5aa4754d8fef', 'base'),
	(124, 6, 'Hat', '428f65f08b4942a69bd56d065d95d6f6', 'medic'),
	(125, 6, 'Hat', '9b3a568e250e446e8a92a350a4f3b4bc', 'cap'),
	(126, 7, 'Vest', '35ea565ae6cd472baac8643b6285b61c', 'plate_carrier'),
	(127, 7, 'Hat', 'b26491c7869a47059f2406b45d327e49', 'cap'),
	(128, 7, 'Hat', '3b5515fb78f74b34bc67e52feb549e91', 'goggles'),
	(129, 7, 'Hat', 'c73147f37cea4ca89bc0d62288fd986e', 'medic'),
	(130, 7, 'Hat', 'b5aa25b5fc9d40eb9a0f639209d08855', 'band'),
	(131, 7, 'Backpack', '96a7b8b901d74697b39ce41260ebc1c7', 'dufflebag'),
	(132, 8, 'Vest', 'ff109681ff93457b84cdafb815ae7b79', 'plate_carrier'),
	(133, 8, 'Hat', '87e9589a0593497e808af86e12a1545e', 'beret'),
	(135, 9, 'Vest', '18a50a13426d4bc6895b7095e1dc4884', 'plate_carrier'),
	(136, 9, 'Hat', '2d969172b039473ab1bdbc7f5b962cf5', 'cap'),
	(137, 11, 'Vest', 'cc160021ea434a028eb4799859d12278', 'plate_carrier'),
	(138, 11, 'Hat', '4efb293de1e04ff4b12b500f02aa9dfd', 'cap'),
	(139, 12, 'Vest', '7a0997d76e8f4c15a28a3926712c76b2', 'plate_carrier'),
	(140, 12, 'Hat', '394adeb51d584e32960a4a58300cb1d9', 'goggles'),
	(141, 12, 'Hat', 'daf06b1d894f49e8a8352c9a9a0c1125', 'cap'),
	(142, 13, 'Vest', '06de9e65b58e440288becea0695e0692', 'plate_carrier'),
	(143, 13, 'Hat', '365054b2139e4b119209d809dcb5cfb6', 'goggles'),
	(144, 13, 'Hat', '2c02f688654948fdb58b7b303a9a4fc0', 'cap'),
	(145, 13, 'Backpack', 'f5e8c11df7d64812988d3c7335f931bb', 'ruggedpack'),
	(146, 13, 'Backpack', 'f8554a2a550b4e26a9a96ad647f4facd', 'dufflebag'),
	(147, 14, 'Vest', '412d79398a4e49519cc589ed85d751f8', 'plate_carrier'),
	(148, 14, 'Hat', 'c8ba6f5b90ca4fb69f5013b857159f87', 'cap'),
	(149, 15, 'Backpack', 'e6ac27f970f1459885d16cb3372b51e3', 'dufflebag'),
	(150, 15, 'Backpack', 'a934c1e6f1b9446286af28fa4f9a7bbb', 'stowpack'),
	(151, 2, 'MapTackFlag', '97f83e26b7f6401bab4662211bb53b89', NULL),
	(152, 3, 'MapTackFlag', '3fcfdadec454482184443e04a33d924a', NULL),
	(153, 4, 'MapTackFlag', '3dc2b9b3d06a40888444e1c9e113b0bc', NULL),
	(154, 5, 'MapTackFlag', '22f65a51411743639b459a68a2fda0da', NULL),
	(155, 6, 'MapTackFlag', '2363f540be904c4c8c213fa8d7f9b89b', NULL),
	(156, 13, 'MapTackFlag', 'd11fd1b5959c4ac8b2646d850d7f3981', NULL),
	(157, 13, 'RallyPoint', 'be13ead6faf842bbb336780e3b4cce4b', NULL);

INSERT INTO `lang_info` (`pk`, `Code`, `DisplayName`, `NativeName`, `DefaultCultureCode`, `HasTranslationSupport`, `RequiresIMGUI`, `FallbackTranslationLanguageCode`, `SteamLanguageName`, `SupportsPluralization`) VALUES
	(1, 'en-us', 'English', 'English', 'en-US', 1, 0, NULL, 'english', 1),
	(2, 'ru-ru', 'Russian', 'Русский язык', 'ru-RU', 1, 0, NULL, 'russian', 0),
	(3, 'es-es', 'Spanish', 'Español', 'es-ES', 1, 0, 'pt-br', 'spanish', 0),
	(4, 'de-de', 'German', 'Deutsch', 'de-DE', 0, 0, NULL, 'german', 0),
	(5, 'fr-fr', 'French', 'Français', 'fr-FR', 0, 0, NULL, 'french', 0),
	(6, 'pl-pl', 'Polish', 'Polski', 'pl-PL', 0, 0, NULL, 'polish', 0),
	(7, 'zh-cn', 'Chinese (Simplified)', '简体字', 'zh-Hans', 1, 0, 'zh-tw', 'schinese', 0),
	(8, 'zh-tw', 'Chinese (Traditional)', '繁體字', 'zh-Hant', 0, 0, 'zh-cn', 'tchinese', 0),
	(9, 'pt-br', 'Portuguese (Brazil)', 'Português', 'pt-BR', 1, 0, 'pt-pt', 'brazilian', 0),
	(10, 'pt-pt', 'Portuguese (Portugal)', 'Português', 'pt-PT', 0, 0, 'pt-br', 'portuguese', 0),
	(11, 'fl-ph', 'Filipino', 'Tagalog', 'fil-PH', 1, 0, NULL, NULL, 0),
	(12, 'nb-no', 'Norwegian Bokmål', 'Norsk Bokmål', 'nb-NO', 0, 0, 'nn-no', 'norwegian', 0),
	(13, 'nn-no', 'Norwegian Nynorsk', 'Norsk Nynorsk', 'nn-NO', 0, 0, 'nb-no', 'norwegian', 0),
	(14, 'nl-nl', 'Dutch', 'Nederlands', 'nl-NL', 0, 0, NULL, 'dutch', 0),
	(15, 'sv-se', 'Swedish', 'Svenska', 'sv-SE', 0, 0, NULL, 'swedish', 0),
	(16, 'ro-ro', 'Romanian', 'Română', 'ro-RO', 1, 0, NULL, 'romanian', 0),
	(18, 'ar-sa', 'Arabic', 'العربية', 'ar-SA', 1, 1, NULL, 'arabic', 0);

INSERT INTO `seasons` (`Id`, `ReleaseTimestampUTC`) VALUES
	(0, '2021-02-28 05:00:00'),
	(1, '2021-08-08 22:30:00'),
	(2, '2023-03-14 02:00:00'),
	(3, '2023-05-04 21:00:00'),
	(4, '2025-01-01 00:00:00');

INSERT INTO `maps` (`Id`, `DisplayName`, `WorkshopId`, `SeasonReleased`, `Team1Faction`, `Team2Faction`) VALUES
	(0, 'Fool\'s Road', 2407566267, 0, 2, 3),
	(1, 'Goose Bay', 2301006771, 1, 2, 3),
	(2, 'Nuijamaa', 2557112412, 1, 2, 3),
	(3, 'Gulf of Aqaba', 2726964335, 2, 2, 4),
	(4, 'Changbai Shan', 2943688379, 3, 5, 6),
	(5, 'Yellowknife', 0, 4, 13, 3);

INSERT INTO `maps_dependencies` (`WorkshopId`, `Map`, `IsRemoved`) VALUES
	(2407566267, 0, 1),
	(2407740920, 0, 0),
	(2407740920, 1, 0),
	(2407740920, 2, 0),
	(2407740920, 3, 0),
	(2407740920, 4, 0);

SET SESSION sql_mode='';
```

## Finishing Up

You should now have a working server. It will boot to a seeding layout (one within the `Layouts/Seeding` folder) and enter seeding mode, waiting for new players. You can disable this in the System Config, but you need to create a normal gamemode for it to use and build it into the Plugins folder. You can use the FreeTDM project as a template.

See [layouts](./layouts.md) for more information.