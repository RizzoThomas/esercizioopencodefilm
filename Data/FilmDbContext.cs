using CognomeNomeAPI.Model;
using Microsoft.EntityFrameworkCore;

namespace CognomeNomeAPI.Data;

public class FilmDbContext : DbContext
{
    public FilmDbContext(DbContextOptions<FilmDbContext> options) : base(options) { }

    public DbSet<Regista> Registi { get; set; } = null!;
    public DbSet<Film> Films { get; set; } = null!;
    public DbSet<Cinema> Cinemas { get; set; } = null!;
    public DbSet<Proiezione> Proiezioni { get; set; } = null!;
    public DbSet<TaskItem> Tasks { get; set; } = null!;
    public DbSet<PriorityLog> PriorityLogs { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Regista>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Nome).IsRequired();
            entity.Property(e => e.Cognome).IsRequired();
            entity.Property(e => e.Nazionalita).IsRequired();
        });

        modelBuilder.Entity<Film>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Titolo).IsRequired();
            entity.HasOne(e => e.Regista)
                .WithMany(r => r.Films)
                .HasForeignKey(e => e.RegistaId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Cinema>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Nome).IsRequired();
            entity.Property(e => e.Indirizzo).IsRequired();
            entity.Property(e => e.Citta).IsRequired();
        });

        modelBuilder.Entity<Proiezione>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.CinemaId, e.FilmId });
            entity.HasOne<Cinema>()
                .WithMany()
                .HasForeignKey(p => p.CinemaId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne<Film>()
                .WithMany()
                .HasForeignKey(p => p.FilmId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<PriorityLog>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.FactorsJson).IsRequired();
            entity.Property(e => e.Score).IsRequired();
        });
    }
}
