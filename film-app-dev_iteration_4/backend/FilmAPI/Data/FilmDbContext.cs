// ============================================================================
// FilmDbContext.cs — CONTESTO DEL DATABASE (Entity Framework Core)
// ============================================================================
// Questa classe è il ponte tra il codice C# e il database MySQL.
// Ogni proprietà DbSet corrisponde a una tabella del database.
// Il metodo OnModelCreating configura:
//   - Indici univoci (per evitare duplicati)
//   - Relazioni tra tabelle (FK, DeleteBehavior)
//   - Vincoli di integrità referenziale
// ============================================================================

using Microsoft.EntityFrameworkCore;
using FilmAPI.Model;

namespace FilmAPI.Data;

public class FilmDbContext : DbContext
{
    public FilmDbContext(DbContextOptions<FilmDbContext> options) : base(options)
    {
    }

    // ========================================================================
    /// <summary>
    /// Auto-popola NormalizedEmail per nuovi User che hanno Email ma non NormalizedEmail.
    /// Questo metodo viene chiamato OGNI VOLTA che si salvano modifiche al database,
    /// quindi possiamo intercettare e modificare le entità prima del salvataggio.
    /// NormalizedEmail serve per fare lookup case-insensitive dell'email.
    /// </summary>
    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        foreach (var entry in ChangeTracker.Entries<User>().Where(e => e.State == EntityState.Added))
        {
            if (string.IsNullOrEmpty(entry.Entity.NormalizedEmail) && !string.IsNullOrEmpty(entry.Entity.Email))
                entry.Entity.NormalizedEmail = entry.Entity.Email.Trim().ToUpperInvariant();
        }
        return base.SaveChangesAsync(cancellationToken);
    }

    // ========================================================================
    // DBSET: ogni proprietà è una TABELLA nel database MySQL
    // ========================================================================
    // DbSet<T> permette di fare query LINQ sulle tabelle:
    //   _db.Films.ToListAsync()         → SELECT * FROM Films
    //   _db.Films.FindAsync(id)         → SELECT * FROM Films WHERE Id = @id
    //   _db.Films.Add(film)             → INSERT INTO Films ...
    //   _db.Films.Remove(film)          → DELETE FROM Films WHERE Id = @id
    // ========================================================================

    // --- Entità Core (Iterazioni 1-3) ---
    public DbSet<Regista> Registi { get; set; }
    public DbSet<Film> Films { get; set; }
    public DbSet<Cinema> Cinemas { get; set; }
    public DbSet<Proiezione> Proiezioni { get; set; }     // Entità legacy (pre-multisala)
    public DbSet<User> Users { get; set; }
    public DbSet<RefreshToken> RefreshTokens { get; set; }
    public DbSet<Prenotazione> Prenotazioni { get; set; }  // Entità legacy
    public DbSet<Categoria> Categorie { get; set; }
    public DbSet<FilmCategoria> FilmCategorie { get; set; }

    // --- Entità Multisala & Ticketing (Iterazione 4) ---
    public DbSet<Sala> Sale { get; set; }
    public DbSet<SalaPosto> SalaPosti { get; set; }
    public DbSet<Show> Shows { get; set; }
    public DbSet<ShowPostoStato> ShowPostiStato { get; set; }
    public DbSet<Ordine> Ordini { get; set; }
    public DbSet<Biglietto> Biglietti { get; set; }
    public DbSet<MovimentoCredito> MovimentiCredito { get; set; }

    // --- Entità Commerciali (Offer, Abbonamenti) ---
    public DbSet<Offerta> Offerte { get; set; }
    public DbSet<Abbonamento> Abbonamenti { get; set; }
    public DbSet<Voucher> Vouchers { get; set; }
    public DbSet<UserSubscription> UserSubscriptions { get; set; }

    // --- Entità Watchlist e Notifiche ---
    public DbSet<WatchlistItem> WatchlistItems { get; set; }
    public DbSet<Notifica> Notifiche { get; set; }
    public DbSet<NotificaSoppressa> NotificheSoppresse { get; set; }

    // --- Entità Auth & Security (Iterazione 5) ---
    public DbSet<UserExternalLogin> UserExternalLogins { get; set; }
    public DbSet<AccountActionToken> AccountActionTokens { get; set; }
    public DbSet<ExternalAuthState> ExternalAuthStates { get; set; }
    public DbSet<ExternalAuthExchangeCode> ExternalAuthExchangeCodes { get; set; }
    public DbSet<UserSecurityAuditLog> UserSecurityAuditLogs { get; set; }

    // --- Support Tickets ---
    public DbSet<SupportTicket> SupportTickets { get; set; }

    // ========================================================================
    // OnModelCreating — CONFIGURAZIONE DELLO SCHEMA DEL DATABASE
    // ========================================================================
    // Qui si definiscono:
    //   1. Indici (per performance e unicità)
    //   2. Relazioni (ForeignKey, DeleteBehavior)
    //   3. Vincoli (IsUnique, Required, MaxLength)
    //
    // NOTA: I delete behavior sono IMPORTANTI:
    //   - Cascade: se cancello il padre, cancello anche i figli
    //   - Restrict: non posso cancellare il padre se ci sono figli
    //   - SetNull: se cancello il padre, imposto la FK a NULL nei figli
    // ========================================================================
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // --- PROIEZIONE (entità legacy) ---
        // Indice univoco: stesso cinema, stesso film, stessa data e ora
        modelBuilder.Entity<Proiezione>(entity =>
        {
            entity.HasIndex(e => new { e.CinemaId, e.FilmId, e.Data, e.Ora })
                  .IsUnique();
        });

        // --- FILM -> REGISTA ---
        // Un film ha UN regista (obbligatorio)
        // Un regista ha MOLTI film
        // DeleteBehavior.Restrict: non posso cancellare un regista se ha film
        modelBuilder.Entity<Film>(entity =>
        {
            entity.HasOne(f => f.Regista)
                  .WithMany(r => r.Films)
                  .HasForeignKey(f => f.RegistaId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        // --- PROIEZIONE -> CINEMA + FILM ---
        modelBuilder.Entity<Proiezione>(entity =>
        {
            entity.HasOne(p => p.Cinema)
                  .WithMany(c => c.Proiezioni)
                  .HasForeignKey(p => p.CinemaId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(p => p.Film)
                  .WithMany(f => f.Proiezioni)
                  .HasForeignKey(p => p.FilmId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        // --- FILMCATEGORIA (relazione N:M tra Film e Categoria) ---
        // Chiave primaria composta: (FilmId, CategoriaId)
        // Cascade su entrambi i lati: se cancello film/categoria, elimino anche la relazione
        modelBuilder.Entity<FilmCategoria>(entity =>
        {
            entity.HasKey(fc => new { fc.FilmId, fc.CategoriaId });

            entity.HasOne(fc => fc.Film)
                  .WithMany(f => f.FilmCategorie)
                  .HasForeignKey(fc => fc.FilmId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(fc => fc.Categoria)
                  .WithMany(c => c.FilmCategorie)
                  .HasForeignKey(fc => fc.CategoriaId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // --- USER ---
        // Indice univoco su Email e NormalizedEmail per login veloce
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasIndex(e => e.Email).IsUnique();
            entity.HasIndex(e => e.NormalizedEmail).IsUnique();

            // CinemaPreferito: relazione opzionale (SetNull se cancello cinema)
            entity.HasOne(u => u.CinemaPreferito)
                  .WithMany()
                  .HasForeignKey(u => u.CinemaPreferitoId)
                  .OnDelete(DeleteBehavior.SetNull);
        });

        // --- ENTITÀ AUTH (Iterazione 5) ---
        // UserExternalLogin: collega utente locale a provider esterno (Google, Microsoft, Facebook)
        modelBuilder.Entity<UserExternalLogin>(entity =>
        {
            entity.HasIndex(e => new { e.Provider, e.ProviderUserId }).IsUnique();
            entity.HasIndex(e => new { e.UserId, e.Provider });
            entity.HasIndex(e => e.EmailAtLogin);

            entity.HasOne(e => e.User)
                  .WithMany(u => u.ExternalLogins)
                  .HasForeignKey(e => e.UserId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // AccountActionToken: token temporanei per reset password, cambio email, etc.
        modelBuilder.Entity<AccountActionToken>(entity =>
        {
            entity.HasIndex(e => e.TokenHash).IsUnique();
            entity.HasIndex(e => new { e.UserId, e.Purpose, e.ExpiresAtUtc });

            entity.HasOne(e => e.User)
                  .WithMany(u => u.ActionTokens)
                  .HasForeignKey(e => e.UserId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // ExternalAuthState: stato OAuth per social login (PKCE)
        modelBuilder.Entity<ExternalAuthState>(entity =>
        {
            entity.HasIndex(e => e.StateHash).IsUnique();
            entity.HasIndex(e => e.ExpiresAtUtc);
        });

        // ExternalAuthExchangeCode: codice monouso per scambio auth
        modelBuilder.Entity<ExternalAuthExchangeCode>(entity =>
        {
            entity.HasIndex(e => e.CodeHash).IsUnique();
            entity.HasIndex(e => e.ExpiresAtUtc);
        });

        // UserSecurityAuditLog: log di sicurezza (login, cambio password, etc.)
        modelBuilder.Entity<UserSecurityAuditLog>(entity =>
        {
            entity.HasIndex(e => new { e.UserId, e.CreatedAtUtc });
            entity.HasIndex(e => new { e.ActorUserId, e.CreatedAtUtc });
            entity.HasIndex(e => new { e.EventType, e.CreatedAtUtc });
        });

        // --- WATCHLIST ---
        // Un utente non può avere lo stesso film due volte in watchlist
        modelBuilder.Entity<WatchlistItem>(entity =>
        {
            entity.HasIndex(e => new { e.UserId, e.FilmId }).IsUnique();
        });

        // --- REFRESHTOKEN ---
        // Un token per dispositivo (UserId + DeviceId)
        // Indice su Token per lookup veloce durante refresh
        // Indice su (UserId, DeviceId) per revoca selettiva
        modelBuilder.Entity<RefreshToken>(entity =>
        {
            entity.HasIndex(e => e.Token).IsUnique();
            entity.HasIndex(e => e.UserId);
            entity.HasIndex(e => new { e.UserId, e.DeviceId });
        });

        // --- PRENOTAZIONE (legacy) ---
        modelBuilder.Entity<Prenotazione>(entity =>
        {
            entity.HasOne(p => p.User)
                  .WithMany(u => u.Prenotazioni)
                  .HasForeignKey(p => p.UserId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(p => p.Proiezione)
                  .WithMany()
                  .HasForeignKey(p => p.ProiezioneId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        // --- CATEGORIA: nome univoco ---
        modelBuilder.Entity<Categoria>(entity =>
        {
            entity.HasIndex(e => e.Nome).IsUnique();
        });

        // --- SALA: numero progressivo univoco per cinema ---
        modelBuilder.Entity<Sala>(entity =>
        {
            entity.HasIndex(e => new { e.CinemaId, e.NumeroProgressivo })
                  .IsUnique();

            entity.HasOne(s => s.Cinema)
                  .WithMany(c => c.Sale)
                  .HasForeignKey(s => s.CinemaId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        // --- SALAPOSTO: un posto è univoco nella sala per (Settore, Fila, Numero) ---
        modelBuilder.Entity<SalaPosto>(entity =>
        {
            entity.HasIndex(e => new { e.SalaId, e.Settore, e.Fila, e.Numero })
                  .IsUnique();

            entity.HasOne(sp => sp.Sala)
                  .WithMany(s => s.Posti)
                  .HasForeignKey(sp => sp.SalaId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // --- SHOW: due spettacoli non possono iniziare nello stesso momento nella stessa sala ---
        modelBuilder.Entity<Show>(entity =>
        {
            entity.HasIndex(e => new { e.CinemaId, e.SalaId, e.StartAtUtc })
                  .IsUnique();

            entity.HasOne(s => s.Cinema)
                  .WithMany()
                  .HasForeignKey(s => s.CinemaId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(s => s.Sala)
                  .WithMany(sa => sa.Shows)
                  .HasForeignKey(s => s.SalaId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(s => s.Film)
                  .WithMany(f => f.Shows)
                  .HasForeignKey(s => s.FilmId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        // --- SHOWPOSTOSTATO: un solo stato per posto per show ---
        // HoldToken per lookup durante refresh/rilascio
        // ScadeAtUtc per cleanup degli hold scaduti
        modelBuilder.Entity<ShowPostoStato>(entity =>
        {
            entity.HasIndex(e => new { e.ShowId, e.SalaPostoId })
                  .IsUnique();
            entity.HasIndex(e => e.HoldToken);
            entity.HasIndex(e => e.ScadeAtUtc);

            entity.HasOne(sps => sps.Show)
                  .WithMany(s => s.PostiStato)
                  .HasForeignKey(sps => sps.ShowId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(sps => sps.SalaPosto)
                  .WithMany()
                  .HasForeignKey(sps => sps.SalaPostoId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(sps => sps.User)
                  .WithMany()
                  .HasForeignKey(sps => sps.UserId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(sps => sps.Ordine)
                  .WithMany()
                  .HasForeignKey(sps => sps.OrdineId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        // --- ORDINE: codice ordine e idempotency key univoci ---
        // CodiceOrdine: codice leggibile per l'utente (es. CB-XXXX)
        // IdempotencyKey: chiave per evitare doppio pagamento
        // Tutte le FK sono Restrict: non si possono cancellare entità collegate a ordini
        modelBuilder.Entity<Ordine>(entity =>
        {
            entity.HasIndex(e => e.CodiceOrdine).IsUnique();
            entity.HasIndex(e => e.IdempotencyKey).IsUnique();

            entity.HasOne(o => o.User)
                  .WithMany(u => u.Ordini)
                  .HasForeignKey(o => o.UserId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(o => o.Show)
                  .WithMany(s => s.Ordini)
                  .HasForeignKey(o => o.ShowId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(o => o.Cinema)
                  .WithMany()
                  .HasForeignKey(o => o.CinemaId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(o => o.Sala)
                  .WithMany()
                  .HasForeignKey(o => o.SalaId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(o => o.Film)
                  .WithMany()
                  .HasForeignKey(o => o.FilmId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        // --- BIGLIETTO: un biglietto per posto per show ---
        // CodiceBiglietto univoco (CB-XXXXXXXX)
        // ShowId + SalaPostoId univoco (non posso vendere due volte lo stesso posto)
        modelBuilder.Entity<Biglietto>(entity =>
        {
            entity.HasIndex(e => new { e.ShowId, e.SalaPostoId }).IsUnique();
            entity.HasIndex(e => e.CodiceBiglietto).IsUnique();

            entity.HasOne(b => b.Ordine)
                  .WithMany(o => o.Biglietti)
                  .HasForeignKey(b => b.OrdineId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(b => b.Show)
                  .WithMany(s => s.Biglietti)
                  .HasForeignKey(b => b.ShowId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(b => b.SalaPosto)
                  .WithMany()
                  .HasForeignKey(b => b.SalaPostoId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(b => b.User)
                  .WithMany(u => u.Biglietti)
                  .HasForeignKey(b => b.UserId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        // --- VOUCHER: codice univoco ---
        modelBuilder.Entity<Voucher>(entity =>
        {
            entity.HasIndex(e => e.Codice).IsUnique();
        });

        // --- USERSUBSCRIPTION: abbonamento utente ---
        modelBuilder.Entity<UserSubscription>(entity =>
        {
            entity.HasIndex(e => new { e.UserId, e.Stato });
            entity.HasIndex(e => e.DataScadenza);
        });

        // --- MOVIMENTOCREDITO: audit trail per ogni transazione ---
        // Ogni movimento registra: saldo PRIMA e DOPO l'operazione
        modelBuilder.Entity<MovimentoCredito>(entity =>
        {
            entity.HasOne(mc => mc.User)
                  .WithMany()
                  .HasForeignKey(mc => mc.UserId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(mc => mc.OperatoreUser)
                  .WithMany()
                  .HasForeignKey(mc => mc.OperatoreUserId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(mc => mc.Cinema)
                  .WithMany()
                  .HasForeignKey(mc => mc.CinemaId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(mc => mc.Ordine)
                  .WithMany()
                  .HasForeignKey(mc => mc.OrdineId)
                  .OnDelete(DeleteBehavior.Restrict);
        });
    }
}
