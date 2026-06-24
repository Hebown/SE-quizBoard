using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI;
using Microsoft.Extensions.DependencyInjection;
using QuizNative.Models;
using QuizNative.ViewModels;

namespace QuizNative.Views;

public sealed partial class StandardPage : Page
{
    public StandardViewModel ViewModel { get; }
    public StandardPage()
    {
        this.InitializeComponent();
        ViewModel = App.Services.GetRequiredService<StandardViewModel>();

        // 监听 ViewModel 属性变化以同步 UI
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
                }
            });
        };
    }

    // ========== 事件处理 ==========

    private void ChapterListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (e.AddedItems.FirstOrDefault() is ChapterItem item)
        {
            ViewModel.SelectedChapter = item.Id;
        }
    }

    private void OptionButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is string optionText)
        {
            ViewModel.JudgeAnswerCommand.Execute(optionText);
        }
    }

    private void PreviousButton_Click(object sender, RoutedEventArgs e)
    {
        ViewModel.PreviousCommand.Execute(null);
    }

    private void NextButton_Click(object sender, RoutedEventArgs e)
    {
        ViewModel.NextCommand.Execute(null);
    }

    // ========== UI 同步 ==========

    private void RefreshQuestionUI()
    {
        var q = ViewModel.CurrentQuestion;

        if (q == null)
        {
            QuestionScrollViewer.Visibility = Visibility.Collapsed;
            NavBar.Visibility = Visibility.Collapsed;
            EmptyPanel.Visibility = Visibility.Visible;
            return;
        }

        QuestionScrollViewer.Visibility = Visibility.Visible;
        EmptyPanel.Visibility = Visibility.Collapsed;

        // 进度头
        ProgressHeader.Text = $"第 {ViewModel.CurrentQuestionNumber} 题 / 共 {ViewModel.TotalQuestions} 题";

        // 题干（视觉降噪：纯文本直出，去掉沉重外框）
        TopicText.Text = q.Topic;

        // 动态构建选项按钮（视觉降噪：减淡边框，只保留轻微描边）
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

        // 底部导航
        NavBar.Visibility = Visibility.Visible;
        NavProgressText.Text = $"{ViewModel.CurrentQuestionNumber} / {ViewModel.TotalQuestions}";

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
        FeedbackBorder.Background = new SolidColorBrush(isCorrect
            ? ColorHelper.FromArgb(20, 0, 180, 0)
            : ColorHelper.FromArgb(20, 200, 0, 0));
        FeedbackBorder.BorderBrush = new SolidColorBrush(isCorrect ? ColorHelper.FromArgb(120, 0, 180, 0) : ColorHelper.FromArgb(120, 200, 0, 0));
        FeedbackTextBlock.Foreground = new SolidColorBrush(isCorrect ? ColorHelper.FromArgb(220, 0, 120, 0) : ColorHelper.FromArgb(220, 180, 0, 0));
    }
}
