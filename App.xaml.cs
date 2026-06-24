using Microsoft.UI.Xaml;
using Microsoft.Extensions.DependencyInjection;
using QuizNative.Services;
using QuizNative.ViewModels;
using System;
using System.IO;
using System.Diagnostics;

namespace QuizNative;

public partial class App : Application
{
    public static IServiceProvider Services { get; private set; } = null!;
    public Window? MainWindow { get; private set; }

    public App()
    {
        this.InitializeComponent();

        // 配置依赖注入容器
        var services = new ServiceCollection();

        // 注入基础设施服务（单例模式）
        services.AddSingleton<IQuizDataService, QuizDataService>();
        services.AddSingleton<ILocalStateService, LocalStateService>();

        // 注入 ViewModel（瞬时模式：每次请求创建新实例）
        services.AddTransient<StandardViewModel>();
        services.AddTransient<WrongBookViewModel>();
        services.AddTransient<HistoryViewModel>();

        Services = services.BuildServiceProvider();
    }

    protected override async void OnLaunched(LaunchActivatedEventArgs args)
    {
        // 预先加载本地用户存档
        var localState = Services.GetRequiredService<ILocalStateService>();
        await localState.LoadProgressAsync();

        // 预先加载题库数据
        var quizService = Services.GetRequiredService<IQuizDataService>();
        await LoadQuizDataAsync(quizService);

        MainWindow = new MainWindow();
        MainWindow.Activate();
    }

    /// <summary>
    /// 从 exe 同目录加载 questions.json 题库文件
    /// </summary>
    private static async Task LoadQuizDataAsync(IQuizDataService quizService)
    {
        try
        {
            string jsonPath = Path.Combine(AppContext.BaseDirectory, "questions.json");

            if (File.Exists(jsonPath))
            {
                string json = await File.ReadAllTextAsync(jsonPath);
                await quizService.LoadQuizAsync(json);

                var chapters = quizService.GetChapters();
                int totalQuestions = 0;
                foreach (var ch in chapters)
                {
                    int count = quizService.GetQuestionsByChapter(ch).Count;
                    totalQuestions += count;
                    Debug.WriteLine($"  章节 {ch}: {count} 题");
                }
                Debug.WriteLine($"✅ 题库加载完成: 共 {chapters.Count} 个章节, {totalQuestions} 道题目");
            }
            else
            {
                Debug.WriteLine($"⚠️ 未找到题库文件: {jsonPath}");
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"❌ 加载题库失败: {ex.Message}");
        }
    }
}
