using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI;
using Microsoft.Extensions.DependencyInjection;
using QuizNative.Services;

namespace QuizNative.Views;

public sealed partial class HistoryChapterDetailsSubPage : Page
{
    private readonly IQuizDataService _quizDataService;
    private readonly ILocalStateService _localStateService;
    private string _chapterId = string.Empty;

    public HistoryChapterDetailsSubPage()
    {
        this.InitializeComponent();

        _quizDataService = App.Services.GetRequiredService<IQuizDataService>();
        _localStateService = App.Services.GetRequiredService<ILocalStateService>();
    }

    protected override void OnNavigatedTo(Microsoft.UI.Xaml.Navigation.NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);

        if (e.Parameter is ChapterDetailParam param)
        {
            _chapterId = param.ChapterId;
            ChapterTitleText.Text = $"第 {_chapterId} 章节";
            LoadChapterQuestions();
        }
    }

    private void LoadChapterQuestions()
    {
        QuestionList.Items.Clear();

        var questions = _quizDataService.GetQuestionsByChapter(_chapterId);
        var correctCounts = _localStateService.Progress.CorrectCounts;
        var wrongCounts = _localStateService.Progress.WrongCounts;

        foreach (var q in questions)
        {
            var btn = CreateQuestionItemButton(q, correctCounts.GetValueOrDefault(q.Id, 0), wrongCounts.GetValueOrDefault(q.Id, 0));
            QuestionList.Items.Add(btn);
        }

        SectionTitle.Text = $"本章节题目履历 (共 {questions.Count} 题)";
    }

    private Button CreateQuestionItemButton(QuizNative.Models.Question q, int correctTimes, int wrongTimes)
    {
        // 正确次数标签
        var correctIcon = new FontIcon { Glyph = "\uE930", FontSize = 12, Foreground = new SolidColorBrush(Colors.Green) };
        var correctText = new TextBlock
        {
            Text = $"做对 {correctTimes} 次",
            Style = (Style)Application.Current.Resources["CaptionTextBlockStyle"],
            FontWeight = Microsoft.UI.Text.FontWeights.Bold,
            Foreground = new SolidColorBrush(Colors.Green)
        };
        var correctPanel = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 4 };
        correctPanel.Children.Add(correctIcon);
        correctPanel.Children.Add(correctText);

        // 错误次数标签
        var wrongIcon = new FontIcon { Glyph = "\uE7BA", FontSize = 12, Foreground = new SolidColorBrush(Colors.Red) };
        var wrongText = new TextBlock
        {
            Text = $"做错 {wrongTimes} 次",
            Style = (Style)Application.Current.Resources["CaptionTextBlockStyle"],
            FontWeight = Microsoft.UI.Text.FontWeights.Bold,
            Foreground = new SolidColorBrush(Colors.Red)
        };
        var wrongPanel = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 4 };
        wrongPanel.Children.Add(wrongIcon);
        wrongPanel.Children.Add(wrongText);

        // 对错徽章行
        var badgePanel = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 12 };
        badgePanel.Children.Add(correctPanel);
        badgePanel.Children.Add(wrongPanel);

        // 题目文本（截断显示）
        var topicText = new TextBlock
        {
            Text = q.Topic,
            Style = (Style)Application.Current.Resources["BodyTextBlockStyle"],
            TextWrapping = TextWrapping.Wrap,
            MaxHeight = 48,
            TextTrimming = TextTrimming.CharacterEllipsis
        };

        // 左侧内容
        var leftStack = new StackPanel { Spacing = 6 };
        leftStack.Children.Add(topicText);
        leftStack.Children.Add(badgePanel);

        // 右箭头
        var arrowIcon = new FontIcon
        {
            Glyph = "\uE76B",
            FontSize = 14,
            Foreground = (Brush)Application.Current.Resources["SystemControlPageTextBaseMediumBrush"]
        };
        arrowIcon.VerticalAlignment = VerticalAlignment.Center;

        // Grid 布局（使用属性设置而非构造函数）
        var grid = new Grid();
        var colDef1 = new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) };
        var colDef2 = new ColumnDefinition { Width = GridLength.Auto };
        grid.ColumnDefinitions.Add(colDef1);
        grid.ColumnDefinitions.Add(colDef2);

        leftStack.SetValue(Grid.ColumnProperty, 0);
        arrowIcon.SetValue(Grid.ColumnProperty, 1);

        grid.Children.Add(leftStack);
        grid.Children.Add(arrowIcon);

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

        var questionId = q.Id;
        var btn = new Button
        {
            Content = border,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            HorizontalContentAlignment = HorizontalAlignment.Stretch,
            Background = new SolidColorBrush(Colors.Transparent),
            BorderThickness = new Thickness(0, 0, 0, 0),
            Padding = new Thickness(0, 0, 0, 0),
            Margin = new Thickness(0, 0, 0, 6),
            Tag = questionId
        };
        btn.Click += (s, e) =>
        {
            var frame = this.Frame;
            if (frame != null)
                frame.Navigate(typeof(HistoryQuestionDetailSubPage), new QuestionDetailParam(questionId));
        };

        return btn;
    }

    private void BackButton_Click(object sender, RoutedEventArgs e)
    {
        if (this.Frame != null && this.Frame.CanGoBack)
            this.Frame.GoBack();
        else if (this.Frame != null)
            this.Frame.Navigate(typeof(HistoryOverviewSubPage));
    }

    private void QuestionItem_Click(object sender, RoutedEventArgs e)
    {
        // Handled by button click event handler above
    }
}
