using CognomeNomeAPI.DTO;
using CognomeNomeAPI.Model;

namespace CognomeNomeAPI.Extensions;

public static class MappingExtensions
{
    public static RegistaDTO ToDTO(this Regista r) => new RegistaDTO { Id = r.Id, Nome = r.Nome, Cognome = r.Cognome, Nazionalita = r.Nazionalita };

    public static FilmDTO ToDTO(this Film f) => new FilmDTO { Id = f.Id, Titolo = f.Titolo, DataProduzione = f.DataProduzione, RegistaId = f.RegistaId, Durata = f.Durata };

    public static CinemaDTO ToDTO(this Cinema c) => new CinemaDTO { Id = c.Id, Nome = c.Nome, Indirizzo = c.Indirizzo, Citta = c.Citta };

    public static Regista ToEntity(this RegistaDTO dto) => new Regista { Id = dto.Id, Nome = dto.Nome, Cognome = dto.Cognome, Nazionalita = dto.Nazionalita };

    public static Film ToEntity(this FilmDTO dto) => new Film { Id = dto.Id, Titolo = dto.Titolo, DataProduzione = dto.DataProduzione, RegistaId = dto.RegistaId, Durata = dto.Durata };

    public static Cinema ToEntity(this CinemaDTO dto) => new Cinema { Id = dto.Id, Nome = dto.Nome, Indirizzo = dto.Indirizzo, Citta = dto.Citta };
}
