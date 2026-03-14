namespace Expense_Tracker.Models
{
    public class ReportMenuItem
    {
        public string Key { get; set; } = string.Empty;
        public string Label { get; set; } = string.Empty;
    }

    public class ReportTableData
    {
        public string Key { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public IReadOnlyList<string> Headers { get; set; } = Array.Empty<string>();
        public IReadOnlyList<IReadOnlyList<string>> Rows { get; set; } = Array.Empty<IReadOnlyList<string>>();
    }

    public class ReportViewModel
    {
        public IReadOnlyList<ReportMenuItem> Reports { get; set; } = Array.Empty<ReportMenuItem>();
        public string SelectedReportType { get; set; } = string.Empty;
        public string SelectedReportTitle { get; set; } = string.Empty;
        public IReadOnlyList<string> Headers { get; set; } = Array.Empty<string>();
        public IReadOnlyList<IReadOnlyList<string>> Rows { get; set; } = Array.Empty<IReadOnlyList<string>>();
    }
}
