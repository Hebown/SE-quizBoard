using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI;
using Microsoft.Extensions.DependencyInjection;
using QuizNative.Services;
using QuizNative.ViewModels;

namespace QuizNative.Views;

public sealed partial class HistoryOverviewSubPage : Page
{
    private readonly IQuizDataService _quizDataService;
    private readonly ILocalStateService _localStateService;
    private readonly HistoryViewModel _viewModel;

    public HistoryOverviewSubPage()
    {
        this.InitializeComponent();

        _quizDataService = App.Services.GetRequiredService<IQuizDataService>();
        _localStateService = App.Services.GetRequiredService<ILocalStateService>();
        _viewModel = App.Services.GetRequiredService<HistoryViewModel>();

        this.Loaded += (s, e) => RenderOverview();
    }

    private void RenderOverview()
    {
        _viewModel.CalculateStatistics();

        // 总进度统计
        TotalPracticedLabel.Text = $"{_viewModel.TotalPracticedCount}";
        TotalSuffixLabel.Text = $" / {_viewModel.TotalQuestionsCount} 题";
        TotalPercentageLabel.Text = _viewModel.FormattedPercentage(_viewModel.TotalProgressPercentage);
        TotalProgressBar.Value = _viewModel.TotalProgressPercentage;

        // 概览小卡片
        WrongCountLabel.Text = $"{_viewModel.CurrentWrongBookCount}";
        TotalCountLabel.Text = $"{_viewModel.TotalQuestionsCount}";

        // 章节列表
        ChapterList.Items.Clear();
        var chapterProgresses = _viewModel.ChapterProgresses;
        foreach (var item in chapterProgresses)
        {
            var btn = CreateChapterItemButton(item);
            ChapterList.Items.Add(btn);
        }
    }

    private Button CreateChapterItemButton(ChapterProgressItem item)
    {
        // 对错徽章
        var correctPanel = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 4 };
        correctPanel.Children.Add(new FontIcon { Glyph = "\uE930", FontSize = 12, Foreground = new SolidColorBrush(Colors.Green) });
        correctPanel.Children.Add(new TextBlock { Text = "做对", Style = (Style)Application.Current.Resources["CaptionTextBlockStyle"], Foreground = (Brush)Application.Current.Resources["SystemControlPageTextBaseMediumBrush"] });
        correctPanel.Children.Add(new TextBlock { Text = item.CorrectTimes.ToString(), Style = (Style)Application.Current.Resources["CaptionTextBlockStyle"], FontWeight = Microsoft.UI.Text.FontWeights.Bold, Foreground = new SolidColorBrush(Colors.Green) });
        correctPanel.Children.Add(new TextBlock { Text = "次", Style = (Style)Application.Current.Resources["CaptionTextBlockStyle"], Foreground = (Brush)Application.Current.Resources["SystemControlPageTextBaseMediumBrush"] });

        var wrongPanel = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 4 };
        wrongPanel.Children.Add(new FontIcon { Glyph = "\uE7BA", FontSize = 12, Foreground = new SolidColorBrush(Colors.Red) });
        wrongPanel.Children.Add(new TextBlock { Text = "做错", Style = (Style)Application.Current.Resources["CaptionTextBlockStyle"], Foreground = (Brush)Application.Current.Resources["SystemControlPageTextBaseMediumBrush"] });
        wrongPanel.Children.Add(new TextBlock { Text = item.WrongTimes.ToString(), Style = (Style)Application.Current.Resources["CaptionTextBlockStyle"], FontWeight = Microsoft.UI.Text.FontWeights.Bold, Foreground = new SolidColorBrush(Colors.Red) });
        wrongPanel.Children.Add(new TextBlock { Text = "次", Style = (Style)Application.Current.Resources["CaptionTextBlockStyle"], Foreground = (Brush)Application.Current.Resources["SystemControlPageTextBaseMediumBrush"] });

        var arrowIcon = new FontIcon { Glyph = "\uE76B", FontSize = 12, Foreground = (Brush)Application.Current.Resources["SystemControlPageTextBaseMediumBrush"] };
        arrowIcon.VerticalAlignment = VerticalAlignment.Center;

        var badgePanel = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 16, Margin = new Thickness(0, 6, 0, 0) };
        badgePanel.Children.Add(correctPanel);
        badgePanel.Children.Add(wrongPanel);
        badgePanel.Children.Add(arrowIcon);

        // 进度条
        var progressBar = new ProgressBar
        {
            Value = item.Percentage,
            Maximum = 100,
            Height = 4,
            Margin = new Thickness(0, 4, 0, 0),
            CornerRadius = new CornerRadius(2)
        };

        // 章节标题
        var titleText = new TextBlock
        {
            Text = item.Title,
            Style = (Style)Application.Current.Resources["BodyTextBlockStyle"],
            FontWeight = Microsoft.UI.Text.FontWeights.Medium
        };

        // 比例文本
        var ratioText = new TextBlock
        {
            Text = item.RatioText,
            Style = (Style)Application.Current.Resources["CaptionTextBlockStyle"],
            Foreground = (Brush)Application.Current.Resources["SystemControlPageTextBaseMediumBrush"]
        };

        // Grid 布局（使用属性设置而非构造函数）
        var grid = new Grid { RowSpacing = 8 };
        var colDef1 = new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) };
        var colDef2 = new ColumnDefinition { Width = GridLength.Auto };
        var rowDef1 = new RowDefinition { Height = GridLength.Auto };
        var rowDef2 = new RowDefinition { Height = GridLength.Auto };
        var rowDef3 = new RowDefinition { Height = GridLength.Auto };
        grid.ColumnDefinitions.Add(colDef1);
        grid.ColumnDefinitions.Add(colDef2);
        grid.RowDefinitions.Add(rowDef1);
        grid.RowDefinitions.Add(rowDef2);
        grid.RowDefinitions.Add(rowDef3);

        titleText.SetValue(Grid.RowProperty, 0); titleText.SetValue(Grid.ColumnProperty, 0);
        ratioText.SetValue(Grid.RowProperty, 0); ratioText.SetValue(Grid.ColumnProperty, 1);
        progressBar.SetValue(Grid.RowProperty, 1); progressBar.SetValue(Grid.ColumnSpanProperty, 2);
        badgePanel.SetValue(Grid.RowProperty, 2); badgePanel.SetValue(Grid.ColumnSpanProperty, 2);

        grid.Children.Add(titleText);
        grid.Children.Add(ratioText);
        grid.Children.Add(progressBar);
        grid.Children.Add(badgePanel);

        // 外框 Border
        var border = new Border
        {
            Background = (Brush)Application.Current.Resources["ControlFillColorDefaultBrush"],
            BorderBrush = (Brush)Application.Current.Resources["ControlStrokeColorDefaultBrush"],
            BorderThickness = new Thickness(1, 1, 1, 1),
            CornerRadius = new CornerRadius(6),
            Padding = new Thickness(16, 12, 16, 12),
            HorizontalAlignment = HorizontalAlignment.Stretch,
            Child = grid
        };

        var chapterId = item.ChapterId;
        var btn = new Button
        {
            Content = border,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            HorizontalContentAlignment = HorizontalAlignment.Stretch,
            Background = new SolidColorBrush(Colors.Transparent),
            BorderThickness = new Thickness(0, 0, 0, 0),
            Padding = new Thickness(0, 0, 0, 0),
            Margin = new Thickness(0, 0, 0, 8),
            Tag = chapterId
        };
        btn.Click += (s, e) =>
        {
            var frame = this.Frame;
            if (frame != null)
                frame.Navigate(typeof(HistoryChapterDetailsSubPage), new ChapterDetailParam(chapterId));
        };

        return btn;
    }

    private void ChapterItem_Click(object sender, RoutedEventArgs e)
    {
        // Handled by button click event handler above
    }
}
