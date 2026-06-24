using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using QuizNative.Views;
using System;

namespace QuizNative;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        this.InitializeComponent();

        // 隐藏传统标题栏，使 Mica 材质蔓延到顶部
        ExtendsContentIntoTitleBar = true;
        SetTitleBar(AppTitleBar);

        // 默认选中"顺序刷题"
        NavView.SelectedItem = NavStandard;
    }

    private void NavView_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
    {
        if (args.SelectedItemContainer is NavigationViewItem item)
        {
            Type? targetPageType = item.Tag switch
            {
                "standard" => typeof(StandardPage),
                "wrong" => typeof(WrongQuestionsPage),
                "history" => typeof(HistoryPage),
                _ => null
            };

            if (targetPageType != null && ContentFrame.CurrentSourcePageType != targetPageType)
            {
                ContentFrame.Navigate(targetPageType, null, args.RecommendedNavigationTransitionInfo);
            }
        }
    }
}
