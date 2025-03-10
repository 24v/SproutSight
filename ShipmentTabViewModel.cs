using PropertyChanged.SourceGenerator;
using System.ComponentModel;

namespace SproutSight;


internal enum ShipmentTab
{
    Today,
    Shipping,
    Wallet,
    CashFlow
}
internal partial class ShipmentTabViewModel(ShipmentTab value, bool isActive) : INotifyPropertyChanged
{
    [Notify]
    private bool _isActive = isActive;

    public Tuple<int, int, int, int> Margin => IsActive ? new(0, 0, -12, 0) : new(0, 0, 0, 0);

    public ShipmentTab Value { get; } = value;

    public string Title {
        get
        {
            return Value switch 
            {
                ShipmentTab.Today => "Today",
                ShipmentTab.Shipping => "Shipping",
                ShipmentTab.CashFlow => "Cash Flow",
                ShipmentTab.Wallet => "Wallet",
                _ => Value.ToString()
            };
        }
    }

}
