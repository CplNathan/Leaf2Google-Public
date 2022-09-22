﻿using Leaf2Google.Models.Generic;
using Leaf2Google.Models.Google;
using Leaf2Google.Models.Car;
using Microsoft.EntityFrameworkCore;

namespace Leaf2Google.Contexts
{
    public class LeafContext : DbContext
    {
        public LeafContext(DbContextOptions<LeafContext> options)
            : base(options)
        {
        }

        public DbSet<CarModel> NissanLeafs { get; set; }

        public DbSet<Audit<CarModel>> NissanAudits { get; set; }

        public DbSet<Auth> GoogleAuths { get; set; }

        public DbSet<Token> GoogleTokens { get; set; }

        public DbSet<Config> AppConfig { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            /*
            modelBuilder
                .Entity<Leaf>()
                .Property(e => e.NissanPassword)
                .HasConversion(
                    v => v.ToString(),
                    v => (EquineBeast)Enum.Parse(typeof(EquineBeast), v));
            */

            modelBuilder
                .Entity<CarModel>()
                .ToTable("t_leafs_leaf");

            modelBuilder
                .Entity<Audit<CarModel>>()
                .ToTable("t_leafs_audit")
                .HasOne(i => i.Owner);

            modelBuilder
                .Entity<Auth>()
                .ToTable("t_auths_auth");

            modelBuilder
                .Entity<Token>()
                .ToTable("t_auths_token");

            modelBuilder
                .Entity<Config>()
                .ToTable("t_app_config");
        }
    }
}