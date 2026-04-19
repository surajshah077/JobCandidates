namespace JobCandidates.DTOs
{
    public class AnalyticsSummaryDTO
    {
        public int TotalJobs { get; set; }
        public int TotalCandidates { get; set; }
        public int TotalApplications { get; set; }
        public int TotalInterviews { get; set; }
    }

    public class ApplicationStatusCountDTO
    {
        public string Status { get; set; } = string.Empty;
        public int Count { get; set; }
    }

    public class JobApplicationCountDTO
    {
        public int JobId { get; set; }
        public string JobTitle { get; set; } = string.Empty;
        public int ApplicationCount { get; set; }
    }
}