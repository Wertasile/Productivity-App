using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace ActualBlazorStandAloneAppSPA.Pages;

public partial class ProjectX : ComponentBase
{
    protected enum TimelineViewMode
    {
        Daily,
        Weekly,
        Monthly
    }

    protected sealed class TimelineHeaderUnit
    {
        public required string PrimaryLabel { get; init; }
        public required string SecondaryLabel { get; init; }
        public required int WidthPx { get; init; }
        public required bool ContainsToday { get; init; }
    }

    private static readonly string[] PhaseColors =
    {
        "#f59e0b",
        "#4f46e5",
        "#84cc16",
        "#ef4444",
        "#0ea5e9",
        "#ec4899"
    };

    protected TimelineViewMode ActiveTimelineView { get; set; } = TimelineViewMode.Daily;

    protected const int PhaseBarTop = 34;
    protected const int PhaseBarHeight = 4;
    protected const int PhaseHeaderHeight = 112;
    protected const int TaskRowHeight = 58;
    private const int TaskBarTopOffset = 18;
    protected ElementReference timelineBoardRef;
    private bool shouldScrollTimelineToToday;

    [Parameter]
    public string ProjectId { get; set; } = string.Empty;

    public Project? Project { get; set; }

    public bool IsVisible { get; set; }
    public Phase? SelectedPhase { get; set; }
    protected bool editDialogVisible;
    protected bool createPhaseDialogVisible;
    protected bool editPhaseDialogVisible;
    protected bool addPhaseTaskDialogVisible;

    private DateTime Today => DateTime.Today;

    protected DateTime TimelineStart => GetTimelineBounds().Start;

    private DateTime TimelineEnd => GetTimelineBounds().End;

    private int TimelineTotalDays => Math.Max(1, (TimelineEnd - TimelineStart).Days + 1);

    private int PixelsPerDay => ActiveTimelineView switch
    {
        TimelineViewMode.Daily => 36,
        TimelineViewMode.Weekly => 18,
        TimelineViewMode.Monthly => 6,
        _ => 18
    };

    private int TimelineCanvasWidth => TimelineTotalDays * PixelsPerDay;

    private int TodayOffset => GetOffset(Today);

    protected List<DateTime> TimelineDays =>
        Enumerable.Range(0, TimelineTotalDays)
            .Select(index => TimelineStart.AddDays(index))
            .ToList();

    protected List<TimelineHeaderUnit> TimelineHeaderUnits => BuildTimelineHeaderUnits();

    protected override async Task OnInitializedAsync()
    {
        await LoadProjectDetailsAsync();
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (!shouldScrollTimelineToToday)
        {
            return;
        }

        shouldScrollTimelineToToday = false;
        await JS.InvokeVoidAsync("projectTimeline.scrollToOffset", timelineBoardRef, TodayOffset, 0);
    }

    protected async Task LoadProjectDetailsAsync()
    {
        Project = await ProjectService.GetProjectAsync(ProjectId);
        shouldScrollTimelineToToday = Project?.Phases.Count > 0;
        StateHasChanged();
    }

    protected void SetTimelineView(TimelineViewMode mode)
    {
        if (ActiveTimelineView == mode)
        {
            return;
        }

        ActiveTimelineView = mode;
        shouldScrollTimelineToToday = Project?.Phases.Count > 0;
    }

    protected void OpenCreatePhaseDialogAsync()
    {
        createPhaseDialogVisible = true;
    }

    protected async Task OnCreatePhaseDialogClosedAsync()
    {
        createPhaseDialogVisible = false;
        await LoadProjectDetailsAsync();
    }

    protected void OpenEditDialogAsync()
    {
        editDialogVisible = true;
    }

    protected async Task OnEditDialogClosedAsync()
    {
        editDialogVisible = false;
        await LoadProjectDetailsAsync();
    }

    protected void OpenPhaseEditor(Phase phase)
    {
        SelectedPhase = phase;
        OpenEditPhaseDialogAsync();
    }

    protected void OpenEditPhaseDialogAsync()
    {
        editPhaseDialogVisible = true;
    }

    protected void OpenAddPhaseTaskDialogAsync(Phase phase)
    {
        SelectedPhase = phase;
        addPhaseTaskDialogVisible = true;
    }

    protected async Task OnAddPhaseTaskDialogClosedAsync()
    {
        addPhaseTaskDialogVisible = false;
        await LoadProjectDetailsAsync();
    }

    protected async Task ScrollTimelineLeftAsync()
    {
        await JS.InvokeVoidAsync("projectTimeline.scrollByViewport", timelineBoardRef, -1, 0.72);
    }

    protected async Task ScrollTimelineRightAsync()
    {
        await JS.InvokeVoidAsync("projectTimeline.scrollByViewport", timelineBoardRef, 1, 0.72);
    }

    protected async Task OnEditPhaseDialogClosedAsync()
    {
        editPhaseDialogVisible = false;
        await LoadProjectDetailsAsync();
    }

    private (DateTime Start, DateTime End) GetTimelineBounds()
    {
        if (Project is null)
        {
            return (Today.AddDays(-30), Today.AddDays(120));
        }

        var dates = new List<DateTime> { Project.Start.Date, Project.End.Date, Today };

        foreach (var phase in Project.Phases)
        {
            dates.Add(phase.Start.Date);
            dates.Add(phase.End.Date);

            foreach (var task in phase.Tasks)
            {
                dates.Add(GetTaskTimelineStart(task, phase));
                dates.Add(GetTaskTimelineEnd(task, phase));
            }
        }

        var start = dates.Min().AddDays(-14);
        var end = dates.Max().AddDays(45);

        return (start.Date, end.Date);
    }

    private int GetOffset(DateTime date)
    {
        return (int)(date.Date - TimelineStart).TotalDays * PixelsPerDay;
    }

    private int GetWidth(DateTime start, DateTime end)
    {
        var totalDays = Math.Max(1, (end.Date - start.Date).Days + 1);
        return totalDays * PixelsPerDay;
    }

    protected int GetTrackHeight(Phase phase)
    {
        var taskRows = Math.Max(1, phase.Tasks?.Count ?? 0);
        return PhaseHeaderHeight + (taskRows * TaskRowHeight);
    }

    protected string GetPhaseColor(int index)
    {
        return PhaseColors[index % PhaseColors.Length];
    }

    protected string GetPhaseStyle(Phase phase, int index)
    {
        var left = GetOffset(phase.Start);
        var width = GetWidth(phase.Start, phase.End);
        var color = GetPhaseColor(index);

        return $"left:{left}px; top:{PhaseBarTop}px; width:{width}px; height:{PhaseBarHeight}px; background:{color};";
    }

    protected string GetTaskStyle(TaskItem task, Phase phase, string phaseColor, int taskIndex)
    {
        var start = GetTaskTimelineStart(task, phase);
        var end = GetTaskTimelineEnd(task, phase);
        var left = GetOffset(start);
        var width = GetWidth(start, end);
        var top = PhaseHeaderHeight + (taskIndex * TaskRowHeight) + TaskBarTopOffset;

        return $"left:{left}px; top:{top}px; width:{width}px; background:{phaseColor};";
    }

    protected string GetHeaderTrackStyle()
    {
        return $"width:{TimelineCanvasWidth}px;";
    }

    protected string GetHeaderUnitStyle(int widthPx)
    {
        return $"width:{widthPx}px; min-width:{widthPx}px;";
    }

    protected string GetMinHeightStyle(int height)
    {
        return $"min-height:{height}px;";
    }

    protected string GetTrackCanvasStyle(int trackHeight, string phaseColor)
    {
        return $"width:{TimelineCanvasWidth}px; min-height:{trackHeight}px; --phase-color:{phaseColor}; --day-width:{PixelsPerDay}px;";
    }

    protected string GetTodayMarkerStyle()
    {
        return $"left:{TodayOffset}px;";
    }

    private List<TimelineHeaderUnit> BuildTimelineHeaderUnits()
    {
        return ActiveTimelineView switch
        {
            TimelineViewMode.Daily => BuildDailyHeaderUnits(),
            TimelineViewMode.Weekly => BuildWeeklyHeaderUnits(),
            TimelineViewMode.Monthly => BuildMonthlyHeaderUnits(),
            _ => BuildDailyHeaderUnits()
        };
    }

    private List<TimelineHeaderUnit> BuildDailyHeaderUnits()
    {
        return TimelineDays
            .Select(day => new TimelineHeaderUnit
            {
                PrimaryLabel = day.ToString("ddd").ToUpperInvariant(),
                SecondaryLabel = day.ToString("dd MMM"),
                WidthPx = PixelsPerDay,
                ContainsToday = day.Date == Today
            })
            .ToList();
    }

    private List<TimelineHeaderUnit> BuildWeeklyHeaderUnits()
    {
        var units = new List<TimelineHeaderUnit>();
        var cursor = TimelineStart;

        while (cursor <= TimelineEnd)
        {
            var unitEnd = cursor.AddDays(6);
            if (unitEnd > TimelineEnd)
            {
                unitEnd = TimelineEnd;
            }

            var weekOfMonth = ((cursor.Day - 1) / 7) + 1;
            units.Add(new TimelineHeaderUnit
            {
                PrimaryLabel = $"W{weekOfMonth}",
                SecondaryLabel = cursor.ToString("MMM yyyy"),
                WidthPx = GetWidth(cursor, unitEnd),
                ContainsToday = Today >= cursor && Today <= unitEnd
            });

            cursor = unitEnd.AddDays(1);
        }

        return units;
    }

    private List<TimelineHeaderUnit> BuildMonthlyHeaderUnits()
    {
        var units = new List<TimelineHeaderUnit>();
        var cursor = new DateTime(TimelineStart.Year, TimelineStart.Month, 1);

        while (cursor <= TimelineEnd)
        {
            var monthEnd = cursor.AddMonths(1).AddDays(-1);
            var clippedStart = cursor < TimelineStart ? TimelineStart : cursor;
            var clippedEnd = monthEnd > TimelineEnd ? TimelineEnd : monthEnd;

            if (clippedEnd >= clippedStart)
            {
                units.Add(new TimelineHeaderUnit
                {
                    PrimaryLabel = cursor.ToString("MMM").ToUpperInvariant(),
                    SecondaryLabel = cursor.ToString("yyyy"),
                    WidthPx = GetWidth(clippedStart, clippedEnd),
                    ContainsToday = Today >= clippedStart && Today <= clippedEnd
                });
            }

            cursor = cursor.AddMonths(1);
        }

        return units;
    }

    private DateTime GetTaskTimelineStart(TaskItem task, Phase phase)
    {
        var start = NormalizeTimelineDate(task.Start);
        if (start.HasValue)
        {
            return start.Value;
        }

        var end = NormalizeTimelineDate(task.End);
        return end ?? phase.Start.Date;
    }

    private DateTime GetTaskTimelineEnd(TaskItem task, Phase phase)
    {
        var start = GetTaskTimelineStart(task, phase);
        var end = NormalizeTimelineDate(task.End);

        if (!end.HasValue)
        {
            return start;
        }

        if (end.Value >= start)
        {
            return end.Value;
        }

        return start;
    }

    private DateTime? NormalizeTimelineDate(DateTime value)
    {
        if (value.Year > 1900)
        {
            return value.Date;
        }

        return null;
    }

    protected string FormatDateRange(DateTime start, DateTime end)
    {
        return $"{start:MMM dd, yyyy} - {end:MMM dd, yyyy}";
    }

    protected string FormatTaskDateRange(TaskItem task, Phase phase)
    {
        var start = GetTaskTimelineStart(task, phase);
        var end = GetTaskTimelineEnd(task, phase);
        return FormatDateRange(start, end);
    }

    protected bool IsToday(DateTime day)
    {
        return day.Date == Today;
    }

    protected string GetTaskStatusClass(string? status)
    {
        return status?.Trim().ToLowerInvariant() switch
        {
            "completed" => "is-completed",
            "in progress" => "is-in-progress",
            _ => "is-not-started"
        };
    }

    protected string GetTaskStatusLabel(string? status)
    {
        return string.IsNullOrWhiteSpace(status) ? "Not Started" : status;
    }

    protected string GetCompletionPercentageText(int completedTaskCount, int totalTaskCount)
    {
        if (totalTaskCount == 0)
        {
            return "0%";
        }

        var percentage = (int)Math.Round((double)completedTaskCount / totalTaskCount * 100, MidpointRounding.AwayFromZero);
        return $"{percentage}%";
    }
}