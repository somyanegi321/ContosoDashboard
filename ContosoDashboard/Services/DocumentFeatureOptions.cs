namespace ContosoDashboard.Services;

public sealed class DocumentFeatureOptions
{
    public const string SectionName = "Documents";
    public string StorageRoot { get; set; } = "AppData/uploads";
    public long MaximumFileSize { get; set; } = 25L * 1024 * 1024;
    public string ScannerMode { get; set; } = "FailClosed";

    public static readonly string[] Categories =
    ["Project Documents", "Team Resources", "Personal Files", "Reports", "Presentations", "Other"];

    public static readonly IReadOnlyDictionary<string, string[]> AllowedMimeTypes =
        new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
        {
            [".pdf"] = ["application/pdf"],
            [".doc"] = ["application/msword"],
            [".docx"] = ["application/vnd.openxmlformats-officedocument.wordprocessingml.document"],
            [".xls"] = ["application/vnd.ms-excel"],
            [".xlsx"] = ["application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"],
            [".ppt"] = ["application/vnd.ms-powerpoint"],
            [".pptx"] = ["application/vnd.openxmlformats-officedocument.presentationml.presentation"],
            [".txt"] = ["text/plain"],
            [".jpg"] = ["image/jpeg"],
            [".jpeg"] = ["image/jpeg"],
            [".png"] = ["image/png"]
        };

    public static readonly string[] ActivityActions =
        ["Upload", "Download", "Preview", "UpdateMetadata", "Replace", "Delete", "Share", "Denied"];
}
