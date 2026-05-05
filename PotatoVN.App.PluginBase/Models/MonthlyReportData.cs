using System;
using System.Collections.Generic;
using System.Linq;
using GalgameManager.Enums;
using GalgameManager.Models;

namespace PotatoVN.App.PluginBase.Models;

public sealed class MonthlyReportData
{
    public int Year { get; init; }
    public int Month { get; init; }
    public int DaysInMonth { get; init; }
    public int TotalMinutes { get; init; }
    public int PlayedDays { get; init; }
    public int PlayedGameCount { get; init; }
    public int NewGamesCount { get; init; }
    public int LongestStreak { get; init; }
    public int CommentWordCount { get; init; }
    public double AverageRating { get; init; }
    public Galgame? FavoriteGame { get; init; }
    public int FavoriteGameMinutes { get; init; }
    public IReadOnlyList<DailyPlayData> DailyPlayData { get; init; } = [];
    public IReadOnlyList<WeekdayPlayData> WeekdayPlayData { get; init; } = [];
    public IReadOnlyList<GamePlayData> AllGames { get; init; } = [];
    public IReadOnlyList<GamePlayData> TopGames { get; init; } = [];
    public IReadOnlyList<PlayTypeData> PlayTypeData { get; init; } = [];

    public bool HasPlayRecord => TotalMinutes > 0;
    public string MonthTitle => $"{Year} 年 {Month} 月";
    public string TotalTimeText => FormatMinutes(TotalMinutes);
    public string PlayedDaysText => PlayedDays.ToString();
    public string PlayedGameCountText => PlayedGameCount.ToString();
    public string NewGamesCountText => NewGamesCount.ToString();
    public string AverageDailyTimeText => DaysInMonth == 0 ? "0 分钟" : FormatMinutes((int)Math.Round((double)TotalMinutes / DaysInMonth));
    public string FavoriteGameTimeText => FormatMinutes(FavoriteGameMinutes);
    public string AverageRatingText => AverageRating > 0 ? AverageRating.ToString("0.0") : "暂无";
    public string FavoriteGameShareText => TotalMinutes > 0 ? $"{FavoriteGameMinutes * 100.0 / TotalMinutes:0.#}%" : "0%";

    public static string FormatMinutes(int minutes)
    {
        if (minutes <= 0) return "0 分钟";
        int hours = minutes / 60;
        int remainingMinutes = minutes % 60;
        if (hours == 0) return $"{remainingMinutes} 分钟";
        return remainingMinutes == 0 ? $"{hours} 小时" : $"{hours} 小时 {remainingMinutes} 分钟";
    }
}

public sealed class DailyPlayData
{
    public int Day { get; init; }
    public int Minutes { get; init; }
    public double Ratio { get; init; }
    public string DayText => $"{Day} 日";
    public string TimeText => MonthlyReportData.FormatMinutes(Minutes);
    public double BarWidth => Math.Max(4, Ratio * 240);
}

public sealed class WeekdayPlayData
{
    public string Weekday { get; init; } = string.Empty;
    public int Minutes { get; init; }
    public double Ratio { get; init; }
    public string TimeText => MonthlyReportData.FormatMinutes(Minutes);
    public double BarWidth => Math.Max(4, Ratio * 200);
}

public sealed class GamePlayData
{
    public Galgame Game { get; init; } = new();
    public int Minutes { get; init; }
    public double Ratio { get; init; }
    public string Name => Game.Name.Value ?? Game.CnName ?? "未命名游戏";
    public string TimeText => MonthlyReportData.FormatMinutes(Minutes);
    public string ShareText => $"{Ratio * 100:0.#}%";
}

public sealed class PlayTypeData
{
    public PlayType PlayType { get; init; }
    public int Count { get; init; }
    public string Name => PlayType switch
    {
        PlayType.Playing => "正在玩",
        PlayType.Played => "已玩完",
        PlayType.Shelved => "搁置中",
        PlayType.Abandoned => "已放弃",
        PlayType.WantToPlay => "想玩",
        _ => "未分类"
    };
    public string CountText => $"{Count} 部";
}
