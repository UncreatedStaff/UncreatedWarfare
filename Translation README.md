# Files
## Enums (Folder)
Enums (enumerations) are a list of text values that correspond represent an option. For example, you could have an enum for direction:
```cs
enum Direction
{
    North,
    South,
    East,
    West
}
```
Some enums have translations, all of which are in the Enums folder. The **%NAME%** option is the name of the enum. In English, the name of this one would be "Direction". Each value also has an option.
<br>See **JSON** section below.

## translations.properties
Primary translations file. Contains the main translations used for chat messages, UI, etc.
<br><br>See **Properties** and **Rich Text** sections below.
### Extra Notation
**Arguments**

Zero based, surrounded in curly brackets (`{}`).
<br> Example (Translation<`int`, `ItemAsset`>): `Given you {0}x {1}.`
<br>  -> `Given you 4x M4A1.`

## factions.properties
Contains the faction translations, including names, short names, and abbreviations.
<br>See **Properties** section below.

## kits.properties
Contains the kit sign text translations.
<br>\<br> is used as a line break for sign texts, causing the name to go on two lines.
<br>See **Properties** section below.

## traits.properties
Contains the trait sign text and description translations.
<br>See **Properties** section below.

## deaths.json
Stores all possible death messages for each set of available data/flags.
<br>There is a comment at the beginning of the file explaining the flags.
<br>See **JSON** section below.

## readme.md
This file.

# Properties
Use IDE Format: **Java Properties** available.
<br>Anything starting with a **!** or a **#** is a comment, and ignored by the file reader.
```properties
# Comment
! Comment
Key: Value
```
Keys should be left as is, and values should be translated.

### Example
```properties
# Description: Sent when a player tries to abandon a damaged vehicle.            (Don't translate)
# Formatting Arguments:                                                          (Don't translate)
#  {0} - [InteractableVehicle]                                                   (Don't translate)
# Default Value: Your {0} is damaged, repair it before returning it to the yard. (Don't translate)
AbandonDamaged: Al tau {0} este deteriorat, reparal inainte sa il returnezi.   # (Translate after the ':' symbol)
```

# JSON
Use IDE Format: **JSONC**, or **JSON** if JSONC is not available.
<br>Anything starting with a **//** or between **/\*** and **\*/** is a comment, and ignored by the file reader.
```jsonc
/*
    Multi-line
    comment
*/
{
    /* Comment */
    "key1": "value1",
    // Comment
    "key2": "value2",
}
```
Keys should be left as is, and values should be translated.<br>
For deaths, the `death-cause`, `custom-key`, `item-cause`, and `vehicle-cause` key/value pairs should not be translated.

# Rich Text
We use rich text to format our translations. The most common you'll see is \<color> tags, but there are others (\<b>, \<i>, \<sub>).
<br>These should not be removed.

Example:
```properties
# Description: Sent when a player tries to abandon a damaged vehicle.
# Formatting Arguments:
#  {0} - [InteractableVehicle]
# Default Value: <#ff8c69>Your <#cedcde>{0}</color> is damaged, repair it before returning it to the yard.
AbandonDamaged: <#ff8c69>Al tau <#cedcde>{0}</color> este deteriorat, reparal inainte sa il returnezi.
```
Notice how the default value was translated but the rich text tags were left.
<br>More info about rich text here: [TMPro Documentation](http://digitalnativestudios.com/textmeshpro/docs/rich-text/).

# Formatting Arguments
Also notice the `{n}` formatting placeholders. These are replaced by translation arguments, which are sometimes explained in the comments above.
```properties
#  {n} - [Type] (Formatting) Description
```

# Notes
Please leave in-game terms such as **FOB**, **Rally**, **Build**, **Ammo**, and other item names in English.

# Examples
`Enum/SDG.Unturned.ELimb.json`
```json
{
  "%NAME%": "Membru",
  "LEFT_FOOT": "Left Glezna",
  "LEFT_LEG": "Left Picior",
  "RIGHT_FOOT": "Dreapta Glezna",
  "RIGHT_LEG": "Dreapta Picior",
  "LEFT_HAND": "Left Mana",
  "LEFT_ARM": "Left Brat",
  "RIGHT_HAND": "Dreapta Mana",
  "RIGHT_ARM": "Dreapta Brat",
  "LEFT_BACK": "Stanga Spate",
  "RIGHT_BACK": "Dreapta Spate",
  "LEFT_FRONT": "Stanga Fata",
  "RIGHT_FRONT": "Dreapta Fata",
  "SPINE": "Spinare",
  "SKULL": "Craniu"
}
```
`deaths.json`
```jsonc
[
  // ...
  {
    "death-cause": "charge",
    "translations": {
      "None": "{0} a explodat de la un explozibil.",
      "Item": "{0} a explodat de la un {3}.",
      "Item, Killer": "{1} la explodat pe {0} cu un {3}.",
      "Killer": "{1} a explodat pe {0} cu un.",
      "Suicide": "{0} sa explodat singur.",
      "Item, Suicide": "{0} sa explodat singur cu un {3}."
    }
  },
  // ...
]
```
`translations.properties`
```properties
# Formatting Arguments:
#  {0} - [Text]
# Default Value: <#a1998d><#dbb67f>{0}</color> needs FOB supplies.
SuppliesBuildToast: <#a1998d><#dbb67f>{0}</color> are nevoie de provizii la FOB.
```
`factions.properties`
```properties
# Germany (ID: germany, #5)
#  Name:         Germany
#  Short Name:   Germany
#  Abbreviation: DE
#  Flag:         https://i.imgur.com/91Apxc5.png
# Default: Germany
Name: 德意志联邦共和国
# Default: Germany
ShortName: 德意志国
# Default: DE
Abbreviation: 德国
```

# Advanced Users (translations):

**Formatting**

***Default Formatting (T Constants)***
```
• FormatPlural              "$plural$"  See Below
• FormatUppercase           "upper"     Turns the argument UPPERCASE.
• FormatLowercase           "lower"     Turns the argument lowercase.
• FormatPropercase          "proper"    Turns the argument ProperCase.
• FormatRarityColor         "rarity"    Colors assets to their rarity color.
• FormatTimeLong            "tlong"     Turns time to: 3 minutes and 4 seconds, etc.
• FormatTimeShort_MM_SS     "tshort1"   Turns time to: 03:04, etc.
• FormatTimeShort_HH_MM_SS  "tshort2"   Turns time to: 01:03:04, etc.
   Time can be int, uint, float (all in seconds), or TimeSpan
```
Other formats are stored in the most prominent class of the interface (`WarfarePlayer` for `IPlayer`, `Fob` for `IDeployable`, etc.)
<br>Anything that would work in `T[N].ToString(string, IFormatProvider)` will work here.

<br>**Conditional pluralization of existing terms**

`${p:arg:text}`  will replace text with the plural version of text if `{arg}` is not one.
`${p:arg:text!}` will replace text with the plural version of text if `{arg}` is one.
 Example: `There ${p:0:is} {0} ${p:0:apple}, ${p:0:it} ${p:0:is} ${p:0:a }${p:0:fruit}. ${p:0:It} ${p:0:taste!} good.`
  -> ({0} = 1) `There is 1 apple, it is a fruit. It tastes good.`
  -> ({0} = 3) `There are 3 apples, they are fruits. They taste good.`

<br>**Conditional pluralization of argument values**

Using the format: `'xxxx' + FormatPlural` will replace the value for that argument with the plural version.
<br> Example: `You cant place {0} here.` arg0Fmt: `RarityFormat + FormatPlural`
<br>  -> `You can't place <#xxxxx>FOB Radios</color> here.`
<br>
<br>Using the format: `'xxxx' + FormatPlural + '{arg}'` will replace the value for that argument with the plural version if `{arg}` is not one.
<br> Example: `There are {0} {1} already on this FOB.` arg1Fmt: `RarityFormat + FormatPlural + {0}`
<br>  -> (4, FOB Radio Asset) `There are 4 <#xxxxx>FOB Radios</color> already on this FOB.`
