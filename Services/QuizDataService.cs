using System.Text.Json;
using QuizNative.Models;

namespace QuizNative.Services;

public class QuizDataService : IQuizDataService
{
    private Dictionary<string, List<Question>> _chaptersData = new();
    private Dictionary<string, Question> _allQuestionsMap = new();

    private class NewQuestionJsonModel
    {
        public string id { get; set; } = string.Empty;
        public int chapter { get; set; }
        public string englishQuestion { get; set; } = string.Empty;
        public List<OptionJsonModel> options { get; set; } = new();
        public List<string> chineseOptions { get; set; } = new();
        public string chineseTranslation { get; set; } = string.Empty;
        public string explanation { get; set; } = string.Empty;
        public string answer { get; set; } = string.Empty;
        public string type { get; set; } = "single";
    }

    private class OptionJsonModel
    {
        public string label { get; set; } = string.Empty;
        public string text { get; set; } = string.Empty;
    }

    public Task LoadQuizAsync(string jsonContent)
    {
        var rawArray = JsonSerializer.Deserialize<List<NewQuestionJsonModel>>(jsonContent)
            ?? throw new InvalidOperationException("无法解析题目数据：JSON 格式不识别，期望根节点为数组。");

        if (rawArray.Count == 0)
            throw new InvalidOperationException("题库为空，JSON 数组中没有任何题目。");

        _chaptersData.Clear();
        _allQuestionsMap.Clear();

        var groups = rawArray.GroupBy(q => q.chapter);
        foreach (var group in groups)
        {
            string chapterId = group.Key.ToString();
            var questionList = new List<Question>();

            int index = 0;
            foreach (var rawQ in group)
            {
                string uniqueId = $"{chapterId}_{index}";

                // 构造英文选项列表
                var options = rawQ.options.Select(o => $"{o.label}. {o.text}").ToList();

                // 构造中文选项列表（与 options 一一对应）
                var chineseOptions = new List<string>();
                if (rawQ.chineseOptions != null && rawQ.chineseOptions.Count > 0)
                {
                    chineseOptions = rawQ.chineseOptions;
                }
                else
                {
                    // 如果没有 chineseOptions，用英文选项填充
                    chineseOptions = options.ToList();
                }

                var question = new Question
                {
                    Id = uniqueId,
                    ChapterId = chapterId,
                    Topic = rawQ.englishQuestion,
                    ChineseTranslation = rawQ.chineseTranslation,
                    Explanation = rawQ.explanation,
                    Type = rawQ.type,
                    Options = options,
                    ChineseOptions = chineseOptions,
                    CorrectAnswers = ParseAnswerToCorrectAnswers(rawQ.answer, rawQ.type)
                };

                questionList.Add(question);
                _allQuestionsMap[uniqueId] = question;
                index++;
            }

            _chaptersData[chapterId] = questionList;
        }

        return Task.CompletedTask;
    }

    /// <summary>
    /// 将答案字符串解析为正确答案列表。
    /// 例如 "AC" → ["A","C"]，"A" → ["A"]，"T" → ["T"]，"F" → ["F"]
    /// </summary>
    private List<string> ParseAnswerToCorrectAnswers(string answer, string type)
    {
        if (string.IsNullOrWhiteSpace(answer))
            return new List<string>();

        string trimmed = answer.Trim().ToUpperInvariant();

        if (type == "truefalse")
        {
            if (trimmed == "T" || trimmed == "(T)")
                return new List<string> { "T" };
            if (trimmed == "F" || trimmed == "(F)")
                return new List<string> { "F" };
        }

        // 去掉括号，如 (A) → A, (AC) → AC
        string clean = trimmed.Trim('(', ')', ' ');

        var result = new List<string>();
        foreach (char c in clean)
        {
            if (char.IsLetter(c))
                result.Add(c.ToString());
        }

        return result.Count > 0 ? result : new List<string> { clean };
    }

    public List<string> GetChapters() =>
        _chaptersData.Keys.OrderBy(k => int.TryParse(k, out var n) ? n : 0).Select(k => k.ToString()).ToList();

    public List<Question> GetQuestionsByChapter(string chapterId) =>
        _chaptersData.TryGetValue(chapterId, out var list) ? list : new List<Question>();

    public Question? GetQuestionById(string id) =>
        _allQuestionsMap.TryGetValue(id, out var q) ? q : null;
}
