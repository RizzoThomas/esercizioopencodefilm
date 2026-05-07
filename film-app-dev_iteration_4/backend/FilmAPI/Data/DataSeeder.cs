using Microsoft.EntityFrameworkCore;
using FilmAPI.Data;
using FilmAPI.Model;

using System.Globalization;

namespace FilmAPI.Data;

public class DataSeeder
{
    private readonly FilmDbContext _context;

    public DataSeeder(FilmDbContext context)
    {
        _context = context;
    }

    public async Task SeedAsync()
    {
        await SeedAdminAsync();
        await SeedCategorieAsync();
        await SeedDevDataAsync();
        await SeedOfferteAsync();
        await SeedAbbonamentiAsync();
        await SeedVoucherAsync();
    }

    private async Task SeedAdminAsync()
    {
        if (_context.Users.Any())
            return;

        var adminEmail = Environment.GetEnvironmentVariable("ADMIN_SEED_EMAIL") ?? "admin@cinebase.it";
        var adminPassword = Environment.GetEnvironmentVariable("ADMIN_SEED_PASSWORD") ?? "Admin123!";

        var admin = new User
        {
            Email = adminEmail,
            NormalizedEmail = adminEmail.ToUpperInvariant(),
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(adminPassword),
            LocalCredentialsEnabled = true,
            Nome = "Admin",
            Cognome = "CineBase",
            Ruolo = UserRole.Admin,
            DataRegistrazione = DateTime.UtcNow,
            CreditoResiduo = 0
        };

        _context.Users.Add(admin);
        await _context.SaveChangesAsync();
    }

    private async Task SeedCategorieAsync()
    {
        if (_context.Categorie.Any())
            return;

        var categorie = new[]
        {
            "Drammatico", "Commedia", "Avventura", "Fantasy", "Horror", "Azione",
            "Fantascienza", "Thriller", "Animazione", "Documentario", "Romantico", "Storico"
        };

        foreach (var nome in categorie)
        {
            _context.Categorie.Add(new Categoria { Nome = nome });
        }

        await _context.SaveChangesAsync();
    }

    private async Task SeedDevDataAsync()
    {
        var env = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production";
        if (!env.Equals("Development", StringComparison.OrdinalIgnoreCase))
            return;

        await SeedDevCinemasAsync();
        await SeedDevRegistiAsync();
        await SeedDevFilmsAsync();
        await SeedDevSaleAsync();
        await SeedDevShowsAsync();
    }

    private async Task SeedOfferteAsync()
    {
        var milano = await _context.Cinemas.FirstOrDefaultAsync(c => c.Nome.Contains("Milano"));
        var roma = await _context.Cinemas.FirstOrDefaultAsync(c => c.Nome.Contains("Roma"));
        var napoli = await _context.Cinemas.FirstOrDefaultAsync(c => c.Nome.Contains("Napoli"));

        var newOffers = new List<Offerta>();

        void AddOrUpdate(string nome, Offerta candidate)
        {
            var existing = _context.Offerte.FirstOrDefault(o => o.Nome == nome);
            if (existing is not null)
            {
                existing.Descrizione = candidate.Descrizione;
                existing.Tipo = candidate.Tipo;
                existing.Prezzo = candidate.Prezzo;
                existing.PrezzoOriginale = candidate.PrezzoOriginale;
                existing.ScontoPercentuale = candidate.ScontoPercentuale;
                existing.InEvidenza = candidate.InEvidenza;
                existing.NumeroBiglietti = candidate.NumeroBiglietti;
                existing.IncludePopcorn = candidate.IncludePopcorn;
                existing.CinemaId = candidate.CinemaId;
                existing.Attiva = true;
                return;
            }

            newOffers.Add(candidate);
        }

        AddOrUpdate("Pacchetto Famiglia", new Offerta
        {
            Nome = "Pacchetto Famiglia",
            Descrizione = "5 biglietti a prezzo promozionale per serate in famiglia.",
            Tipo = "solo_biglietti",
            Prezzo = 35m,
            PrezzoOriginale = 50m,
            ScontoPercentuale = 30,
            InEvidenza = true,
            NumeroBiglietti = 5,
            IncludePopcorn = 0,
            CinemaId = milano?.Id,
            Attiva = true,
            CreatedAtUtc = DateTime.UtcNow
        });

        AddOrUpdate("Serata Cinema", new Offerta
        {
            Nome = "Serata Cinema",
            Descrizione = "3 biglietti con popcorn inclusi per una serata completa.",
            Tipo = "biglietti_popcorn",
            Prezzo = 28m,
            PrezzoOriginale = 40m,
            ScontoPercentuale = 30,
            NumeroBiglietti = 3,
            IncludePopcorn = 3,
            CinemaId = roma?.Id,
            Attiva = true,
            CreatedAtUtc = DateTime.UtcNow
        });

        AddOrUpdate("Coppia", new Offerta
        {
            Nome = "Coppia",
            Descrizione = "2 biglietti con popcorn per una serata in due.",
            Tipo = "biglietti_popcorn",
            Prezzo = 18m,
            PrezzoOriginale = 25m,
            ScontoPercentuale = 28,
            NumeroBiglietti = 2,
            IncludePopcorn = 2,
            CinemaId = napoli?.Id,
            Attiva = true,
            CreatedAtUtc = DateTime.UtcNow
        });

        AddOrUpdate("Super Risparmio", new Offerta
        {
            Nome = "Super Risparmio",
            Descrizione = "6 biglietti per chi vuole organizzare una maratona cinema.",
            Tipo = "solo_biglietti",
            Prezzo = 38m,
            PrezzoOriginale = 55m,
            ScontoPercentuale = 31,
            InEvidenza = true,
            NumeroBiglietti = 6,
            IncludePopcorn = 0,
            CinemaId = milano?.Id,
            Attiva = true,
            CreatedAtUtc = DateTime.UtcNow
        });

        AddOrUpdate("Notte Horror", new Offerta
        {
            Nome = "Notte Horror",
            Descrizione = "4 biglietti e 4 popcorn per una notte da brividi.",
            Tipo = "biglietti_popcorn",
            Prezzo = 32m,
            PrezzoOriginale = 48m,
            ScontoPercentuale = 33,
            NumeroBiglietti = 4,
            IncludePopcorn = 4,
            CinemaId = roma?.Id,
            Attiva = true,
            CreatedAtUtc = DateTime.UtcNow
        });

        AddOrUpdate("Pomeriggio Family", new Offerta
        {
            Nome = "Pomeriggio Family",
            Descrizione = "3 biglietti per una uscita pomeridiana in famiglia.",
            Tipo = "solo_biglietti",
            Prezzo = 15m,
            PrezzoOriginale = 22m,
            ScontoPercentuale = 32,
            InEvidenza = true,
            NumeroBiglietti = 3,
            IncludePopcorn = 0,
            CinemaId = napoli?.Id,
            Attiva = true,
            CreatedAtUtc = DateTime.UtcNow
        });

        AddOrUpdate("Abbonamento Serale", new Offerta
        {
            Nome = "Abbonamento Serale",
            Descrizione = "Perfetto per chi ama guardare film dopo il lavoro.",
            Tipo = "abbonamento",
            Prezzo = 24m,
            PrezzoOriginale = 30m,
            ScontoPercentuale = 20,
            NumeroBiglietti = 2,
            IncludePopcorn = 0,
            CinemaId = roma?.Id,
            Attiva = true,
            CreatedAtUtc = DateTime.UtcNow
        });

        AddOrUpdate("Abbonamento Weekend", new Offerta
        {
            Nome = "Abbonamento Weekend",
            Descrizione = "Due ingressi e snack per il fine settimana.",
            Tipo = "abbonamento",
            Prezzo = 19m,
            PrezzoOriginale = 26m,
            ScontoPercentuale = 27,
            NumeroBiglietti = 2,
            IncludePopcorn = 2,
            CinemaId = milano?.Id,
            Attiva = true,
            CreatedAtUtc = DateTime.UtcNow
        });

        if (newOffers.Count > 0)
            _context.Offerte.AddRange(newOffers);

        await _context.SaveChangesAsync();
    }

    private async Task SeedAbbonamentiAsync()
    {
        if (_context.Abbonamenti.Any())
            return;

        _context.Abbonamenti.AddRange(new[]
        {
            new Abbonamento
            {
                Nome = "Abbonamento Mensile Base",
                Descrizione = "Ideale per chi vuole guardare più film spendendo meno ogni mese.",
                Tipo = "mensile",
                Prezzo = 29.90m,
                ScontoPercentuale = 25,
                NumeroBigliettiPerMese = 4,
                IncludePopcornPerMese = 0,
                Attivo = true,
                CreatedAtUtc = DateTime.UtcNow
            },
            new Abbonamento
            {
                Nome = "Abbonamento Annuale Plus",
                Descrizione = "La soluzione completa per chi vive il cinema tutto l'anno.",
                Tipo = "annuale",
                Prezzo = 24.90m,
                PrezzoAnnuale = 249m,
                ScontoPercentuale = 40,
                NumeroBigliettiPerMese = 4,
                IncludePopcornPerMese = 2,
                Attivo = true,
                CreatedAtUtc = DateTime.UtcNow
            },
            new Abbonamento
            {
                Nome = "Abbonamento Mensile Premium",
                Descrizione = "Più biglietti, più vantaggi, più cinema ogni mese.",
                Tipo = "mensile",
                Prezzo = 49.90m,
                ScontoPercentuale = 35,
                NumeroBigliettiPerMese = 8,
                IncludePopcornPerMese = 4,
                Attivo = true,
                CreatedAtUtc = DateTime.UtcNow
            }
        });

        await _context.SaveChangesAsync();
    }

    private async Task SeedVoucherAsync()
    {
        if (_context.Vouchers.Any(v => v.Codice == "CINEBASE50"))
            return;

        _context.Vouchers.Add(new Voucher
        {
            Codice = "CINEBASE50",
            ImportoIniziale = 50m,
            SaldoResiduo = 50m,
            Stato = "attivo",
            DataScadenza = null,
            UserId = null,
            CreatedAtUtc = DateTime.UtcNow
        });

        await _context.SaveChangesAsync();
    }

    private async Task SeedDevCinemasAsync()
    {
        if (_context.Cinemas.Any())
            return;

        var cinemas = new[]
        {
            new Cinema
            {
                Nome = "CineBase Roma",
                Citta = "Roma",
                Indirizzo = "Via Roma 123",
                Latitudine = 41.9028,
                Longitudine = 12.4964,
                Telefono = "06 1234567",
                CodiceLocale = "CBR001"
            },
            new Cinema
            {
                Nome = "CineBase Milano",
                Citta = "Milano",
                Indirizzo = "Via Milano 456",
                Latitudine = 45.4642,
                Longitudine = 9.1900,
                Telefono = "02 1234567",
                CodiceLocale = "CBM001"
            },
            new Cinema
            {
                Nome = "CineBase Napoli",
                Citta = "Napoli",
                Indirizzo = "Via Napoli 789",
                Latitudine = 40.8518,
                Longitudine = 14.2681,
                Telefono = "081 1234567",
                CodiceLocale = "CBN001"
            }
        };

        _context.Cinemas.AddRange(cinemas);
        await _context.SaveChangesAsync();
    }

    private async Task SeedDevRegistiAsync()
    {
        if (_context.Registi.Any())
            return;

        var registi = new[]
        {
            new Regista { Nome = "Christopher", Cognome = "Nolan", Nazionalita = "Regno Unito" },
            new Regista { Nome = "Quentin", Cognome = "Tarantino", Nazionalita = "USA" },
            new Regista { Nome = "Greta", Cognome = "Gerwig", Nazionalita = "USA" },
            new Regista { Nome = "Javier", Cognome = "Mariscal", Nazionalita = "Spagna" }
        };

        _context.Registi.AddRange(registi);
        await _context.SaveChangesAsync();
    }

    private async Task SeedDevFilmsAsync()
    {
        if (_context.Films.Any())
            return;

        var registaNolan = await _context.Registi.FirstAsync(r => r.Cognome == "Nolan");
        var registaTarantino = await _context.Registi.FirstAsync(r => r.Cognome == "Tarantino");
        var registaGerwig = await _context.Registi.FirstAsync(r => r.Cognome == "Gerwig");

        var films = new[]
        {
            new Film
            {
                Titolo = "Oppenheimer",
                DataProduzione = new DateTime(2023, 7, 19),
                RegistaId = registaNolan.Id,
                Durata = 180,
                DescrizioneLunga = "La storia del fisico J. Robert Oppenheimer e il suo ruolo nello sviluppo della bomba atomica.",
                CastText = "Cillian Murphy, Emily Blunt, Matt Damon, Robert Downey Jr.",
                DataRilascio = new DateOnly(2023, 7, 19)
            },
            new Film
            {
                Titolo = "Barbie",
                DataProduzione = new DateTime(2023, 7, 19),
                RegistaId = registaGerwig.Id,
                Durata = 114,
                DescrizioneLunga = "Barbie e Ken frequentano un mondo colorato e ottimistico fino a quando non vengono cacciati dal loro paradiso e partono per il mondo reale.",
                CastText = "Margot Robbie, Ryan Gosling, America Ferrera, Will Ferrell.",
                DataRilascio = new DateOnly(2023, 7, 19)
            },
            new Film
            {
                Titolo = "Dune - Parte Due",
                DataProduzione = new DateTime(2023, 10, 15),
                RegistaId = registaNolan.Id,
                Durata = 166,
                DescrizioneLunga = "Paul Atreides si unisce ai Fremen mentre cerca vendetta contro coloro che hanno distrutto la sua famiglia.",
                CastText = "Timothee Chalamet, Zendaya, Rebecca Ferguson, Josh Brolin.",
                DataRilascio = new DateOnly(2023, 10, 15)
            },
            new Film
            {
                Titolo = "Pulp Fiction",
                DataProduzione = new DateTime(1994, 10, 14),
                RegistaId = registaTarantino.Id,
                Durata = 154,
                DescrizioneLunga = "Le storie di due gangster, un pugile e una coppia di rapinatori si intrecciano in questo capolavoro del cinema.",
                CastText = "John Travolta, Uma Thurman, Samuel L. Jackson, Bruce Willis.",
                DataRilascio = new DateOnly(1994, 10, 14)
            }
        };

        _context.Films.AddRange(films);
        await _context.SaveChangesAsync();

        var categoriaDrammatico = await _context.Categorie.FirstAsync(c => c.Nome == "Drammatico");
        var categoriaAzione = await _context.Categorie.FirstAsync(c => c.Nome == "Azione");
        var categoriaFantascienza = await _context.Categorie.FirstAsync(c => c.Nome == "Fantascienza");

        var filmOppenheimer = await _context.Films.FirstAsync(f => f.Titolo == "Oppenheimer");
        var filmBarbie = await _context.Films.FirstAsync(f => f.Titolo == "Barbie");
        var filmDune = await _context.Films.FirstAsync(f => f.Titolo == "Dune - Parte Due");
        var filmPulp = await _context.Films.FirstAsync(f => f.Titolo == "Pulp Fiction");

        _context.FilmCategorie.AddRange(new[]
        {
            new FilmCategoria { FilmId = filmOppenheimer.Id, CategoriaId = categoriaDrammatico.Id },
            new FilmCategoria { FilmId = filmOppenheimer.Id, CategoriaId = categoriaFantascienza.Id },
            new FilmCategoria { FilmId = filmBarbie.Id, CategoriaId = categoriaAzione.Id },
            new FilmCategoria { FilmId = filmBarbie.Id, CategoriaId = categoriaDrammatico.Id },
            new FilmCategoria { FilmId = filmDune.Id, CategoriaId = categoriaFantascienza.Id },
            new FilmCategoria { FilmId = filmDune.Id, CategoriaId = categoriaAzione.Id },
            new FilmCategoria { FilmId = filmPulp.Id, CategoriaId = categoriaDrammatico.Id }
        });
        await _context.SaveChangesAsync();
    }

    private async Task SeedDevSaleAsync()
    {
        if (_context.Sale.Any())
            return;

        var cinemaRoma = await _context.Cinemas.FirstAsync(c => c.CodiceLocale == "CBR001");
        var cinemaMilano = await _context.Cinemas.FirstAsync(c => c.CodiceLocale == "CBM001");
        var cinemaNapoli = await _context.Cinemas.FirstAsync(c => c.CodiceLocale == "CBN001");

        var sale = new List<Sala>();

        sale.AddRange(new[]
        {
            new Sala { CinemaId = cinemaRoma.Id, NumeroProgressivo = 1, TipoSala = TipoSala.DueD, Nome = "Sala 1", Supplemento = 0, IsAttiva = true },
            new Sala { CinemaId = cinemaRoma.Id, NumeroProgressivo = 2, TipoSala = TipoSala.TreD, Nome = "Sala 2", Supplemento = 2.00m, IsAttiva = true },
            new Sala { CinemaId = cinemaRoma.Id, NumeroProgressivo = 3, TipoSala = TipoSala.ISENSE, Nome = "Sala 3", Supplemento = 4.00m, IsAttiva = true },
            new Sala { CinemaId = cinemaMilano.Id, NumeroProgressivo = 1, TipoSala = TipoSala.DueD, Nome = "Sala 1", Supplemento = 0, IsAttiva = true },
            new Sala { CinemaId = cinemaMilano.Id, NumeroProgressivo = 2, TipoSala = TipoSala.XL, Nome = "Sala XL", Supplemento = 3.00m, IsAttiva = true },
            new Sala { CinemaId = cinemaNapoli.Id, NumeroProgressivo = 1, TipoSala = TipoSala.DueD, Nome = "Sala 1", Supplemento = 0, IsAttiva = true },
            new Sala { CinemaId = cinemaNapoli.Id, NumeroProgressivo = 2, TipoSala = TipoSala.TreD, Nome = "Sala 2", Supplemento = 2.00m, IsAttiva = true }
        });

        _context.Sale.AddRange(sale);
        await _context.SaveChangesAsync();
    }

    private async Task SeedDevShowsAsync()
    {
        if (_context.Shows.Any())
            return;

        var rawDefaultTicketPrice = Environment.GetEnvironmentVariable("DEFAULT_TICKET_PRICE");
        var defaultTicketPrice = !string.IsNullOrWhiteSpace(rawDefaultTicketPrice)
            && (decimal.TryParse(rawDefaultTicketPrice, NumberStyles.Number, CultureInfo.InvariantCulture, out var price)
                || decimal.TryParse(rawDefaultTicketPrice, NumberStyles.Number, CultureInfo.GetCultureInfo("it-IT"), out price)
                || decimal.TryParse(rawDefaultTicketPrice, out price))
            ? price
            : 8.50m;

        var cinemaRoma = await _context.Cinemas.FirstAsync(c => c.CodiceLocale == "CBR001");
        var cinemaMilano = await _context.Cinemas.FirstAsync(c => c.CodiceLocale == "CBM001");
        var cinemaNapoli = await _context.Cinemas.FirstAsync(c => c.CodiceLocale == "CBN001");

        var salaRoma1 = await _context.Sale.FirstAsync(s => s.CinemaId == cinemaRoma.Id && s.NumeroProgressivo == 1);
        var salaRoma2 = await _context.Sale.FirstAsync(s => s.CinemaId == cinemaRoma.Id && s.NumeroProgressivo == 2);
        var salaMilano1 = await _context.Sale.FirstAsync(s => s.CinemaId == cinemaMilano.Id && s.NumeroProgressivo == 1);
        var salaNapoli1 = await _context.Sale.FirstAsync(s => s.CinemaId == cinemaNapoli.Id && s.NumeroProgressivo == 1);

        var filmOppenheimer = await _context.Films.FirstAsync(f => f.Titolo == "Oppenheimer");
        var filmBarbie = await _context.Films.FirstAsync(f => f.Titolo == "Barbie");
        var filmDune = await _context.Films.FirstAsync(f => f.Titolo == "Dune - Parte Due");
        var filmPulp = await _context.Films.FirstAsync(f => f.Titolo == "Pulp Fiction");

        var baseDate = DateTime.UtcNow.Date;
        var today = baseDate;
        var tomorrow = baseDate.AddDays(1);
        var dayAfter = baseDate.AddDays(2);

        var shows = new List<Show>
        {
            new Show
            {
                CinemaId = cinemaRoma.Id,
                SalaId = salaRoma1.Id,
                FilmId = filmOppenheimer.Id,
                StartAtUtc = today.AddHours(16),
                DurataMinutiSnapshot = filmOppenheimer.Durata,
                PrezzoBase = defaultTicketPrice,
                SupplementoSala = salaRoma1.Supplemento
            },
            new Show
            {
                CinemaId = cinemaRoma.Id,
                SalaId = salaRoma1.Id,
                FilmId = filmOppenheimer.Id,
                StartAtUtc = today.AddHours(20),
                DurataMinutiSnapshot = filmOppenheimer.Durata,
                PrezzoBase = defaultTicketPrice,
                SupplementoSala = salaRoma1.Supplemento
            },
            new Show
            {
                CinemaId = cinemaRoma.Id,
                SalaId = salaRoma2.Id,
                FilmId = filmBarbie.Id,
                StartAtUtc = today.AddHours(18),
                DurataMinutiSnapshot = filmBarbie.Durata,
                PrezzoBase = defaultTicketPrice,
                SupplementoSala = salaRoma2.Supplemento
            },
            new Show
            {
                CinemaId = cinemaRoma.Id,
                SalaId = salaRoma2.Id,
                FilmId = filmDune.Id,
                StartAtUtc = tomorrow.AddHours(17),
                DurataMinutiSnapshot = filmDune.Durata,
                PrezzoBase = defaultTicketPrice,
                SupplementoSala = salaRoma2.Supplemento
            },
            new Show
            {
                CinemaId = cinemaMilano.Id,
                SalaId = salaMilano1.Id,
                FilmId = filmPulp.Id,
                StartAtUtc = today.AddHours(19),
                DurataMinutiSnapshot = filmPulp.Durata,
                PrezzoBase = defaultTicketPrice,
                SupplementoSala = salaMilano1.Supplemento
            },
            new Show
            {
                CinemaId = cinemaMilano.Id,
                SalaId = salaMilano1.Id,
                FilmId = filmOppenheimer.Id,
                StartAtUtc = tomorrow.AddHours(15),
                DurataMinutiSnapshot = filmOppenheimer.Durata,
                PrezzoBase = defaultTicketPrice,
                SupplementoSala = salaMilano1.Supplemento
            },
            new Show
            {
                CinemaId = cinemaNapoli.Id,
                SalaId = salaNapoli1.Id,
                FilmId = filmBarbie.Id,
                StartAtUtc = today.AddHours(17),
                DurataMinutiSnapshot = filmBarbie.Durata,
                PrezzoBase = defaultTicketPrice,
                SupplementoSala = salaNapoli1.Supplemento
            },
            new Show
            {
                CinemaId = cinemaNapoli.Id,
                SalaId = salaNapoli1.Id,
                FilmId = filmDune.Id,
                StartAtUtc = dayAfter.AddHours(20),
                DurataMinutiSnapshot = filmDune.Durata,
                PrezzoBase = defaultTicketPrice,
                SupplementoSala = salaNapoli1.Supplemento
            }
        };

        _context.Shows.AddRange(shows);
        await _context.SaveChangesAsync();
    }
}
