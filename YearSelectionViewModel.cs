using PropertyChanged.SourceGenerator;
using System.ComponentModel;

namespace SproutSight;


internal partial class YearSelectionViewModel(int Year, bool IsChecked) 
{
    public static readonly int YEAR_ALL = 0;

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

    public void HandleAllSelection(bool isAll)
    {
        if (Year == YEAR_ALL && !isAll)
        {
            IsChecked = false;
        }
        if (Year != YEAR_ALL && isAll)
        {
            IsChecked = false;
        }
    }
}
