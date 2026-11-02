using System.Windows;
using System.Windows.Controls;
using WinCleaner.Core;

namespace WinCleaner.App;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        SectionList.ItemsSource = Section.All;
        SectionList.SelectedIndex = 0;
    }

    private void SectionList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (SectionList.SelectedItem is Section section)
            SectionTitle.Text = section.Title;
    }
}
