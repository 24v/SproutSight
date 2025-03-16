namespace SproutSight.Display;

internal partial class YearSelectionViewModel(int Year, bool isChecked) 
{
    public static readonly int YEAR_ALL = 0;
    
    public bool IsChecked {get; set; } = isChecked;

    public int Year {get; set;} = Year;

    public string Text
    {
        get{
            if (Year > 0)
            {
                return Year + "";
            }
            else 
            {
                return "All";
            }
        }
    }

    public void HandleAllSelection(bool allSelected)
    {
        if (Year == YEAR_ALL && !allSelected)
        {
            IsChecked = false;
        }

        if (Year != YEAR_ALL && allSelected)
        {
            IsChecked = false;
        }
    }
}
