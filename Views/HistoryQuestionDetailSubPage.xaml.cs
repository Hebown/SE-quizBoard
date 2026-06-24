using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI;
using Microsoft.Extensions.DependencyInjection;
using QuizNative.Services;

namespace QuizNative.Views;

public sealed partial class HistoryQuestionDetailSubPage : Page
{
    private readonly IQuizDataService _quizDataService;
    private readonly ILocalStateService _localStateService;
    private string _questionId = string.Empty;

    public HistoryQuestionDetailSubPage()
    {
        this.InitializeComponent();

        _quizDataService = App.Services.GetRequiredService<IQuizDataService>();
        _localStateService = App.Services.GetRequiredService<ILocalStateService>();
    }

    protected override void OnNavigatedTo(Microsoft.UI.Xaml.Navigation.NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);

        if (e.Parameter is QuestionDetailParam param)
        {
            _questionId = param.QuestionId;
            LoadQuestionDetail();
        }
    }

    private void LoadQuestionDetail()
    {
        var q = _quizDataService.GetQuestionById(_questionId);
        if (q == null) return;

        // 面包屑标题
        BreadcrumbTitle.Text = $"题目 {q.Id}";

        // 履历统计卡片
        int correctTimes = _localStateService.Progress.CorrectCounts.GetValueOrDefault(_questionId, 0);
        int wrongTimes = _localStateService.Progress.WrongCounts.GetValueOrDefault(_questionId, 0);
        int totalAttempts = correctTimes + wrongTimes;

        CorrectCountText.Text = correctTimes.ToString();
        WrongCountText.Text = wrongTimes.ToString();
        TotalAttemptsText.Text = totalAttempts.ToString();

        // 题干
        TopicText.Text = q.Topic;

        // 正确答案
        AnswerText.Text = $"标准答案: {q.Answer}";

        // 选项列表（只读锁定）
        OptionsList.Items.Clear();
        foreach (var option in q.Options)
        {
            var border = new Border
            {
                Background = (Brush)Application.Current.Resources["ControlFillColorDefaultBrush"],
                BorderBrush = (Brush)Application.Current.Resources["ControlStrokeColorDefaultBrush"],
                BorderThickness = new Thickness(1, 1, 1, 1),
                CornerRadius = new CornerRadius(6),
                Padding = new Thickness(16, 12, 16, 12),
                Margin = new Thickness(0, 0, 0, 8),
                HorizontalAlignment = HorizontalAlignment.Stretch,
                IsHitTestVisible = false,
                Child = new TextBlock
                {
                    Text = option,
                    Style = (Style)Application.Current.Resources["BodyTextBlockStyle"],
                    TextWrapping = TextWrapping.Wrap
                }
            };
            OptionsList.Items.Add(border);
        }
    }

    private void BackButton_Click(object sender, RoutedEventArgs e)
    {
        if (this.Frame != null && this.Frame.CanGoBack)
            this.Frame.GoBack();
        else if (this.Frame != null)
            this.Frame.Navigate(typeof(HistoryOverviewSubPage));
    }
}
