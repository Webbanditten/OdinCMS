using System.Collections.Generic;

namespace KeplerCMS.Models
{
    /// <summary>
    /// A catalogue page for the housekeeping editor, with the fields Odin sends the client (CATALOGUE_PAGE):
    /// i = NameIndex, n = Name, l = Layout, g = Headline, e = Teasers, h = Body, w = LabelPick, s = LabelExtraS,
    /// t1..t11 = Texts, u = Links.
    /// </summary>
    public class CataloguePageEdit
    {
        public int Id { get; set; }
        public int Order { get; set; }
        public string NameIndex { get; set; } = "";
        public string Name { get; set; } = "";
        public string Layout { get; set; } = "ctlg_layout2";
        public string Headline { get; set; } = "";
        public List<string> Teasers { get; set; } = new();
        public string Body { get; set; } = "";
        public string LabelPick { get; set; } = "";
        /// <summary>The special text badge, "type:text" (type 1 or 2 picks the background).</summary>
        public string LabelExtraS { get; set; } = "";
        /// <summary>label_extra_t: text slots by number (ctlg_text_1 ... in the layout).</summary>
        public Dictionary<int, string> Texts { get; set; } = new();
        public List<string> Links { get; set; } = new();
        public bool IndexVisible { get; set; } = true;
        public bool ClubOnly { get; set; }
        public string Fuse { get; set; } = "DEFAULT";
        public int ProductCount { get; set; }
    }

    /// <summary>A catalogue item (product) as the editor shows and saves it.</summary>
    public class CatalogueProductEdit
    {
        public int Id { get; set; }
        public List<int> PageIds { get; set; } = new();
        public int Order { get; set; }
        public int Price { get; set; } = 3;
        public int Amount { get; set; } = 1;
        public bool Hidden { get; set; }
        public string SaleCode { get; set; } = "";
        public int DefinitionId { get; set; }
        public int SpecialSpriteId { get; set; }
        public bool IsPackage { get; set; }
        public string PackageName { get; set; } = "";
        public string PackageDescription { get; set; } = "";
        /// <summary>catalogue_items.name/description: used for packages and special-sprite items, like Odin does.</summary>
        public string OwnName { get; set; } = "";
        public string OwnDescription { get; set; } = "";
        /// <summary>What the client shows (Odin's rule: packages and special sprites use their own texts).</summary>
        public string Name { get; set; } = "";
        public string Description { get; set; } = "";
        public CatalogueFurni Definition { get; set; }
        public List<CataloguePackageEdit> Packages { get; set; } = new();
    }

    public class CataloguePackageEdit
    {
        public int DefinitionId { get; set; }
        public int Amount { get; set; } = 1;
        public int SpecialSpriteId { get; set; }
        public CatalogueFurni Definition { get; set; }
    }

    /// <summary>Furni master data (items_definitions).</summary>
    public class CatalogueFurni
    {
        public int Id { get; set; }
        public string Sprite { get; set; } = "";
        public int SpriteId { get; set; }
        public string Name { get; set; } = "";
        public string Description { get; set; } = "";
        public string Colour { get; set; } = "0,0,0";
        public int Length { get; set; } = 1;
        public int Width { get; set; } = 1;
        public double TopHeight { get; set; }
        public string MaxStatus { get; set; } = "0";
        public string Behaviour { get; set; } = "";
        public string Interactor { get; set; } = "default";
        public bool IsTradable { get; set; } = true;
        public bool IsRecyclable { get; set; } = true;
        public string DrinkIds { get; set; } = "";
        public bool IsWallItem => (Behaviour ?? "").Contains("wall_item");
        /// <summary>Catalogue pages that sell it (for the search results).</summary>
        public List<int> OnPages { get; set; } = new();
    }

    /// <summary>Odin's ItemBehaviour and InteractionType names, lower case as stored.</summary>
    public static class CatalogueFurniOptions
    {
        public static readonly string[] Behaviours =
        {
            "solid", "can_stack_on_top", "can_sit_on_top", "can_stand_on_top", "can_lay_on_top", "custom_data_numeric_on_off",
            "requires_touching_for_interaction", "custom_data_true_false", "public_space_object", "extra_parameter", "dice",
            "custom_data_on_off", "custom_data_numeric_state", "teleporter", "requires_rights_for_interaction", "set_random_state",
            "gate", "prize_trophy", "roller", "redeemable", "sound_machine", "sound_machine_sample_set", "jukebox", "wall_item",
            "post_it", "decoration", "wheel_of_fortune", "roomdimmer", "present", "photo", "place_roller_on_top", "invisible",
            "effect", "elevation", "redirect_rotation_0", "redirect_rotation_2", "song_disk", "pet_water_bowl", "pet_food",
            "pet_cat_food", "pet_dog_food", "pet_croc_food", "pet_toy", "private_furniture",
        };

        public static readonly string[] Interactors =
        {
            "default", "bed", "chair", "teleport", "vending_machine", "lert", "scoreboard", "fortune", "pet_nest", "pet_food",
            "pet_water_bowl", "pet_toy", "totem_leg", "totem_head", "totem_planet", "pool_enter", "pool_exit", "pool_booth",
            "pool_lift", "queue_tile", "game_tic_tac_toe", "game_chess", "game_battleships", "game_poker", "ws_join_queue",
            "ws_queue_tile", "ws_tile_start",
        };
    }
}
