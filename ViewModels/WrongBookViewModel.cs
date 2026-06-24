using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using QuizNative.Models;
using QuizNative.Services;

namespace QuizNative.ViewModels;

public partial class WrongBookViewModel : ObservableObject
{
    private readonly IQuizDataService _quizDataService;
    private readonly ILocalStateService _localStateService;

    public ObservableCollection<Question> WrongQuestions { get; } = new();

    [ObservableProperty]
    private Question? _currentQuestion;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(NextCommand))]
    private int _currentIndex = -1;

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

    public ObservableCollection<string> SingleChoiceOptions { get; } = new();
    public ObservableCollection<string> MultipleChoiceOptions { get; } = new();

    public bool IsMultipleChoice => CurrentQuestion?.Type == "multiple";
    public bool IsTrueFalse => CurrentQuestion?.Type == "truefalse";
    public bool IsSingleChoice => CurrentQuestion?.Type == "single";

    public int TotalQuestions => WrongQuestions.Count;
    public int CurrentQuestionNumber => CurrentIndex + 1;
    public bool HasWrongQuestions => WrongQuestions.Count > 0;
    public bool CanNext => CurrentIndex >= 0 && CurrentIndex < WrongQuestions.Count - 1;

    public WrongBookViewModel(IQuizDataService quizDataService, ILocalStateService localStateService)
    {
        _quizDataService = quizDataService;
        _localStateService = localStateService;
        RefreshWrongQuestions();
    }

    public void RefreshWrongQuestions()
    {
        WrongQuestions.Clear();
        var wrongIds = _localStateService.Progress.WrongCounts.Keys;
        foreach (var id in wrongIds)
        {
            var q = _quizDataService.GetQuestionById(id);
            if (q != null) WrongQuestions.Add(q);
        }

        OnPropertyChanged(nameof(HasWrongQuestions));
        OnPropertyChanged(nameof(TotalQuestions));

        CurrentIndex = WrongQuestions.Count > 0 ? 0 : -1;
    }

    partial void OnCurrentIndexChanged(int value)
    {
        if (value >= 0 && value < WrongQuestions.Count)
        {
            CurrentQuestion = WrongQuestions[value];
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
            else
            {
                foreach (var opt in CurrentQuestion.Options)
                    SingleChoiceOptions.Add(opt);
            }
        }
    }

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
            FeedbackText = "🎉 太棒了，这次做对了！该题已从错题本中移除。";
            _localStateService.RecordCorrectAnswer(CurrentQuestion.Id);
            _localStateService.RemoveFromWrongQuestions(CurrentQuestion.Id);

            int nextIndex = CurrentIndex;
            RefreshWrongQuestions();
            if (WrongQuestions.Count > 0 && nextIndex < WrongQuestions.Count)
                CurrentIndex = nextIndex;
        }
        else
        {
            string correctStr = string.Join("", CurrentQuestion.CorrectAnswers);
            FeedbackText = $"❌ 仍然错误。标准答案是：{correctStr}。继续加油！";
        }
    }

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
            FeedbackText = "🎉 太棒了，这次做对了！该题已从错题本中移除。";
            _localStateService.RecordCorrectAnswer(CurrentQuestion.Id);
            _localStateService.RemoveFromWrongQuestions(CurrentQuestion.Id);

            int nextIndex = CurrentIndex;
            RefreshWrongQuestions();
            if (WrongQuestions.Count > 0 && nextIndex < WrongQuestions.Count)
                CurrentIndex = nextIndex;
        }
        else
        {
            string correctStr = string.Join("", CurrentQuestion.CorrectAnswers);
            string userStr = string.Join("", userAnswers);
            FeedbackText = $"❌ 仍然错误。你的选择：{userStr}，标准答案是：{correctStr}。继续加油！";
        }
    }

    [RelayCommand(CanExecute = nameof(CanNext))]
    private void Next() => CurrentIndex++;
}
