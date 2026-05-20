using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace HorizonXIBackupApp.Views;

public partial class AddonsView : UserControl
{
    public AddonsView()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}
