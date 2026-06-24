namespace QuizNative.Models;

/// <summary>
/// 单道题目数据模型。
/// Id 格式为 "Chapter_Index"，如 "1_0" 表示第一章第0题，
/// 由 QuizDataService 在解析 JSON 时自动生成，保证全局唯一。
/// </summary>
public record Question
{
    /// <summary>自动生成的唯一标识，如 "1_0"（第一章第0题）</summary>
    public string Id { get; init; } = string.Empty;

    /// <summary>所属章节 ID，如 "1", "2"</summary>
    public string ChapterId { get; init; } = string.Empty;

    /// <summary>题目文本（英文，纯题干，不含选项）</summary>
    public string Topic { get; init; } = string.Empty;

    /// <summary>中文翻译（题干翻译）</summary>
    public string ChineseTranslation { get; init; } = string.Empty;

    /// <summary>中文解析/解释</summary>
    public string Explanation { get; init; } = string.Empty;

    /// <summary>题型：single（单选）、multiple（多选）、truefalse（判断）</summary>
    public string Type { get; init; } = "single";

    /// <summary>选项列表（英文），如 ["A. 选项一", "B. 选项二"]</summary>
    public List<string> Options { get; init; } = new();

    /// <summary>选项中文翻译列表，与 Options 一一对应</summary>
    public List<string> ChineseOptions { get; init; } = new();

    /// <summary>
    /// 所有正确选项的集合。
    /// - 单选题：包含1个元素，如 ["A"]
    /// - 多选题：包含多个元素，如 ["A", "C"]
    /// - 判断题：包含1个元素，如 ["T"] 或 ["F"]
    /// </summary>
    public List<string> CorrectAnswers { get; init; } = new();

    /// <summary>
    /// 判断用户选择的答案是否正确（支持多选比对）。
    /// 用户答案和正确答案均为无序比较。
    /// </summary>
    public bool IsAnswerCorrect(List<string> userAnswers)
    {
        if (userAnswers == null || userAnswers.Count == 0)
            return false;

        if (CorrectAnswers.Count == 0)
            return false;

        // 排序后逐个比较，忽略顺序
        var sortedCorrect = CorrectAnswers.OrderBy(a => a).ToList();
        var sortedUser = userAnswers.OrderBy(a => a).ToList();

        if (sortedCorrect.Count != sortedUser.Count)
            return false;

        for (int i = 0; i < sortedCorrect.Count; i++)
        {
            if (!string.Equals(sortedCorrect[i], sortedUser[i], StringComparison.OrdinalIgnoreCase))
                return false;
        }
        return true;
    }

    /// <summary>向后兼容：返回单个答案字符串（多选题取第一个）</summary>
    public string Answer => CorrectAnswers.Count > 0 ? CorrectAnswers[0] : string.Empty;
}
