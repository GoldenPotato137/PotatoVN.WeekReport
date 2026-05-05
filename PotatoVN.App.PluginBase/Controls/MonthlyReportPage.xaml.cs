using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using GalgameManager.Enums;
using GalgameManager.Models;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using PotatoVN.App.PluginBase.Models;

namespace PotatoVN.App.PluginBase.Controls;

public sealed partial class MonthlyReportPage : Page
{
    private static readonly string[] WeekdayNames = ["周日", "周一", "周二", "周三", "周四", "周五", "周六"];
    private DateTime _selectedMonth = new(DateTime.Now.Year, DateTime.Now.Month, 1);
    private bool _isLoading = true;

    public MonthlyReportData Report { get; private set; } = CreateEmptyReport(DateTime.Now.Year, DateTime.Now.Month);
    public Visibility LoadingVisibility => _isLoading ? Visibility.Visible : Visibility.Collapsed;
    public Visibility ContentVisibility => _isLoading ? Visibility.Collapsed : Visibility.Visible;
    public Visibility EmptyVisibility => !_isLoading && !Report.HasPlayRecord ? Visibility.Visible : Visibility.Collapsed;
    public Visibility ReportContentVisibility => !_isLoading && Report.HasPlayRecord ? Visibility.Visible : Visibility.Collapsed;
    public string FavoriteGameName => Report.FavoriteGame?.Name.Value ?? Report.FavoriteGame?.CnName ?? "暂无";
    public string FavoriteGameDescription => $"本月游玩 {Report.FavoriteGameTimeText}，占总时长 {Report.FavoriteGameShareText}";
    public double FavoriteGameShareValue => Report.TotalMinutes > 0 ? Report.FavoriteGameMinutes * 100.0 / Report.TotalMinutes : 0;
    public string HabitSummary => $"本月游玩 {Report.PlayedDays} 天，最长连续游玩 {Report.LongestStreak} 天。";
    public string AppreciationSummary => $"本月玩过的游戏平均评分：{Report.AverageRatingText}；吐槽字数：{Report.CommentWordCount}。";

    public MonthlyReportPage()
    {
        XamlResourceLocatorFactory.PluginControlInit(ref _contentLoaded, this);
        Loaded += MonthlyReportPage_Loaded;
    }

    private async void MonthlyReportPage_Loaded(object sender, RoutedEventArgs e)
    {
        Loaded -= MonthlyReportPage_Loaded;
        await RefreshReportAsync();
    }

    private async void PreviousMonthButton_OnClick(object sender, RoutedEventArgs e)
    {
        _selectedMonth = _selectedMonth.AddMonths(-1);
        await RefreshReportAsync();
    }

    private async void NextMonthButton_OnClick(object sender, RoutedEventArgs e)
    {
        _selectedMonth = _selectedMonth.AddMonths(1);
        await RefreshReportAsync();
    }

    private async void RefreshButton_OnClick(object sender, RoutedEventArgs e)
    {
        await RefreshReportAsync();
    }

    private async Task RefreshReportAsync()
    {
        _isLoading = true;
        Bindings.Update();

        int year = _selectedMonth.Year;
        int month = _selectedMonth.Month;
        List<Galgame> games = Plugin.HostApi.GetAllGames();
        Report = await Task.Run(() => BuildReport(games, year, month));

        _isLoading = false;
        Bindings.Update();
    }

    private static MonthlyReportData BuildReport(IReadOnlyList<Galgame> games, int year, int month)
    {
        int daysInMonth = DateTime.DaysInMonth(year, month);
        var dailyMinutes = new int[daysInMonth];
        var weekdayMinutes = new int[7];
        var gameMinutes = new Dictionary<Galgame, int>();
        var playTypeCounts = new Dictionary<PlayType, int>();
        var playedDates = new HashSet<DateTime>();
        int newGamesCount = 0;
        int commentWordCount = 0;
        int ratingCount = 0;
        double ratingTotal = 0;

        foreach (Galgame game in games)
        {
            if (game.AddTime.Year == year && game.AddTime.Month == month)
                newGamesCount++;

            int minutesInMonth = 0;
            foreach (KeyValuePair<string, int> playedTime in game.PlayedTime)
            {
                if (!TryParsePlayedDate(playedTime.Key, out DateTime date)) continue;
                if (date.Year != year || date.Month != month) continue;

                int minutes = Math.Max(0, playedTime.Value);
                if (minutes == 0) continue;

                minutesInMonth += minutes;
                dailyMinutes[date.Day - 1] += minutes;
                weekdayMinutes[(int)date.DayOfWeek] += minutes;
                playedDates.Add(date.Date);
            }

            if (minutesInMonth <= 0) continue;

            gameMinutes[game] = minutesInMonth;
            if (!playTypeCounts.TryAdd(game.PlayType, 1))
                playTypeCounts[game.PlayType]++;

            if (game.MyRate > 0)
            {
                ratingTotal += game.MyRate;
                ratingCount++;
            }

            if (!string.IsNullOrEmpty(game.Comment))
                commentWordCount += game.Comment.Length;
        }

        int totalMinutes = dailyMinutes.Sum();
        int maxDailyMinutes = dailyMinutes.Length == 0 ? 0 : dailyMinutes.Max();
        int maxWeekdayMinutes = weekdayMinutes.Length == 0 ? 0 : weekdayMinutes.Max();
        Galgame? favoriteGame = null;
        int favoriteGameMinutes = 0;
        if (gameMinutes.Count > 0)
        {
            KeyValuePair<Galgame, int> favorite = gameMinutes.MaxBy(pair => pair.Value);
            favoriteGame = favorite.Key;
            favoriteGameMinutes = favorite.Value;
        }

        List<GamePlayData> allGames = gameMinutes
            .OrderByDescending(pair => pair.Value)
            .Select(pair => new GamePlayData
            {
                Game = pair.Key,
                Minutes = pair.Value,
                Ratio = totalMinutes == 0 ? 0 : (double)pair.Value / totalMinutes
            })
            .ToList();

        return new MonthlyReportData
        {
            Year = year,
            Month = month,
            DaysInMonth = daysInMonth,
            TotalMinutes = totalMinutes,
            PlayedDays = playedDates.Count,
            PlayedGameCount = gameMinutes.Count,
            NewGamesCount = newGamesCount,
            LongestStreak = CalculateLongestStreak(playedDates),
            CommentWordCount = commentWordCount,
            AverageRating = ratingCount == 0 ? 0 : ratingTotal / ratingCount,
            FavoriteGame = favoriteGame,
            FavoriteGameMinutes = favoriteGameMinutes,
            DailyPlayData = Enumerable.Range(1, daysInMonth)
                .Select(day => new DailyPlayData
                {
                    Day = day,
                    Minutes = dailyMinutes[day - 1],
                    Ratio = maxDailyMinutes == 0 ? 0 : (double)dailyMinutes[day - 1] / maxDailyMinutes
                })
                .ToList(),
            WeekdayPlayData = Enumerable.Range(0, 7)
                .Select(day => new WeekdayPlayData
                {
                    Weekday = WeekdayNames[day],
                    Minutes = weekdayMinutes[day],
                    Ratio = maxWeekdayMinutes == 0 ? 0 : (double)weekdayMinutes[day] / maxWeekdayMinutes
                })
                .ToList(),
            AllGames = allGames,
            TopGames = allGames.Take(10).ToList(),
            PlayTypeData = playTypeCounts
                .OrderByDescending(pair => pair.Value)
                .Select(pair => new PlayTypeData
                {
                    PlayType = pair.Key,
                    Count = pair.Value
                })
                .ToList()
        };
    }

    private static int CalculateLongestStreak(HashSet<DateTime> playedDates)
    {
        if (playedDates.Count == 0) return 0;

        List<DateTime> dates = playedDates.OrderBy(date => date).ToList();
        int longest = 1;
        int current = 1;
        for (int i = 1; i < dates.Count; i++)
        {
            if ((dates[i] - dates[i - 1]).Days == 1)
            {
                current++;
                longest = Math.Max(longest, current);
            }
            else
            {
                current = 1;
            }
        }

        return longest;
    }

    private static bool TryParsePlayedDate(string value, out DateTime date)
    {
        if (DateTime.TryParse(value, CultureInfo.CurrentCulture, DateTimeStyles.None, out date)) return true;
        if (DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.None, out date)) return true;
        return DateTime.TryParse(value, CultureInfo.GetCultureInfo("zh-CN"), DateTimeStyles.None, out date);
    }

    private static MonthlyReportData CreateEmptyReport(int year, int month)
    {
        int daysInMonth = DateTime.DaysInMonth(year, month);
        return new MonthlyReportData
        {
            Year = year,
            Month = month,
            DaysInMonth = daysInMonth,
            DailyPlayData = Enumerable.Range(1, daysInMonth)
                .Select(day => new DailyPlayData { Day = day })
                .ToList(),
            WeekdayPlayData = Enumerable.Range(0, 7)
                .Select(day => new WeekdayPlayData { Weekday = WeekdayNames[day] })
                .ToList()
        };
    }
}
