using System.Text.Json;
using QuizNative.Models;

namespace QuizNative.Services;

public class QuizDataService : IQuizDataService
{
    private Dictionary<string, List<Question>> _chaptersData = new();
    private Dictionary<string, Question> _allQuestionsMap = new();

    // 内部辅助类，映射原始 JSON
    private class QuestionJsonModel
    {
        public string topic { get; set; } = string.Empty;
        public string answer { get; set; } = string.Empty;
        public List<string> options { get; set; } = new();
    }

    public Task LoadQuizAsync(string jsonContent)
    {
        var rawData = JsonSerializer.Deserialize<Dictionary<string, List<QuestionJsonModel>>>(jsonContent);

        _chaptersData.Clear();
        _allQuestionsMap.Clear();

        if (rawData != null)
        {
            foreach (var kvp in rawData)
            {
                string chapterId = kvp.Key;
                var questionList = new List<Question>();

                for (int i = 0; i < kvp.Value.Count; i++)
                {
                    var rawQ = kvp.Value[i];
                    string uniqueId = $"{chapterId}_{i}"; // 生成形如 "1_0" 的唯一ID

                    var question = new Question
                    {
                        Id = uniqueId,
                        ChapterId = chapterId,
                        Topic = rawQ.topic,
                        Answer = rawQ.answer,
                        Options = rawQ.options
                    };

                    questionList.Add(question);
                    _allQuestionsMap[uniqueId] = question;
                }

                _chaptersData[chapterId] = questionList;
            }
        }
        return Task.CompletedTask;
    }

    public List<string> GetChapters() =>
        _chaptersData.Keys.OrderBy(k => int.TryParse(k, out var n) ? n : 0).Select(k => k.ToString()).ToList();

    public List<Question> GetQuestionsByChapter(string chapterId) =>
        _chaptersData.TryGetValue(chapterId, out var list) ? list : new List<Question>();

    public Question? GetQuestionById(string id) =>
        _allQuestionsMap.TryGetValue(id, out var q) ? q : null;
}
