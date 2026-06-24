using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI;
using Microsoft.Extensions.DependencyInjection;
using QuizNative.ViewModels;

namespace QuizNative.Views;

public sealed partial class WrongQuestionsPage : Page
{
    public WrongBookViewModel ViewModel { get; }

    public WrongQuestionsPage()
    {
        this.InitializeComponent();
        ViewModel = App.Services.GetRequiredService<WrongBookViewModel>();

        // 每次切进页面时从本地存储刷新错题数据
        this.Loaded += (s, e) => ViewModel.RefreshWrongQuestions();

        ViewModel.PropertyChanged += (s, e) =>
        {
            DispatcherQueue.TryEnqueue(() =>
            {
                switch (e.PropertyName)
                {
                    case nameof(ViewModel.CurrentQuestion):
                    case nameof(ViewModel.CurrentQuestionNumber):
                    case nameof(ViewModel.TotalQuestions):
                        RefreshQuestionUI();
                        break;
                    case nameof(ViewModel.IsAnswerRevealed):
                    case nameof(ViewModel.IsCurrentAnswerCorrect):
                    case nameof(ViewModel.FeedbackText):
                        RefreshFeedbackUI();
                        break;
                    case nameof(ViewModel.HasWrongQuestions):
                        RefreshVisibility();
                        break;
                }
            });
        };
    }

    private void OptionButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is string optionText)
        {
            ViewModel.JudgeWrongAnswerCommand.Execute(optionText);
        }
    }

    private void NextButton_Click(object sender, RoutedEventArgs e)
    {
        ViewModel.NextCommand.Execute(null);
    }

    private void RefreshVisibility()
    {
        bool hasQuestions = ViewModel.HasWrongQuestions;
        QuestionPanel.Visibility = hasQuestions ? Visibility.Visible : Visibility.Collapsed;
        EmptyPanel.Visibility = hasQuestions ? Visibility.Collapsed : Visibility.Visible;
    }

    private void RefreshQuestionUI()
    {
        var q = ViewModel.CurrentQuestion;
        if (q == null) { RefreshVisibility(); return; }

        QuestionPanel.Visibility = Visibility.Visible;
        EmptyPanel.Visibility = Visibility.Collapsed;

        ProgressHeader.Text = $"错题 {ViewModel.CurrentQuestionNumber} / 共 {ViewModel.TotalQuestions} 题";
        TopicText.Text = q.Topic;

        OptionsList.Items.Clear();
        for (int i = 0; i < q.Options.Count; i++)
        {
            var optionText = q.Options[i];
            var btn = new Button
            {
                Content = new TextBlock
                {
                    Text = optionText,
                    TextWrapping = TextWrapping.Wrap,
                    Style = (Style)Application.Current.Resources["BodyTextBlockStyle"]
                },
                Tag = optionText,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                HorizontalContentAlignment = HorizontalAlignment.Left,
                Padding = new Thickness(16, 12, 16, 12),
                Margin = new Thickness(0, 0, 0, 8),
                Background = new SolidColorBrush(Colors.Transparent),
                BorderBrush = new SolidColorBrush(ColorHelper.FromArgb(30, 128, 128, 128)),
                BorderThickness = new Thickness(1, 1, 1, 1),
                CornerRadius = new CornerRadius(6)
            };
            btn.Click += OptionButton_Click;
            OptionsList.Items.Add(btn);
        }
        OptionsList.IsEnabled = !ViewModel.IsAnswerRevealed;
        NextButton.IsEnabled = ViewModel.CanNext;
        RefreshFeedbackUI();
    }

    private void RefreshFeedbackUI()
    {
        if (!ViewModel.IsAnswerRevealed)
        {
            FeedbackBorder.Visibility = Visibility.Collapsed;
            OptionsList.IsEnabled = true;
            return;
        }

        FeedbackBorder.Visibility = Visibility.Visible;
        OptionsList.IsEnabled = false;
        FeedbackTextBlock.Text = ViewModel.FeedbackText;

        bool isCorrect = ViewModel.IsCurrentAnswerCorrect;
        FeedbackBorder.Background = new SolidColorBrush(
            ColorHelper.FromArgb(20, isCorrect ? (byte)0 : (byte)200, isCorrect ? (byte)180 : (byte)0, 0));
        FeedbackBorder.BorderBrush = new SolidColorBrush(isCorrect
            ? ColorHelper.FromArgb(120, 0, 180, 0)
            : ColorHelper.FromArgb(120, 200, 0, 0));
        FeedbackTextBlock.Foreground = new SolidColorBrush(isCorrect
            ? ColorHelper.FromArgb(220, 0, 120, 0)
            : ColorHelper.FromArgb(220, 180, 0, 0));
    }
}
