using FilmAPI.DTO;

namespace FilmAPI.Services;

public interface IMediaService
{
    Task<MediaUploadResultDTO> UploadCoverAsync(IFormFile file);
}
