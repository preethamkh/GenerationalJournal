namespace GenerationalJournal.Application.Services;

using GenerationalJournal.Application.DTOs.Media;

public interface IMediaService
{
    Task<MediaResponse> UploadMediaAsync(
        Guid entryId, Guid userId, string fileName, string contentType, long length, Stream content);
    Task<List<MediaResponse>> GetMediaByEntryAsync(Guid entryId, Guid userId);
    Task DeleteMediaAsync(Guid mediaId, Guid userId);
    Task<(Stream Content, string ContentType, string FileName)> GetMediaFileAsync(Guid mediaId, Guid userId);
}
