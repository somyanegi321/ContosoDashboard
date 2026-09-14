using ContosoDashboard.Services;

namespace ContosoDashboard.Verification;

public static class DocumentFeatureVerification
{
    public static void VerifyRelativePathRejection(IFileStorageService storage)
    {
        try { storage.DeleteAsync("../outside").GetAwaiter().GetResult(); throw new InvalidOperationException("Traversal was accepted."); }
        catch (ArgumentException) { }
    }

    public static async Task VerifyFailClosedAsync(IMalwareScanner scanner)
    {
        var result = await scanner.ScanAsync(new MemoryStream([1]), "sample.pdf", "application/pdf");
        if (result is MalwareScanResult.Infected or MalwareScanResult.Unavailable) return;
        throw new InvalidOperationException("Scanner approved a non-clean result.");
    }
}
