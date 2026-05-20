using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace HorizonXIBackupApp.Views;

public partial class BackupsView : UserControl
{
    public BackupsView()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}
