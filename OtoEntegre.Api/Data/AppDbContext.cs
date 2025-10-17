using Microsoft.EntityFrameworkCore;
using OtoEntegre.Api.Entities; // <-- Dealer bu namespace’de

namespace OtoEntegre.Api.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options)
            : base(options)
        { }

        public DbSet<Entegrasyonlar> Entegrasyonlar { get; set; }
        public DbSet<KULLANICILAR> Kullanicilar { get; set; }
        public DbSet<ROLLER> Roller { get; set; }
        public DbSet<Siparisler> Siparisler { get; set; }
        public DbSet<Urunler> Urunler { get; set; }
        public DbSet<SiparisDosyalari> SiparisDosyalari { get; set; }
        public DbSet<SiparisUrunleri> SiparisUrunleri { get; set; }
        public DbSet<KullaniciRolleri> KullaniciRolleri { get; set; } // eklendi
        public DbSet<Dealer> Dealers { get; set; }  // <- Bu olmalı
        public DbSet<Dealer> Platformlar { get; set; }  // <- Bu olmalı
    public DbSet<Krediler> Krediler { get; set; }
        public DbSet<KrediIslemleri> KrediIslemleri { get; set; }


        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // KullaniciRolleri mapping
            modelBuilder.Entity<KullaniciRolleri>(entity =>
            {
                entity.ToTable("kullanici_rolleri");

                entity.HasKey(e => e.Id);

                entity.Property(e => e.KullaniciId).HasColumnName("kullanici_id");
                entity.Property(e => e.RolId).HasColumnName("rol_id");

                entity.HasOne(e => e.Kullanici)
                    .WithMany(k => k.Roller)
                    .HasForeignKey(e => e.KullaniciId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.Rol)
                    .WithMany()
                    .HasForeignKey(e => e.RolId)
                    .OnDelete(DeleteBehavior.Cascade);
            });



            // KULLANICILAR mapping
            modelBuilder.Entity<KULLANICILAR>(entity =>
            {
                entity.ToTable("kullanicilar");
                entity.HasKey(e => e.Id);

                entity.Property(e => e.Ad).HasColumnName("ad");
                entity.Property(e => e.Email).HasColumnName("email");
                entity.Property(e => e.Telefon).HasColumnName("telefon");
                entity.Property(e => e.Sifre_Hash).HasColumnName("sifre_hash");
                entity.Property(e => e.Rol).HasColumnName("rol");
                entity.Property(e => e.Created_At).HasColumnName("created_at");
                entity.Property(e => e.Updated_At).HasColumnName("updated_at");
            });

            modelBuilder.Entity<Dealer>(entity =>
{
    entity.ToTable("dealers");
    entity.HasKey(e => e.Id);

    entity.Property(e => e.Id).HasColumnName("id");       // <--- burayı ekleyin
    entity.Property(e => e.Name).HasColumnName("name");   // diğer sütunlar
    entity.Property(e => e.Email).HasColumnName("email");
    entity.Property(e => e.Phone).HasColumnName("phone");
});
            modelBuilder.Entity<SiparisUrunleri>(entity =>
    {
        entity.ToTable("siparis_urunleri");
        entity.HasKey(e => e.Id);

        entity.Property(e => e.Siparis_Id).HasColumnName("siparis_id");
        entity.Property(e => e.Urun_Id).HasColumnName("urun_id");
        entity.Property(e => e.Adet).HasColumnName("adet");
        entity.Property(e => e.Birim_Fiyat).HasColumnName("birim_fiyat");
        entity.Property(e => e.Toplam_Fiyat).HasColumnName("toplam_fiyat");

        entity.HasOne(e => e.Siparis)
              .WithMany(s => s.SiparisUrunleri)
              .HasForeignKey(e => e.Siparis_Id)
              .OnDelete(DeleteBehavior.Cascade);

        entity.HasOne(e => e.Urun)
              .WithMany(u => u.SiparisUrunleri)
              .HasForeignKey(e => e.Urun_Id)
              .OnDelete(DeleteBehavior.Cascade);
    });

            // Krediler mapping
            modelBuilder.Entity<Krediler>(entity =>
            {
                entity.ToTable("krediler");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).HasColumnName("id");
                entity.Property(e => e.KullaniciId).HasColumnName("kullanici_id");
                entity.Property(e => e.KalanKredi).HasColumnName("kalan_kredi");
                entity.Property(e => e.SonSatinAlim).HasColumnName("son_satin_alim");
            });

            // KrediIslemleri mapping
            modelBuilder.Entity<KrediIslemleri>(entity =>
            {
                entity.ToTable("kredi_islemleri");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).HasColumnName("id");
                entity.Property(e => e.KullaniciId).HasColumnName("kullanici_id");
                entity.Property(e => e.Miktar).HasColumnName("miktar");
                entity.Property(e => e.Tip).HasColumnName("tip");
                entity.Property(e => e.BalanceAfter).HasColumnName("balance_after");
                entity.Property(e => e.Referans).HasColumnName("referans");
                entity.Property(e => e.Aciklama).HasColumnName("aciklama");
                entity.Property(e => e.Metadata).HasColumnName("metadata");
                entity.Property(e => e.CreatedAt).HasColumnName("created_at");
                entity.Property(e => e.CreatedBy).HasColumnName("created_by");
            });


        }

    }
}
