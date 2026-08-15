namespace GenerationalJournal.Application.Services;

using GenerationalJournal.Application.Configuration;
using GenerationalJournal.Application.DTOs.Media;
using GenerationalJournal.Application.Storage;
using GenerationalJournal.Domain.Entities;
using GenerationalJournal.Domain.Repositories;
using Microsoft.Extensions.Options;

public class MediaService : IMediaService
{
    private static readonly string[] ImageExtensions = { ".jpg", ".png", ".gif" };

    private readonly IMediaRepository _mediaRepository;
    private readonly IJournalEntryRepository _journalEntryRepository;
    private readonly IFamilyRepository _familyRepository;
    private readonly IMediaStorage _mediaStorage;
    private readonly MediaSettings _settings;

    public MediaService(
        IMediaRepository mediaRepository,
        IJournalEntryRepository journalEntryRepository,
        IFamilyRepository familyRepository,
        IMediaStorage mediaStorage,
        IOptions<MediaSettings> settings)
    {
        _mediaRepository = mediaRepository;
        _journalEntryRepository = journalEntryRepository;
        _familyRepository = familyRepository;
        _mediaStorage = mediaStorage;
        _settings = settings.Value;
    }

    public async Task<MediaResponse> UploadMediaAsync(
        Guid entryId, Guid userId, string fileName, string contentType, long length, Stream content)
    {
        var entry = await _journalEntryRepository.GetByIdAsync(entryId)
            ?? throw new KeyNotFoundException("Journal entry not found.");

        await EnsureMemberAsync(entry.FamilyId, userId);

        var extension = System.IO.Path.GetExtension(fileName).ToLowerInvariant();
        if (!_settings.AllowedExtensions.Contains(extension))
        {
            throw new InvalidOperationException(
                $"File type '{extension}' is not allowed. Allowed types: {string.Join(", ", _settings.AllowedExtensions)}.");
        }

        if (length <= 0)
        {
            throw new InvalidOperationException("The uploaded file is empty.");
        }

        if (length > _settings.MaxFileSizeBytes)
        {
            throw new InvalidOperationException(
                $"File exceeds the maximum allowed size of {_settings.MaxFileSizeBytes / (1024 * 1024)} MB.");
        }

        var mediaType = ImageExtensions.Contains(extension) ? "image" : "video";
        var storedFileName = $"{Guid.NewGuid():N}{extension}";
        var relativePath = $"{entry.FamilyId}/{entry.Id}/{storedFileName}";

        var storagePath = await _mediaStorage.SaveAsync(relativePath, content);

        var mediaItem = new MediaItem
        {
            Id = Guid.NewGuid(),
            EntryId = entry.Id,
            FamilyId = entry.FamilyId,
            UploadedByUserId = userId,
            FileName = fileName,
            StoredFileName = storedFileName,
            ContentType = contentType,
            FileSizeBytes = length,
            MediaType = mediaType,
            StoragePath = storagePath,
            CreatedAt = DateTime.UtcNow
        };

        await _mediaRepository.CreateAsync(mediaItem);

        return MapMedia(mediaItem);
    }

    public async Task<List<MediaResponse>> GetMediaByEntryAsync(Guid entryId, Guid userId)
    {
        var entry = await _journalEntryRepository.GetByIdAsync(entryId)
            ?? throw new KeyNotFoundException("Journal entry not found.");

        await EnsureMemberAsync(entry.FamilyId, userId);

        var media = await _mediaRepository.GetByEntryIdAsync(entryId);
        return media.Select(MapMedia).ToList();
    }

    public async Task DeleteMediaAsync(Guid mediaId, Guid userId)
    {
        var mediaItem = await _mediaRepository.GetByIdAsync(mediaId)
            ?? throw new KeyNotFoundException("Media not found.");

        await EnsureMemberAsync(mediaItem.FamilyId, userId);

        await _mediaRepository.DeleteAsync(mediaItem);

        await _mediaStorage.DeleteAsync(mediaItem.StoragePath);
    }

    public async Task<(Stream Content, string ContentType, string FileName)> GetMediaFileAsync(Guid mediaId, Guid userId)
    {
        var mediaItem = await _mediaRepository.GetByIdAsync(mediaId)
            ?? throw new KeyNotFoundException("Media not found.");

        await EnsureMemberAsync(mediaItem.FamilyId, userId);

        var content = await _mediaStorage.GetAsync(mediaItem.StoragePath);

        return (content, mediaItem.ContentType, mediaItem.FileName);
    }

    private async Task EnsureMemberAsync(Guid familyId, Guid userId)
    {
        if (await _familyRepository.GetByIdAsync(familyId) is null)
        {
            throw new KeyNotFoundException("Family not found.");
        }

        if (await _familyRepository.GetMemberAsync(familyId, userId) is null)
        {
            throw new UnauthorizedAccessException("You are not a member of this family.");
        }
    }

    private static MediaResponse MapMedia(MediaItem mediaItem)
    {
        return new MediaResponse
        {
            Id = mediaItem.Id,
            EntryId = mediaItem.EntryId,
            FamilyId = mediaItem.FamilyId,
            FileName = mediaItem.FileName,
            ContentType = mediaItem.ContentType,
            FileSizeBytes = mediaItem.FileSizeBytes,
            MediaType = mediaItem.MediaType,
            Url = $"/api/media/{mediaItem.Id}/file",
            CreatedAt = mediaItem.CreatedAt
        };
    }
}
