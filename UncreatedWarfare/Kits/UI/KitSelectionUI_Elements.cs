using DanielWillett.ReflectionTools;
using Uncreated.Framework.UI;
using Uncreated.Framework.UI.Patterns;
using Uncreated.Framework.UI.Presets;
using Uncreated.Warfare.Util;
// ReSharper disable ClassNeverInstantiated.Local
// ReSharper disable UnusedAutoPropertyAccessor.Local

namespace Uncreated.Warfare.Kits.UI;

partial class KitSelectionUI
{
    // Filter Pane

    private readonly PlaceholderTextBox _kitNameFilter = new PlaceholderTextBox("Filters/Viewport/Content/Kit_Search", "./Viewport/Placeholder");
    private readonly UnturnedLabel _kitNameFilterSearchLabel = new UnturnedLabel("Filters/Viewport/Content/Search/Label");

    private readonly UnturnedLabel _classFilterLabel = new UnturnedLabel("Filters/Viewport/Content/Label_Classes");

    private readonly UnturnedButton[] _classButtons = ElementPatterns.CreateArray<UnturnedButton>(
        i => new UnturnedButton($"Filters/Viewport/Content/Classes_Grid/Kits_Class_{EnumUtility.GetName((Class)i)}"),
        (int)Class.Squadleader,
        to: (int)Class.SpecOps
    );

    private readonly UnturnedLabel _favoritesLabel = new UnturnedLabel("Filters/Viewport/Content/Label_Favorites");
    
    private readonly FavoriteKitInfo[] _favoriteKits = ElementPatterns.CreateArray<FavoriteKitInfo>("Filters/Viewport/Content/Kits_Favorite_Kit_{0}", 1, to: 15);

    private readonly UnturnedUIElement _switchToListLogic = new UnturnedUIElement("~/Logic_SwitchToList");
    private readonly UnturnedUIElement _switchToPanelLogic = new UnturnedUIElement("~/Logic_SwitchToPanel");
    private readonly UnturnedUIElement _startCloseAnimationLogic = new UnturnedUIElement("~/Logic_Close");

    // Public Kits

    private readonly LabeledButton _switchBackToPanel = new LabeledButton("Kits_Back_To_Panel", "./Label");
    private readonly LabeledButton _close = new LabeledButton("Kits_Close", "./Label");
    private readonly UnturnedLabel _publicKitsTitle = new UnturnedLabel("Public_Kit_Layout/Title_Public_Kits/Label");
    private readonly KitPanel[] _panels = ElementPatterns.CreateArray<KitPanel>(
        "Public_Kit_Layout/Viewport/Content/Kit_Panel_{0}", (int)Class.Squadleader, to: (int)Class.SpecOps
    );

    // Kit Search

    private readonly UnturnedLabel _searchResultsTitle = new UnturnedLabel("Kit_Info/List/Title_Searched_Kits/Label");

    private readonly UnturnedUIElement _listNoResult = new UnturnedUIElement("Kit_Info/List/Viewport/Content/Kit_NoResults");
    private readonly UnturnedLabel _listNoResultLabel = new UnturnedLabel("Kit_Info/List/Viewport/Content/Kit_NoResults/BoxText");

    private readonly LabeledStateButton _listPreviousPage = new LabeledStateButton("Kit_Info/List/Pages/Kits_Info_Page_Previous", "./Label", "./ButtonState");
    private readonly LabeledStateButton _listNextPage = new LabeledStateButton("Kit_Info/List/Pages/Kits_Info_Page_Next", "./Label", "./ButtonState");
    private readonly StatePlaceholderTextBox _listPage = new StatePlaceholderTextBox("Kit_Info/List/Pages/Kits_Info_Page", "./Viewport/Placeholder", "./InputFieldState");

    private readonly ListKitInfo[] _listResults = ElementPatterns.CreateArray<ListKitInfo>("Kit_Info/List/Viewport/Content/Kit_{0}", 1, to: 10);


#nullable disable

    private class KitPanel : PatternRoot
    {
        [Pattern("Title", AdditionalPath = "Viewport/Content")]
        public UnturnedLabel Title { get; set; }

        [Pattern("Desc", AdditionalPath = "Viewport/Content")]
        public UnturnedLabel Description { get; set; }

        [Pattern("Kit_{0}", AdditionalPath = "Viewport/Content")]
        [ArrayPattern(1, To = 3)]
        public PanelKitInfo[] Kits { get; set; }
    }

    private class PanelKitInfo : KitInfo
    {
        [Pattern("Kit_Panel_{1}_Kit_{0}_Favorite", AdditionalPath = "Buttons/Favorite")]
        public override UnturnedButton FavoriteButton { get; set; }

        [Pattern("Kit_Panel_{1}_Kit_{0}_Unfavorite", AdditionalPath = "Buttons/Unfavorite")]
        public override UnturnedButton UnfavoriteButton { get; set; }

        [Pattern("Kit_Panel_{1}_Kit_{0}_Request", AdditionalPath = "Buttons/Request")]
        public override UnturnedButton RequestButton { get; set; }

        [Pattern("Kit_Panel_{1}_Kit_{0}_Preview", AdditionalPath = "Buttons/Preview")]
        public override UnturnedButton PreviewButton { get; set; }

        [Pattern("Kit_Panel_{1}_Kit_{0}_Purchase", AdditionalPath = "Unlock", PresetPaths = [ "./Label" ])]
        public override LabeledButton UnlockButton { get; set; }
    }

    private class ListKitInfo : KitInfo
    {
        [Pattern("Kit_List_{0}_Favorite", AdditionalPath = "Buttons/Favorite")]
        public override UnturnedButton FavoriteButton { get; set; }

        [Pattern("Kit_List_{0}_Unfavorite", AdditionalPath = "Buttons/Unfavorite")]
        public override UnturnedButton UnfavoriteButton { get; set; }

        [Pattern("Kit_List_{0}_Request", AdditionalPath = "Buttons/Request")]
        public override UnturnedButton RequestButton { get; set; }

        [Pattern("Kit_List_{0}_Preview", AdditionalPath = "Buttons/Preview")]
        public override UnturnedButton PreviewButton { get; set; }

        [Pattern("Kit_List_{0}_Purchase", AdditionalPath = "Unlock", PresetPaths = [ "./Label" ])]
        public override LabeledButton UnlockButton { get; set; }
    }

    private abstract class KitInfo : PatternRoot
    {
        [Pattern("Flag")]
        public UnturnedLabel Flag { get; set; }

        [Pattern("Class")]
        public UnturnedLabel Class { get; set; }

        [Pattern("Name")]
        public UnturnedLabel Name { get; set; }

        // was going to do playtime but i think ID is more useful here
        [Pattern("Playtime")]
        public UnturnedLabel Id { get; set; }

        [Ignore] public abstract UnturnedButton FavoriteButton { get; set; }
        [Ignore] public abstract UnturnedButton UnfavoriteButton { get; set; }
        [Ignore] public abstract UnturnedButton RequestButton { get; set; }
        [Ignore] public abstract UnturnedButton PreviewButton { get; set; }

        [Pattern("Favorite", AdditionalPath = "Buttons")]
        public UnturnedUIElement FavoriteButtonParent { get; set; }

        [Pattern("Unfavorite", AdditionalPath = "Buttons")]
        public UnturnedUIElement UnfavoriteButtonParent { get; set; }

        [Pattern("Request", AdditionalPath = "Buttons")]
        public UnturnedUIElement RequestButtonParent { get; set; }

        [Pattern("Preview", AdditionalPath = "Buttons")]
        public UnturnedUIElement PreviewButtonParent { get; set; }

        [Pattern("Include_{0}")]
        [ArrayPattern(1, To = 20)]
        public CountIncludeLabel[] IncludeLabels { get; set; }

        [Pattern("Include_1_{0}")]
        [ArrayPattern(1, To = 5)]
        public IncludeLabel[] PrimaryAttachments { get; set; }

        [Pattern("Include_2_{0}")]
        [ArrayPattern(1, To = 5)]
        public IncludeLabel[] SecondaryAttachments { get; set; }

        [Pattern("Include_3_{0}")]
        [ArrayPattern(1, To = 5)]
        public IncludeLabel[] TertiaryAttachments { get; set; }

        [Pattern("Status")]
        public UnturnedLabel StatusLabel { get; set; }

        [Pattern("Unlock")]
        public UnturnedUIElement UnlockSection { get; set; }

        [Ignore] public abstract LabeledButton UnlockButton { get; set; }
    }

    private class IncludeLabel : PatternRoot
    {
        [Pattern("Icon")]
        public UnturnedLabel Icon { get; set; }

        [Pattern("Name")]
        public UnturnedLabel Name { get; set; }
    }

    private class CountIncludeLabel : IncludeLabel
    {
        [Pattern("Count")]
        public UnturnedLabel Count { get; set; }
    }

    private class FavoriteKitInfo : PatternButtonRoot
    {
        [Pattern("Flag")]
        public UnturnedLabel Flag { get; set; }

        [Pattern("Class")]
        public UnturnedLabel Class { get; set; }

        [Pattern("Name")]
        public UnturnedLabel Name { get; set; }

        [Pattern("Id")]
        public UnturnedLabel Id { get; set; }

        [Pattern("Kits_Favorite_Unfavorite_{0}")]
        public UnturnedButton UnfavoriteButton { get; set; }

        [Pattern("Kits_Favorite_QuickRequest_{0}")]
        public UnturnedButton RequestButton { get; set; }
    }

#nullable restore
}
