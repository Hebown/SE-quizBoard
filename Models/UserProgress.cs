namespace QuizNative.Models;

/// <summary>
/// 用户刷题进度，用于持久化存储。
/// 升级版：精确记录每道题做对/做错次数，支持多维履历统计。
/// </summary>
public class UserProgress
{
    /// <summary>记录每道题做对的次数：Key = 题目ID, Value = 做对次数</summary>
    public Dictionary<string, int> CorrectCounts { get; set; } = new();

    /// <summary>
    /// 记录每道题做错的次数：Key = 题目ID, Value = 做错次数。
    /// 即错题本——只要存在于该字典中就是错题，移除即消灭。
    /// </summary>
    public Dictionary<string, int> WrongCounts { get; set; } = new();

    /// <summary>
    /// 兼容旧逻辑：只要在任一字典里出现过，就代表已刷过。
    /// （用于已刷题目去重统计，求并集）
    /// </summary>
    public HashSet<string> PracticedQuestionIds => new(CorrectCounts.Keys.Concat(WrongCounts.Keys));
}
