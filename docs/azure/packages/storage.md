# MonadicSharp.Azure.Storage

`MonadicSharp.Azure.Storage` wraps the Azure Blob Storage SDK. Upload, download, delete, and metadata operations all return `Result<T>` or `Option<T>`.

## Install

```bash
dotnet add package MonadicSharp.Azure.Storage
```

## Methods

```csharp
// Upload — returns Result<BlobContentInfo>
Task<Result<BlobContentInfo>> UploadBlobMonadicAsync(
    this BlobContainerClient container,
    string blobName,
    Stream content,
    BlobUploadOptions? options = null,
    CancellationToken ct = default);

// Download — returns Option<BlobDownloadResult> (None when blob does not exist)
Task<Option<BlobDownloadResult>> DownloadBlobMonadicAsync(
    this BlobContainerClient container,
    string blobName,
    CancellationToken ct = default);

// Download to stream
Task<Result<Unit>> DownloadToStreamMonadicAsync(
    this BlobContainerClient container,
    string blobName,
    Stream destination,
    CancellationToken ct = default);

// Delete
Task<Result<Unit>> DeleteBlobMonadicAsync(
    this BlobContainerClient container,
    string blobName,
    DeleteSnapshotsOption snapshots = DeleteSnapshotsOption.None,
    CancellationToken ct = default);

// Check existence
Task<Result<bool>> ExistsBlobMonadicAsync(
    this BlobContainerClient container,
    string blobName,
    CancellationToken ct = default);

// Get properties
Task<Option<BlobProperties>> GetBlobPropertiesMonadicAsync(
    this BlobContainerClient container,
    string blobName,
    CancellationToken ct = default);
```

## Upload with content-type validation

```csharp
public class ImageStorageService(BlobContainerClient container)
{
    private static readonly HashSet<string> AllowedTypes =
        ["image/jpeg", "image/png", "image/webp"];

    public async Task<Result<Uri>> UploadImageAsync(
        IFormFile file,
        Guid userId,
        CancellationToken ct)
    {
        if (!AllowedTypes.Contains(file.ContentType))
            return Result.Failure<Uri>(
                Error.Validation("File.ContentType",
                    $"Content type '{file.ContentType}' is not allowed. Use: {string.Join(", ", AllowedTypes)}"));

        if (file.Length > 5 * 1024 * 1024)
            return Result.Failure<Uri>(
                Error.Validation("File.Size", "File exceeds the 5 MB limit."));

        var blobName = $"users/{userId}/avatars/{Guid.NewGuid()}{Path.GetExtension(file.FileName)}";

        await using var stream = file.OpenReadStream();

        var uploadResult = await container.UploadBlobMonadicAsync(
            blobName,
            stream,
            new BlobUploadOptions
            {
                HttpHeaders = new BlobHttpHeaders { ContentType = file.ContentType }
            },
            ct);

        return uploadResult.Map(_ => container.GetBlobClient(blobName).Uri);
    }
}
```

## Download — Option\<T\> for missing blobs

`DownloadBlobMonadicAsync` returns `Option<BlobDownloadResult>` because a missing blob is a valid state, not an error.

```csharp
public async Task<IActionResult> GetAvatar(Guid userId, CancellationToken ct)
{
    var blobName = $"users/{userId}/avatars/current.jpg";

    var download = await container.DownloadBlobMonadicAsync(blobName, ct);

    return download.Match(
        onSome: blob => File(blob.Content.ToStream(), blob.Details.ContentType),
        onNone: ()   => (IActionResult)NotFound());
}
```

## Streaming large files

For files too large to buffer in memory, stream directly to the HTTP response:

```csharp
public async Task<IActionResult> DownloadReport(
    string reportId,
    HttpResponse response,
    CancellationToken ct)
{
    var blobName = $"reports/{reportId}.csv";

    var exists = await container.ExistsBlobMonadicAsync(blobName, ct);
    if (!exists.IsSuccess || !exists.Value)
        return NotFound();

    response.ContentType = "text/csv";
    response.Headers.ContentDisposition = $"attachment; filename=\"{reportId}.csv\"";

    var result = await container.DownloadToStreamMonadicAsync(blobName, response.Body, ct);

    return result.Match(
        onSuccess: _ => new EmptyResult(),
        onFailure: e => StatusCode(500, e.Message));
}
```

## Delete with existence check

```csharp
public async Task<Result<Unit>> DeleteAvatarAsync(Guid userId, CancellationToken ct)
{
    var blobName = $"users/{userId}/avatars/current.jpg";

    return await container.DeleteBlobMonadicAsync(blobName, ct);
    // Returns Result.Failure(AzureError.NotFound) if blob does not exist
    // Returns Result.Success(Unit.Value) on deletion
}
```

## Generating SAS URLs

```csharp
public Result<Uri> GenerateDownloadUrl(string blobName, TimeSpan validity)
{
    var blobClient = container.GetBlobClient(blobName);

    if (!blobClient.CanGenerateSasUri)
        return Result.Failure<Uri>(Error.Configuration(
            "Storage.SAS", "Container client was not initialised with StorageSharedKeyCredential."));

    var sasUri = blobClient.GenerateSasUri(BlobSasPermissions.Read, DateTimeOffset.UtcNow.Add(validity));

    return Result.Success(sasUri);
}
```
