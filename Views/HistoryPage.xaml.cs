using Microsoft.UI.Xaml.Controls;
using Microsoft.Extensions.DependencyInjection;
using QuizNative.ViewModels;

namespace QuizNative.Views;

public sealed partial class HistoryPage : Page
{
    public HistoryViewModel ViewModel { get; }

    public HistoryPage()
    {
        this.InitializeComponent();

        ViewModel = App.Services.GetRequiredService<HistoryViewModel>();

        // 每次切进页面时重算统计数据
        this.Loaded += (s, e) =>
        {
            ViewModel.CalculateStatistics();
            // 默认进入层级1：总览概览页
            HistoryFrame.Navigate(typeof(HistoryOverviewSubPage));
        };
    }
}
