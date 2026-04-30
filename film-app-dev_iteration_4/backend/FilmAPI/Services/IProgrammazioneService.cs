using FilmAPI.DTO;

namespace FilmAPI.Services;

public interface IProgrammazioneService
{
    Task<ProgrammazioneFilmPagedResultDTO> GetFilmsAsync(string? tab, string? search, int? categoriaId, int? cinemaId, int page = 1, int pageSize = 20);
    Task<List<CinemaCardDTO>> GetCinemasAsync(double? lat, double? lng);
    Task<FilmSchedaDTO?> GetFilmSchedaAsync(int filmId, int? cinemaId);
    Task<List<CinemaCardDTO>> GetMyCinemasAsync();
    Task<CinemaScheduleDayDTO?> GetCinemaScheduleAsync(int cinemaId, DateOnly? date);
    Task<CinemaPreferitoDTO?> GetCinemaPreferitoAsync(int userId);
    Task<CinemaPreferitoDTO> SetCinemaPreferitoAsync(int userId, int? cinemaId);
}
