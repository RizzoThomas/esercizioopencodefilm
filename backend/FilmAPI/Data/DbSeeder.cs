using FilmAPI.Data;
using FilmAPI.Model;

namespace FilmAPI.Data;

public static class DbSeeder
{
    public static void SeedIfEmpty(FilmDbContext db)
    {
        if (db.Registi.Any()) return;

        // Seed Categorie
        if (!db.Categorie.Any())
        {
            var categorie = new List<Categoria>
            {
                new() { Nome = "Azione", Descrizione = "Film d'azione ad alta intensità" },
                new() { Nome = "Avventura", Descrizione = "Film di avventura ed esplorazione" },
                new() { Nome = "Animazione", Descrizione = "Film animati per tutte le età" },
                new() { Nome = "Commedia", Descrizione = "Film comici e divertenti" },
                new() { Nome = "Drammatico", Descrizione = "Film drammatici e toccanti" },
                new() { Nome = "Fantasy", Descrizione = "Film di fantasia e mondi immaginari" },
                new() { Nome = "Horror", Descrizione = "Film horror e thriller" },
                new() { Nome = "Thriller", Descrizione = "Film a suspence e tensione" },
                new() { Nome = "Sci-Fi", Descrizione = "Film di fantascienza" },
                new() { Nome = "Documentario", Descrizione = "Documentari e film informativi" }
            };

            db.Categorie.AddRange(categorie);
            db.SaveChanges();
            Console.WriteLine("[Seeder] Categorie create");
        }

        // Seed Utenti di Test
        if (!db.Users.Any())
        {
            var users = new List<User>
            {
                new()
                {
                    Email = "admin@cinebase.it",
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword("admin123"),
                    Nome = "Admin",
                    Cognome = "CineBase",
                    Ruolo = UserRole.Admin,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                },
                new()
                {
                    Email = "power@cinebase.it",
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword("power123"),
                    Nome = "Power",
                    Cognome = "User",
                    Ruolo = UserRole.PowerUser,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                },
                new()
                {
                    Email = "user@cinebase.it",
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword("user123"),
                    Nome = "Mario",
                    Cognome = "Rossi",
                    Telefono = "+39 333 1234567",
                    DataNascita = new DateTime(1990, 5, 15),
                    Ruolo = UserRole.User,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                }
            };

            db.Users.AddRange(users);
            db.SaveChanges();
            Console.WriteLine("[Seeder] Utenti di test creati");
        }

        var registi = new List<Regista>
        {
            new() { Nome = "Christopher", Cognome = "Nolan", Nazionalita = "Regno Unito" },
            new() { Nome = "Quentin", Cognome = "Tarantino", Nazionalita = "USA" },
            new() { Nome = "Greta", Cognome = "Gerwig", Nazionalita = "USA" },
            new() { Nome = "Denis", Cognome = "Villeneuve", Nazionalita = "Canada" },
            new() { Nome = "Paolo", Cognome = "Sorrentino", Nazionalita = "Italia" },
            new() { Nome = "Guillermo", Cognome = "Del Toro", Nazionalita = "Messico" },
        };

        db.Registi.AddRange(registi);
        db.SaveChanges();

        var films = new List<Film>
        {
            new() { Titolo = "Interstellar", DataProduzione = new DateTime(2014, 11, 7), RegistaId = registi[0].Id, Durata = 169, CopertinaPath = "https://image.tmdb.org/t/p/w500/gEU2QniE6E77NI6lCU6MxlNBvIx.jpg" },
            new() { Titolo = "Inception", DataProduzione = new DateTime(2010, 7, 16), RegistaId = registi[0].Id, Durata = 148, CopertinaPath = "https://image.tmdb.org/t/p/w500/edv5CZvWj09upOsy2Y6IwDhK8bt.jpg" },
            new() { Titolo = "Pulp Fiction", DataProduzione = new DateTime(1994, 10, 14), RegistaId = registi[1].Id, Durata = 154, CopertinaPath = "https://image.tmdb.org/t/p/w500/d5iIlFn5s0ImszYzBPb8JPIfbXD.jpg" },
            new() { Titolo = "Dune", DataProduzione = new DateTime(2021, 10, 22), RegistaId = registi[3].Id, Durata = 155, CopertinaPath = "https://image.tmdb.org/t/p/w500/d5NXSklXo0qyIYkgV94XAgMIckC.jpg" },
            new() { Titolo = "La Grande Bellezza", DataProduzione = new DateTime(2013, 5, 21), RegistaId = registi[4].Id, Durata = 141, CopertinaPath = "https://image.tmdb.org/t/p/w500/jIb7uD2nMMeE61Ls3w4Qo0X0IKo.jpg" },
            new() { Titolo = "Barbie", DataProduzione = new DateTime(2023, 7, 21), RegistaId = registi[2].Id, Durata = 114, CopertinaPath = "https://image.tmdb.org/t/p/w500/iuFNMS8U5cb6xfzi51Dbkovj7vM.jpg" },
            new() { Titolo = "The Shape of Water", DataProduzione = new DateTime(2017, 12, 1), RegistaId = registi[5].Id, Durata = 123, CopertinaPath = "https://image.tmdb.org/t/p/w500/bvS50jBZXtglmLu72EAt5KgJBrL.jpg" },
        };

        db.Films.AddRange(films);
        db.SaveChanges();

        // Associa categorie ai film
        var categorieEsistenti = db.Categorie.ToList();
        if (categorieEsistenti.Any())
        {
            var filmCategorie = new List<FilmCategoria>
            {
                new() { FilmId = films[0].Id, CategoriaId = categorieEsistenti.First(c => c.Nome == "Sci-Fi").Id },
                new() { FilmId = films[0].Id, CategoriaId = categorieEsistenti.First(c => c.Nome == "Avventura").Id },
                new() { FilmId = films[1].Id, CategoriaId = categorieEsistenti.First(c => c.Nome == "Sci-Fi").Id },
                new() { FilmId = films[1].Id, CategoriaId = categorieEsistenti.First(c => c.Nome == "Azione").Id },
                new() { FilmId = films[2].Id, CategoriaId = categorieEsistenti.First(c => c.Nome == "Drammatico").Id },
                new() { FilmId = films[2].Id, CategoriaId = categorieEsistenti.First(c => c.Nome == "Thriller").Id },
                new() { FilmId = films[3].Id, CategoriaId = categorieEsistenti.First(c => c.Nome == "Sci-Fi").Id },
                new() { FilmId = films[3].Id, CategoriaId = categorieEsistenti.First(c => c.Nome == "Avventura").Id },
                new() { FilmId = films[4].Id, CategoriaId = categorieEsistenti.First(c => c.Nome == "Drammatico").Id },
                new() { FilmId = films[5].Id, CategoriaId = categorieEsistenti.First(c => c.Nome == "Commedia").Id },
                new() { FilmId = films[5].Id, CategoriaId = categorieEsistenti.First(c => c.Nome == "Fantasy").Id },
                new() { FilmId = films[6].Id, CategoriaId = categorieEsistenti.First(c => c.Nome == "Fantasy").Id },
                new() { FilmId = films[6].Id, CategoriaId = categorieEsistenti.First(c => c.Nome == "Drammatico").Id },
            };

            db.FilmCategorie.AddRange(filmCategorie);
            db.SaveChanges();
            Console.WriteLine("[Seeder] Categorie associate ai film");
        }

        var cinemas = new List<Cinema>
        {
            new() { Nome = "Cinema Roma", Indirizzo = "Via Roma 1", Citta = "Roma", CapienzaTotale = 180 },
            new() { Nome = "Cinema Milano", Indirizzo = "Via Dante 12", Citta = "Milano", CapienzaTotale = 140 },
            new() { Nome = "Multisala Napoli", Indirizzo = "Piazza Garibaldi 5", Citta = "Napoli", CapienzaTotale = 220 },
            new() { Nome = "Cineplex Torino", Indirizzo = "Corso Vittorio 88", Citta = "Torino", CapienzaTotale = 160 },
        };

        db.Cinemas.AddRange(cinemas);
        db.SaveChanges();

        var baseDate = DateTime.UtcNow.Date.AddDays(1);

        var proiezioni = new List<Proiezione>
        {
            new() { CinemaId = cinemas[0].Id, FilmId = films[0].Id, Data = baseDate, Ora = new DateTime(2026, 1, 1, 20, 30, 0) },
            new() { CinemaId = cinemas[0].Id, FilmId = films[2].Id, Data = baseDate, Ora = new DateTime(2026, 1, 1, 22, 0, 0) },
            new() { CinemaId = cinemas[1].Id, FilmId = films[1].Id, Data = baseDate.AddDays(1), Ora = new DateTime(2026, 1, 1, 19, 0, 0) },
            new() { CinemaId = cinemas[1].Id, FilmId = films[3].Id, Data = baseDate.AddDays(1), Ora = new DateTime(2026, 1, 1, 21, 30, 0) },
            new() { CinemaId = cinemas[2].Id, FilmId = films[4].Id, Data = baseDate, Ora = new DateTime(2026, 1, 1, 18, 0, 0) },
            new() { CinemaId = cinemas[3].Id, FilmId = films[5].Id, Data = baseDate.AddDays(2), Ora = new DateTime(2026, 1, 1, 20, 0, 0) },
            new() { CinemaId = cinemas[0].Id, FilmId = films[6].Id, Data = baseDate.AddDays(3), Ora = new DateTime(2026, 1, 1, 22, 30, 0) },
        };

        db.Proiezioni.AddRange(proiezioni);
        db.SaveChanges();

        Console.WriteLine($"[Seeder] Database popolato: {registi.Count} registi, {films.Count} film, {cinemas.Count} cinema, {proiezioni.Count} proiezioni");
    }
}
