using StardewValley.ItemTypeDefinitions;
using StardewModdingAPI;
using StardewValley;
using StardewValley.GameData;

namespace SproutSight;
internal partial class TrackedItemStack : IComparable<TrackedItemStack>
{
    public string Id { get; set; } = "";
    public int StackCount { get; set; }
    public int SalePrice { get; set; }
    public int TotalSalePrice => SalePrice * StackCount;
    public int Quality { get; set; }
    public string QualityName => ((ItemQuality)Quality).ToString();

    public int Category { get; set; }
    public string CategoryName => GetCategoryName(Category);

    private ParsedItemData? _parsedItemData;
    public ParsedItemData ParsedItemData => _parsedItemData ??= ItemRegistry.GetDataOrErrorItem(Id);

    private string? _name;
    public string Name => _name ??= ParsedItemData.DisplayName;

    public ParsedItemData Sprite => ParsedItemData;

    public TrackedItemStack()
    {
    }

    public TrackedItemStack(Item item)
    {
        this.Id = item.ItemId;
        this.StackCount = item.stack.Value;
        this.SalePrice = item.sellToStorePrice();
        this.Quality = item.Quality;
        this.Category = item.Category;
    }

    public int CompareTo(TrackedItemStack? other)
    {
        if (other == null) return 1;
        // Sort by TotalSalePrice in descending order (highest first)
        return other.TotalSalePrice.CompareTo(this.TotalSalePrice);
    }

    public static string GetCategoryName(int category)
    {
        return category switch
        {
            -2 => "Mineral",           // Gem Category (affected by Gemologist)
            -4 => "Fish",              // Fish Category (affected by Fisher/Angler)
            -5 => "Animal Product",     // Egg Category (affected by Rancher)
            -6 => "Animal Product",     // Milk Category (affected by Rancher)
            -7 => "Cooking",           // Cooking Category
            -8 => "Crafting",          // Crafting Category
            -9 => "Big Craftable",     // Big Craftable Category
            -12 => "Mineral",          // Minerals Category (affected by Gemologist)
            -14 => "Animal Product",    // Meat Category
            -15 => "Resource",         // Metal Resources
            -16 => "Resource",         // Building Resources
            -17 => "Pierre's Store",   // Sell at Pierre's
            -18 => "Animal Product",    // Sell at Pierre's and Marnie's (affected by Rancher)
            -19 => "Fertilizer",       // Fertilizer Category
            -20 => "Trash",            // Junk Category
            -21 => "Bait",            // Bait Category
            -22 => "Fishing Tackle",   // Tackle Category
            -23 => "Fish Shop",        // Sell at Fish Shop
            -24 => "Decor",           // Furniture Category
            -25 => "Cooking",         // Ingredients Category
            -26 => "Artisan Goods",    // Artisan Goods Category (affected by Artisan)
            -27 => "Artisan Goods",    // Syrup Category (affected by Tapper)
            -28 => "Monster Loot",     // Monster Loot Category
            -29 => "Equipment",        // Equipment Category
            -74 => "Seed",            // Seeds Category
            -75 => "Vegetable",        // Vegetable Category (affected by Tiller)
            -79 => "Fruit",           // Fruits Category (affected by Tiller if not foraged)
            -80 => "Flower",          // Flowers Category (affected by Tiller)
            -81 => "Forage",          // Greens Category
            -95 => "Hat",             // Hat Category
            -96 => "Ring",            // Ring Category
            -97 => "Boots",           // Boots Category
            -98 => "Weapon",          // Weapon Category
            -99 => "Tool",            // Tool Category
            -100 => "Clothing",        // Clothing Category
            -101 => "Trinket",         // Trinket Category
            -102 => "Book",            // Books Category
            -103 => "Skill Book",      // Skill Books Category
            -999 => "Litter",          // Litter Category
            0 => "Uncategorized",
            _ => $"Unknown ({category})"
        };
    }

    public override string ToString()
    {
        return $"{Name} (Id: {Id}, Stack: {StackCount}, Price: {SalePrice}g, Quality: {QualityName}, Category: {CategoryName})";
    }

    public string FormattedSale => $"({StackCount}x{SalePrice}g)";

}