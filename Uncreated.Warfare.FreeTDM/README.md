# Free Team Deathmatch

Free TDM is the Team-Deathmatch mode used in player seeding.

Unlike every other gamemode, this gamemode is public and is
included in this repository to serve as an example for anyone that wants
to make a custom gamemode. Obviously it doesn't show some important stuff
like flags but it should be a good starting point.

## Configuration
Layouts are defined using YAML files. These files are used to configure what services
are loaded for your layout.

Components can also use this to get custom layout-specific config values.

### Local Strings
Some properties are defined as 'local strings' which means they can either just be a string or can implement a language table:
```yml
Name: "Hello"
# or 
Name:
  en-us: "Hello"
  es-es: "Hola"
```

### Example layout

The following YAML snippits are all part of the same file

ex: `Layouts/FTDM/Free TDM Infantry 1.yml`

```yml
Type: "Layout"
Name: "<Layout Unique Internal Name, ex: Free TDM Infantry I>"
GamemodeName: "Free Team Deathmatch"
LayoutName: "<Layout sub-name, ex: Infantry I>"
Map: "<Map Name, ex: Yellowknife>"
```

The 'Services' property is used to list implementations of `ILayoutServiceConfigurer` that will be invoked when the layout is being loaded.

```yml
Services:
  - "Uncreated.Warfare.FreeTeamDeathmatch.LayoutServiceConfigurer, Uncreated.Warfare.FreeTDM"
```

The 'Teams' property is used to configure properties about teams. The ManagerType must implement `ITeamManager<>` and can use custom team object types for more configuration options. `TwoSidedTeamManager` is the default for opfor vs. blufor games.

'Role' can be one of `TwoSidedTeamRole` enum values: None, Random, Blufor, Opfor

'Faction' is used to assign a faction to the team. A faction starting in 'Map' is used to reference a faction configured for the map in MySQL (maps table). Otherwise, faction should be a Faction internal name (such as 'caf').

Gamemodes can add custom configuration options if needed.

```yml
Teams:
  ManagerType: TwoSidedTeamManager
  Teams:
    - Role: Random
      Faction: Map 1

    - Role: Random
      Faction: Map 2
```

The 'Components' property is used to create a configurable list of singleton services within the layout's scope.
They will be registered as their type and all implemented interfaces.

Each type should have a 'Type' property along with any other properties which can be injected as an IConfiguration object.
The properties will also be binded to the object on startup.

The `VehicleSpawnerLayoutConfigurer` component can be added to configure which vehicle spawners are disabled and their delays. It can also just define an 'Import' property that links to another file with the 'EnabledVehicleSpawners' property.

```yml
Components:
  - Type: "Uncreated.Warfare.Layouts.UI.Leaderboards.DualSidedLeaderboardUI, Uncreated.Warfare"
  - Type: VehicleSpawnerLayoutConfigurer
    EnabledVehicleSpawners:
      - SpawnerName: caf_logi_1
      - SpawnerName: caf_logi_2
        Delay:
          Timer: 5m
```
Phases are frames of time the game is split into. The majority of gamemodes will be configured to use the ActionPhase to wait until some condition is reached, such as all flags being owned.

Must define at least one phase.

Common phases:
  - PreparationPhase
  - ActionPhase
  - WinnerPopupPhase
  - LeaderboardPhase

*See https://github.com/UncreatedStaff/UncreatedWarfare/tree/master/UncreatedWarfare/Layouts/Phases for more info*

You can also create your own phases that implement `ILayoutPhase` or inherit `BasePhase<>`. All properties in the phase's object will be binded to the created `ILayoutPhase` on layout initialization.

```yml
Phases:
  - Type: PreparationPhase
    Duration: 2m
    Name:
      en_us: Preparation Phase
    Teams:
        # blufor can leave main but opfor can't 
      - Team: blufor
        Grounded: false
      - Team: opfor
        Grounded: true

  - Type: ActionPhase
    Name:
      en_us: Action Phase

  - Type: WinnerPopupPhase
    Duration: 10s
    Invincible: true

  - Type: LeaderboardPhase
    Duration: 30s
    PlayerStatsPath: "../../Leaderboard/AAS Stats.yml"
    Invincible: true
```

Most phases can define the following properties:
| Property | Type | Description | Default |
| -- | -- | -- | -- |
| Duration | time span | Time before the phase automatically ends. | Infinite |
| Invincible | boolean | Whether or not all players are invincible. | false |
| Name | local string | Display name of the phase (shown in popup). | null |
| Teams | PhaseTeamSettings[] | List of team-specific configuration options for this phase. | [ ] |

\
*PhaseTeamSettings*
| Property | Type | Description | Default |
| -- | -- | -- | -- |
| Team | string | The team to apply these options to. Either faction internal ID ('caf'), 'blufor', 'opfor', or a team index | required |
| Grounded | boolean | Whether or not team members can leave their HQ. | false |
| Invincible | boolean | Whether or not team members are invincible. | false |
| Name | local string | Override for the display name of the phase for team members. | null |

\
\
LeaderboardPhase has some specific properties for the leaderboard UI and statistics functionality
| Property | Type | Description | Default |
| -- | -- | -- | -- |
| PlayerStatsPath | relative path | Path to a file containing the other properties. | null |
| Stats | LeaderboardPhaseStatInfo[] | List of player stats available to use for MVP or leaderboard stats. | [ ] |
| ValuablePlayers | ValuablePlayerInfo[] | List of available MVP roles. | [ ] |

\
*LeaderboardPhaseStatInfo*
| Property | Type | Description | Default |
| -- | -- | -- | -- |
| Name | string | Unique internal name of this stat. | required |
| NumberFormat | string | [.NET Format Specifier](https://learn.microsoft.com/en-us/dotnet/standard/base-types/standard-numeric-format-strings) used to format the number on the leaderboard. | 0.## |
| DisplayName | local string | Name used to display this stat in various UIs. | @Name |
| FormulaName | string | Overrides the name used to refer to this stat in expressions. | @Name |
| IsLeaderboardColumn | boolean | Whether or not this stat is included in the player stats on the leaderboard. | false |
| DisablePointsUIDisplay | boolean | Whether or not this stat is excluded from the stat cylcer on the points UI. | false |
| ColumnHeader | string | Header for the column in the leaderboard. Should only be 1-3 letters. | false |
| IsGlobalStat | boolean | Whether or not this stat is included in the global stats on the leaderboard. | false |
| DefaultLeaderboardSort | Ascending \| Descending | The default sort mode for this column on the leaderboard. | Ascending |
| Expression | math expression | Expression used to calculate this statistic from other statistics. | null |

\
*ValuablePlayerInfo*
| Property | Type | Description | Default |
| -- | -- | -- | -- |
| Name | string | Unique internal name of this role. | required |
| DisplayName | local string | Name used to display the role in the leaderboard. | @Name |
| Format | local string | [.NET string.Format](https://learn.microsoft.com/en-us/dotnet/fundamentals/runtime-libraries/system-string-format#get-started-with-the-stringformat-method) template used to format the value into the description. | required |
| Type | ValuablePlayerInfoType | Aggregation mode: HighestStatistic, LowestStatistic, or Custom. | required |
| Statistic | string | Unique name of the statistic to use for HighestStatistic and LowestStatistic modes. | required unless Type = Custom |
| Provider | type | Type implementing IValuablePlayerProvider to use with Custom type. | null |
| MinimumDistance | string | When using LongestShotValuablePlayerProvider, the minimum distance for a shot to be considered. | 0 |
| Players | ulong[] | When using SpecificPlayerValuablePlayerProvider, list of players who can get the role. | false |
| RandomValueMinimum | double | When using SpecificPlayerValuablePlayerProvider, min for a random value formatted into @Format. | 3 |
| RandomValueMaximum | double | When using SpecificPlayerValuablePlayerProvider, max for a random value formatted into @For

Custom stats can be added by using:
```cs
player.ComponentOrNull<PlayerGameStatsComponent>()?.AddToStat([Name], 1)
```
or for offline players:
```cs
LeaderboardPhase phase = /* etc */;
phase.AddToOfflineStat(phase.GetStatIndex(killStat), 1d, player, team);
```
If the stat isn't registered then the method silently fails