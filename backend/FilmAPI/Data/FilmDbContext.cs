using Microsoft.EntityFrameworkCore;
using FilmAPI.Model;

namespace FilmAPI.Data;

public class FilmDbContext : DbContext
{
    public FilmDbContext(DbContextOptions<FilmDbContext> options) : base(options)
    {
    }

    public DbSet<Regista> Registi { get; set; }
    public DbSet<Film> Films { get; set; }
    public DbSet<Cinema> Cinemas { get; set; }
    public DbSet<Proiezione> Proiezioni { get; set; }
    public DbSet<User> Users { get; set; }
    public DbSet<RefreshToken> RefreshTokens { get; set; }
    public DbSet<Categoria> Categorie { get; set; }
    public DbSet<FilmCategoria> FilmCategorie { get; set; }
    public DbSet<UserProiezione> UserProiezioni { get; set; }
    public DbSet<Prenotazione> Prenotazioni { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Unique index on User.Email
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasIndex(e => e.Email).IsUnique();
        });

        // Unique constraint on Proiezione
        modelBuilder.Entity<Proiezione>(entity =>
        {
            entity.HasIndex(e => new { e.CinemaId, e.FilmId, e.Data, e.Ora })
            .IsUnique();
        });

        // Film-Regista relationship
        modelBuilder.Entity<Film>(entity =>
        {
            entity.HasOne(f => f.Regista)
            .WithMany(r => r.Films)
            .HasForeignKey(f => f.RegistaId)
            .OnDelete(DeleteBehavior.Restrict);
        });

        // Proiezione relationships
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

        // Many-to-many Film-Categoria
        modelBuilder.Entity<FilmCategoria>(entity =>
        {
            entity.HasKey(e => new { e.FilmId, e.CategoriaId });

            entity.HasOne(e => e.Film)
                .WithMany()
                .HasForeignKey(e => e.FilmId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Categoria)
                .WithMany(c => c.FilmCategorie)
                .HasForeignKey(e => e.CategoriaId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // UserProiezione relationships
        modelBuilder.Entity<UserProiezione>(entity =>
        {
            entity.HasOne(up => up.User)
                .WithMany(u => u.ProiezioniSalvate)
                .HasForeignKey(up => up.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(up => up.Proiezione)
                .WithMany()
                .HasForeignKey(up => up.ProiezioneId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Prenotazione relationships
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

        // Unique constraint on CodicePrenotazione
        modelBuilder.Entity<Prenotazione>(entity =>
        {
            entity.HasIndex(e => e.CodicePrenotazione).IsUnique();
        });
    }
}
