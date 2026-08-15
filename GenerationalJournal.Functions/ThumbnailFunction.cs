using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats;
using SixLabors.ImageSharp.Formats.Gif;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.Processing;

namespace GenerationalJournal.Functions;

public class ThumbnailFunction
{
    private const int ThumbnailMaxDimension = 320;

    private static readonly HashSet<string> SupportedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".jpg", ".jpeg", ".png", ".gif"
    };

    private readonly ILogger<ThumbnailFunction> _logger;

    public ThumbnailFunction(ILogger<ThumbnailFunction> logger)
    {
        _logger = logger;
    }

    [Function(nameof(ThumbnailFunction))]
    [BlobOutput("thumbnails/{familyId}/{entryId}/{fileName}", Connection = "AzureWebJobsStorage")]
    public async Task<byte[]> GenerateAsync(
        [BlobTrigger("media/{familyId}/{entryId}/{fileName}", Connection = "AzureWebJobsStorage")] Stream sourceBlob,
        string familyId,
        string entryId,
        string fileName,
        FunctionContext context)
    {
        var extension = Path.GetExtension(fileName);
        if (!SupportedExtensions.Contains(extension))
        {
            _logger.LogInformation("Skipping non-image blob {FileName}.", fileName);
            return null!;
        }

        using var source = new MemoryStream();
        await sourceBlob.CopyToAsync(source);
        source.Position = 0;

        using var image = await Image.LoadAsync(source);

        image.Mutate(x => x.Resize(new ResizeOptions
        {
            Mode = ResizeMode.Max,
            Size = new Size(ThumbnailMaxDimension, ThumbnailMaxDimension)
        }));

        using var output = new MemoryStream();
        await image.SaveAsync(output, ResolveEncoder(extension));

        _logger.LogInformation(
            "Generated thumbnail for {FileName} ({FamilyId}/{EntryId}).",
            fileName, familyId, entryId);

        return output.ToArray();
    }

    private static IImageEncoder ResolveEncoder(string extension) =>
        extension.ToLowerInvariant() switch
        {
            ".png" => new PngEncoder(),
            ".gif" => new GifEncoder(),
            _ => new JpegEncoder()
        };
}
