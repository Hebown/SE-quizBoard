using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using QuizNative.Models;
using QuizNative.Services;

namespace QuizNative.ViewModels;

/// <summary>
/// 带显示文本的章节项
/// </summary>
public record ChapterItem(string Id, string DisplayText);

public partial class StandardViewModel : ObservableObject
{
    private readonly IQuizDataService _quizDataService;
    private readonly ILocalStateService _localStateService;

    /// <summary>所有可供选择的章节列表</summary>
    public ObservableCollection<ChapterItem> Chapters { get; } = new();

    /// <summary>当前选中章节下的全部题目</summary>
    public ObservableCollection<Question> CurrentQuestions { get; } = new();

    [ObservableProperty]
    private string? _selectedChapter;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(PreviousCommand))]
    [NotifyCanExecuteChangedFor(nameof(NextCommand))]
    private int _currentIndex = -1;

    [ObservableProperty]
    private Question? _currentQuestion;

    [ObservableProperty]
    private string? _selectedOption;

    /// <summary>多选题：用户已勾选的选项字母列表</summary>
    public ObservableCollection<string> SelectedMultipleOptions { get; } = new();

    [ObservableProperty]
    private bool _isAnswerRevealed;

    [ObservableProperty]
    private string _feedbackText = string.Empty;

    [ObservableProperty]
    private bool _isCurrentAnswerCorrect;

    /// <summary>单选题/判断题的选项文本（可点击的按钮）</summary>
    public ObservableCollection<string> SingleChoiceOptions { get; } = new();

    /// <summary>多选题的选项文本（可勾选）</summary>
    public ObservableCollection<string> MultipleChoiceOptions { get; } = new();

    /// <summary>当前是否为多选题</summary>
    public bool IsMultipleChoice => CurrentQuestion?.Type == "multiple";
    /// <summary>当前是否为判断题</summary>
    public bool IsTrueFalse => CurrentQuestion?.Type == "truefalse";
    /// <summary>当前是否为单选题</summary>
    public bool IsSingleChoice => CurrentQuestion?.Type == "single";

    public int TotalQuestions => CurrentQuestions.Count;
    public int CurrentQuestionNumber => CurrentIndex + 1;
    public bool HasQuestions => CurrentQuestions.Count > 0;

    public StandardViewModel(IQuizDataService quizDataService, ILocalStateService localStateService)
    {
        _quizDataService = quizDataService;
        _localStateService = localStateService;
        LoadChapters();
    }

    private void LoadChapters()
    {
        Chapters.Clear();
        foreach (var ch in _quizDataService.GetChapters())
        {
            Chapters.Add(new ChapterItem(ch, $"第 {ch} 章节"));
        }
    }

    partial void OnSelectedChapterChanged(string? value)
    {
        CurrentQuestions.Clear();
        if (!string.IsNullOrEmpty(value))
        {
            var questions = _quizDataService.GetQuestionsByChapter(value);
            foreach (var q in questions) CurrentQuestions.Add(q);
            OnPropertyChanged(nameof(HasQuestions));
            OnPropertyChanged(nameof(TotalQuestions));
            CurrentIndex = -1;
            CurrentIndex = CurrentQuestions.Count > 0 ? 0 : -1;
        }
        else
        {
            CurrentIndex = -1;
        }
    }

    partial void OnCurrentIndexChanged(int value)
    {
        if (value >= 0 && value < CurrentQuestions.Count)
        {
            CurrentQuestion = CurrentQuestions[value];
            OnPropertyChanged(nameof(CurrentQuestionNumber));
            OnPropertyChanged(nameof(IsMultipleChoice));
            OnPropertyChanged(nameof(IsTrueFalse));
            OnPropertyChanged(nameof(IsSingleChoice));
            ResetAnswerState();
        }
        else
        {
            CurrentQuestion = null;
        }
    }

    private void ResetAnswerState()
    {
        SelectedOption = null;
        SelectedMultipleOptions.Clear();
        SingleChoiceOptions.Clear();
        MultipleChoiceOptions.Clear();
        IsAnswerRevealed = false;
        FeedbackText = string.Empty;
        IsCurrentAnswerCorrect = false;

        if (CurrentQuestion != null)
        {
            // 根据题型设置选项
            if (CurrentQuestion.Type == "truefalse")
            {
                SingleChoiceOptions.Add("T. True");
                SingleChoiceOptions.Add("F. False");
            }
            else if (CurrentQuestion.Type == "multiple")
            {
                foreach (var opt in CurrentQuestion.Options)
                    MultipleChoiceOptions.Add(opt);
            }
            else // single
            {
                foreach (var opt in CurrentQuestion.Options)
                    SingleChoiceOptions.Add(opt);
            }
        }
    }

    /// <summary>
    /// 单选题/判断题：选择一个选项
    /// </summary>
    [RelayCommand]
    private void SelectSingleOption(string optionText)
    {
        if (IsAnswerRevealed || CurrentQuestion == null || string.IsNullOrEmpty(optionText))
            return;

        SelectedOption = optionText;
        IsAnswerRevealed = true;

        string chosenLetter = optionText.Trim().Split('.')[0].Trim();
        var userAnswers = new List<string> { chosenLetter };

        bool correct = CurrentQuestion.IsAnswerCorrect(userAnswers);
        IsCurrentAnswerCorrect = correct;

        if (correct)
        {
            FeedbackText = "🎉 回答正确！";
            _localStateService.RecordCorrectAnswer(CurrentQuestion.Id);
        }
        else
        {
            string correctStr = string.Join("", CurrentQuestion.CorrectAnswers);
            FeedbackText = $"❌ 回答错误。标准答案是：{correctStr}";
            _localStateService.RecordWrongAnswer(CurrentQuestion.Id);
        }
    }

    /// <summary>
    /// 多选题：切换某个选项的选中状态（勾选/取消）
    /// </summary>
    [RelayCommand]
    private void ToggleMultipleOption(string optionText)
    {
        if (IsAnswerRevealed || CurrentQuestion == null || string.IsNullOrEmpty(optionText))
            return;

        string letter = optionText.Trim().Split('.')[0].Trim();
        if (SelectedMultipleOptions.Contains(letter))
            SelectedMultipleOptions.Remove(letter);
        else
            SelectedMultipleOptions.Add(letter);
    }

    /// <summary>
    /// 多选题：提交答案
    /// </summary>
    [RelayCommand]
    private void SubmitMultipleAnswer()
    {
        if (IsAnswerRevealed || CurrentQuestion == null || SelectedMultipleOptions.Count == 0)
            return;

        IsAnswerRevealed = true;

        var userAnswers = SelectedMultipleOptions.ToList();
        bool correct = CurrentQuestion.IsAnswerCorrect(userAnswers);
        IsCurrentAnswerCorrect = correct;

        if (correct)
        {
            FeedbackText = "🎉 回答正确！";
            _localStateService.RecordCorrectAnswer(CurrentQuestion.Id);
        }
        else
        {
            string correctStr = string.Join("", CurrentQuestion.CorrectAnswers);
            string userStr = string.Join("", userAnswers);
            FeedbackText = $"❌ 回答错误。你的选择：{userStr}，标准答案是：{correctStr}";
            _localStateService.RecordWrongAnswer(CurrentQuestion.Id);
        }
    }

    [RelayCommand(CanExecute = nameof(CanGoPrevious))]
    private void Previous() => CurrentIndex--;
    private bool CanGoPrevious() => CurrentIndex > 0;

    [RelayCommand(CanExecute = nameof(CanGoNext))]
    private void Next() => CurrentIndex++;
    private bool CanGoNext() => CurrentIndex >= 0 && CurrentIndex < CurrentQuestions.Count - 1;
}
