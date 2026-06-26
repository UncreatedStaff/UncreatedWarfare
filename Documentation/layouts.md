# Layouts

Layout files are [YAML](https://yaml.org/) files within the `Layouts` folder of the Warfare configuration folder.

> [!NOTE]
> Seeding layouts are located within the `Layouts/Seeding` folder, and are treated differently than other files.

## The Layout File
```yml
# The C# type to use as the layout. Usually will just be 'Layout'.
Type: Layout

# Shorter display name of the gamemode and layout
Name: AAS Air Assault 1

# Image used as the background in the voting UI. Should be 200x100px if using the official UI mod.
Image: https://i.imgur.com/Ur8J86b.png

# Display name of the gamemode
GamemodeName: Advance and Secure

# Display name of the layout
LayoutName: Air Assault I

# Map this layout is for
Map: Yellowknife
# -OR-
Maps: [ "Yellowknife", "Gulf of Aqaba" ]

# Optional chance this layout will be picked relative to other layouts (defaults to 1.0)
Weight: 0.5

# List of ILayoutServiceConfigurer classes to invoke.
# used to add services to the scoped service provider for this gamemode.
Services:
  - "Uncreated.Warfare.AdvanceAndSecure.LayoutServiceConfigurer, Uncreated.Warfare.AdvanceAndSecure"

# Team configuration
Teams:
  # Type implementing ITeamManager<>, for two-teamed gamemodes TwoSidedTeamManager will usually suffice.
  ManagerType: TwoSidedTeamManager
  # Config specific to TwoSidedTeamManager
  Teams:
      # What side this team will be on. Random picks either Blufor (defense) or Opfor (attack)
      # None, Random, Blufor, Opfor
    - Role: None
      # The faction to use for the team.
      # Map 1 or 2 choses that team configured for the map in the database.
      Faction: Map 1

    - Role: None
      Faction: Map 2

# List of services to configure.  
Components:
  - Type: "Uncreated.Warfare.AdvanceAndSecure.AASFlagService, Uncreated.Warfare.AdvanceAndSecure"
    # Includes multiple files, one of which will be randomly chosen to replace the config for this component
    IncludedVariations: "../FlagVariations/*.yml"

  - Type: "VehicleSpawnerLayoutConfigurer"
    Import: "../VehicleConfigurations/Yellowknife Air Assault.yml"

  - Type: "Uncreated.Warfare.Layouts.UI.Leaderboards.DualSidedLeaderboardUI, Uncreated.Warfare"

# List of game phases in their order of execution.
# Most phases should inherit BasePhase<PhaseTeamSettings>.
Phases:
    # The C# type of the phase. Not a service. PreparationPhase is usually the first one.
  - Type: PreparationPhase
    # How long the phase lasts (not supported by all phases)
    Duration: 2m
    # This is what shows on the popup when the phase starts.
    # Names can optionally be localized.
    # Name:
    #   en_us: Preparation Phase
    Name: Preparation Phase
    # Whether or not all players are invincible during this phase
    Invincible: false
    # Most phases support team-specific configuration.
    Teams:
        # Can be a faction ID, 'blufor'/'opfor', or a team index (0, 1, ...).
        # Uses ITeamManager<>.FindTeam.
      - Team: 1 # blufor, usa
        # Not allowed to leave main
        Grounded: true
        # Unable to take damage
        Invincible: false
        # Overrides the name of the phase for this team
        Name: Preparation Phase
        # Extra properties can be used through IConfiguration for custom gamemodes

      - Team: 2
        Grounded: true
    # Custom phases can pull extra information through IConfiguration or
    # by creating a property for the config to bind to on the phase.

    # ActionPhase is a mostly empty phase used to allow components to do their thing,
    # but could be replaced by a phase with more logic in it
  - Type: ActionPhase
    Name:
      en_us: Action Phase

    # Phase showing the winner.
  - Type: WinnerPopupPhase
    Duration: 10s
    Invincible: true

    # Phase showing the leaderboard.
  - Type: LeaderboardPhase
    Duration: 30s
    # Imports stats for the gamemodes. See below.
    PlayerStatsPath: "../../Leaderboard/AAS Stats.yml"
    Invincible: true
```

For more information about the leaderboard configuration, see [stats](./stats.md).

### Variations
Variations are randomized overrides to layout files. They can be applied to phases, components, and the base layout file. Variations are included using the `IncludedVariations` and `ExcludedVariations` properties. Both properties use [glob patterns](https://learn.microsoft.com/en-us/dotnet/core/extensions/file-globbing#pattern-formats).

```yml
Type: Layout

# ... Excluded for brevity ...

# Includes all YAML files in 'AAS_Variations' (relative to the current layout file).
IncludedVariations: "FTDM Variations/*.yml"
# Excludes NotAVariation from the previous line's inclusion.
ExcludedVariations: "FTDM Variations/NotAVariation.yml"

Components:
  - Type: "Uncreated.Warfare.FreeTeamDeathmatch.FtdmService, Uncreated.Warfare.FreeTDM"
    # Include all .yml files in './FtdmService Variations/'
    IncludedVariations: "FtdmService Variations/*.yml"
    AllowReenterSpawn: true

Phases:
  - Type: PreparationPhase
    Name: Preparation Phase
    Duration: 2m
    # Include all .yml files in './Prep Phase Variations/'
    IncludedVariations: "Prep Phase Variations/*.yml"

  - Type: ActionPhase
    Name: Action Phase
```


#### A Variation File
```yml
# Map this variation is for
Map: Yellowknife
# -OR-
Maps: [ "Yellowknife", "Gulf of Aqaba" ]

# Optional chance this variation will be picked relative to other variations (defaults to 1.0)
Weight: 0.25

# ... Other properties relating to whatever it's a variation of ... #
```

For example, picture a layout component in a layout file at `Layouts/FTDM/FTDM.yml`.

```yml
Components:
  - Type: "Uncreated.Warfare.FreeTeamDeathmatch.FtdmService, Uncreated.Warfare.FreeTDM"
    IncludedVariations: "FtdmService Variations/*.yml"
    AllowReenterSpawn: true
```

It is including all .yml files in the `FtdmService Variations` folder.

So in `Layouts/FTDM/FtdmService Variations/` we could have the following files:
* Sniper kits (30% of the time)
    ```yml
    Map: Gulf of Aqaba
    Weight: 0.3
    Kits:
      usa: [ usmar1, ussni1 ]
      mec: [ memar1, mesni1 ]
    ```
* Close-quarters kits (65% of the time)
    ```yml
    Map: Gulf of Aqaba
    Weight: 0.65
    Kits:
      usa: [ usrif1, usmed1, usbre1, usar1 ]
      mec: [ merif1, memed1, mebre1, mear1 ]
    ```
* Fun kits (5% of the time)
    ```yml
    Map: Gulf of Aqaba
    Weight: 0.05
    Kits:
      usa: [ uslat1, ushat1 ]
      mec: [ melat1, mehat1 ]
    AllowReenterSpawn: false # this one also overrides the property in the base file
    ```

And if the sniper variation was chosen, the resulting component would be read like this:
```yml
Components:
  - Type: "Uncreated.Warfare.FreeTeamDeathmatch.FtdmService, Uncreated.Warfare.FreeTDM"
    Kits:
      usa: [ usmar1, ussni1 ]
      mec: [ memar1, mesni1 ]
    AllowReenterSpawn: true
```
but if the 'fun kits' variation was chosen, it'd look like this:
```yml
Components:
  - Type: "Uncreated.Warfare.FreeTeamDeathmatch.FtdmService, Uncreated.Warfare.FreeTDM"
    Kits:
      usa: [ uslat1, ushat1 ]
      mec: [ melat1, mehat1 ]
    AllowReenterSpawn: false
```

---

> [!WARNING]
> Overriding arrays doesn't work as you'd expect.

For example, if this file:
```yml
Kits: [ usrif1, usmed1, usbre1, usar1 ]
```
were to be overridden by this file:
```yml
Kits: [ ussni1, usmar1 ]
```
the resulting file would be:
```yml
Kits: [ ussni1, usmar1, usbre1, usar1 ]
```

Read [this article](https://alexey-gnetko.com/posts/configuration-overrides-in-net/) by Alexey Gnetko for a bit more information.