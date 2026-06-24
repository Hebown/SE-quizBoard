using System.Collections.Generic;
using System.Linq;
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
            if (ViewModel.IsMultipleChoice)
            {
                ViewModel.ToggleMultipleOptionCommand.Execute(optionText);
            }
            else
            {
                ViewModel.SelectSingleOptionCommand.Execute(optionText);
            }
        }
    }

    private void MultipleCheckBox_CheckedChanged(object sender, RoutedEventArgs e)
    {
        if (sender is CheckBox cb && cb.Tag is string optionText)
        {
            ViewModel.ToggleMultipleOptionCommand.Execute(optionText);
        }
    }

    private void SubmitMultipleButton_Click(object sender, RoutedEventArgs e)
    {
        ViewModel.SubmitMultipleAnswerCommand.Execute(null);
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

    /// <summary>
    /// 为某个选项构建包含英文 + 中文翻译的 StackPanel
    /// </summary>
    private StackPanel BuildOptionContent(string englishText, string chineseText)
    {
        var panel = new StackPanel { Spacing = 2 };
        panel.Children.Add(new TextBlock
        {
            Text = englishText,
            TextWrapping = TextWrapping.Wrap,
            Style = (Style)Application.Current.Resources["BodyTextBlockStyle"],
            FontSize = 15
        });
        if (!string.IsNullOrEmpty(chineseText))
        {
            panel.Children.Add(new TextBlock
            {
                Text = chineseText,
                TextWrapping = TextWrapping.Wrap,
                FontSize = 13,
                Foreground = new SolidColorBrush(ColorHelper.FromArgb(180, 100, 100, 100)),
                FontStyle = Windows.UI.Text.FontStyle.Italic
            });
        }
        return panel;
    }

    private void RefreshQuestionUI()
    {
        var q = ViewModel.CurrentQuestion;
        if (q == null) { RefreshVisibility(); return; }

        QuestionPanel.Visibility = Visibility.Visible;
        EmptyPanel.Visibility = Visibility.Collapsed;

        ProgressHeader.Text = $"错题 {ViewModel.CurrentQuestionNumber} / 共 {ViewModel.TotalQuestions} 题";
        TopicText.Text = q.Topic;

        // 中文翻译
        if (!string.IsNullOrEmpty(q.ChineseTranslation))
        {
            TranslationText.Text = q.ChineseTranslation;
            TranslationText.Visibility = Visibility.Visible;
        }
        else
        {
            TranslationText.Visibility = Visibility.Collapsed;
        }

        // 解析面板默认隐藏
        ExplanationPanel.Visibility = Visibility.Collapsed;

        if (q.Type == "multiple")
        {
            // 多选题：使用复选框
            OptionsList.Visibility = Visibility.Collapsed;
            MultipleOptionsList.Visibility = Visibility.Visible;
            SubmitMultipleButton.Visibility = Visibility.Visible;

            MultipleOptionsList.Items.Clear();
            for (int i = 0; i < q.Options.Count; i++)
            {
                var engText = q.Options[i];
                var cnText = i < q.ChineseOptions.Count ? q.ChineseOptions[i] : "";
                var content = BuildOptionContent(engText, cnText);
                var cb = new CheckBox
                {
                    Content = content,
                    Tag = engText,
                    Margin = new Thickness(0, 0, 0, 6),
                    Padding = new Thickness(8, 4, 8, 4)
                };
                cb.Checked += MultipleCheckBox_CheckedChanged;
                cb.Unchecked += MultipleCheckBox_CheckedChanged;
                MultipleOptionsList.Items.Add(cb);
            }
        }
        else
        {
            // 单选题 / 判断题：使用按钮
            OptionsList.Visibility = Visibility.Visible;
            MultipleOptionsList.Visibility = Visibility.Collapsed;
            SubmitMultipleButton.Visibility = Visibility.Collapsed;

            OptionsList.Items.Clear();
            var options = q.Type == "truefalse"
                ? new List<string> { "T. True", "F. False" }
                : q.Options;

            var chineseOpts = q.Type == "truefalse"
                ? new List<string> { "T. 正确", "F. 错误" }
                : q.ChineseOptions;

            for (int i = 0; i < options.Count; i++)
            {
                var engText = options[i];
                var cnText = i < chineseOpts.Count ? chineseOpts[i] : "";
                var content = BuildOptionContent(engText, cnText);

                var btn = new Button
                {
                    Content = content,
                    Tag = engText,
                    HorizontalAlignment = HorizontalAlignment.Stretch,
                    HorizontalContentAlignment = HorizontalAlignment.Left,
                    Padding = new Thickness(12, 8, 12, 8),
                    Margin = new Thickness(0, 0, 0, 6),
                    Background = new SolidColorBrush(Colors.Transparent),
                    BorderBrush = new SolidColorBrush(ColorHelper.FromArgb(30, 128, 128, 128)),
                    BorderThickness = new Thickness(1),
                    CornerRadius = new CornerRadius(4)
                };
                btn.Click += OptionButton_Click;
                OptionsList.Items.Add(btn);
            }
        }

        OptionsList.IsEnabled = !ViewModel.IsAnswerRevealed;
        MultipleOptionsList.IsEnabled = !ViewModel.IsAnswerRevealed;
        SubmitMultipleButton.IsEnabled = !ViewModel.IsAnswerRevealed;
        NextButton.IsEnabled = ViewModel.CanNext;
        RefreshFeedbackUI();
    }

    private void RefreshFeedbackUI()
    {
        var q = ViewModel.CurrentQuestion;

        if (!ViewModel.IsAnswerRevealed)
        {
            FeedbackBorder.Visibility = Visibility.Collapsed;
            OptionsList.IsEnabled = true;
            MultipleOptionsList.IsEnabled = true;
            SubmitMultipleButton.IsEnabled = true;
            ExplanationPanel.Visibility = Visibility.Collapsed;
            return;
        }

        FeedbackBorder.Visibility = Visibility.Visible;
        OptionsList.IsEnabled = false;
        MultipleOptionsList.IsEnabled = false;
        SubmitMultipleButton.IsEnabled = false;
        FeedbackTextBlock.Text = ViewModel.FeedbackText;

        bool isCorrect = ViewModel.IsCurrentAnswerCorrect;
        FeedbackBorder.Background = new SolidColorBrush(isCorrect
            ? ColorHelper.FromArgb(20, 0, 180, 0)
            : ColorHelper.FromArgb(20, 200, 0, 0));
        FeedbackBorder.BorderBrush = new SolidColorBrush(isCorrect
            ? ColorHelper.FromArgb(120, 0, 180, 0)
            : ColorHelper.FromArgb(120, 200, 0, 0));
        FeedbackTextBlock.Foreground = new SolidColorBrush(isCorrect
            ? ColorHelper.FromArgb(220, 0, 120, 0)
            : ColorHelper.FromArgb(220, 180, 0, 0));

        // ★ 回答后自动显示解析（无论对错）
        if (q != null)
        {
            if (!string.IsNullOrEmpty(q.ChineseTranslation))
            {
                ChineseTranslationText.Text = q.ChineseTranslation;
                ChineseTranslationText.Visibility = Visibility.Visible;
            }
            else
            {
                ChineseTranslationText.Visibility = Visibility.Collapsed;
            }

            if (!string.IsNullOrEmpty(q.Explanation))
            {
                ExplanationText.Text = q.Explanation;
            }
            ExplanationPanel.Visibility = Visibility.Visible;
        }
    }
}
