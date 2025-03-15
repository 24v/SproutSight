using PropertyChanged.SourceGenerator;
using System.ComponentModel;

namespace SproutSight;


internal partial class YearSelectionViewModel(int Year, bool isChecked) : INotifyPropertyChanged
{
    public static readonly int YEAR_ALL = 0;
    
    [Notify]
    private bool _isChecked = isChecked;

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
        Logging.Monitor.Log($"Handle All Selection {Year}, {allSelected}");
        if (Year == YEAR_ALL && !allSelected)
        {
            IsChecked = false;
            Logging.Monitor.Log($"1 -> setting isChecked to {IsChecked}");
        }

        if (Year != YEAR_ALL && allSelected)
        {
            IsChecked = false;
            Logging.Monitor.Log($"2 -> setting isChecked to {IsChecked}");
        }
    }
}
