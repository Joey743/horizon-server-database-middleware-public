﻿using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace DeadlockedDatabase.Models
{
    public partial class Ratchet_DeadlockedContext : DbContext
    {
        public Ratchet_DeadlockedContext()
        {
        }

        public Ratchet_DeadlockedContext(DbContextOptions<Ratchet_DeadlockedContext> options)
            : base(options)
        {
        }

        public virtual DbSet<Account> Account { get; set; }
        public virtual DbSet<AccountFriend> AccountFriend { get; set; }
        public virtual DbSet<AccountIgnored> AccountIgnored { get; set; }
        public virtual DbSet<AccountStat> AccountStat { get; set; }
        public virtual DbSet<AccountStatus> AccountStatus { get; set; }
        public virtual DbSet<Banned> Banned { get; set; }
        public virtual DbSet<DimStats> DimStats { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Account>(entity =>
            {
                entity.ToTable("account", "ACCOUNTS");

                entity.Property(e => e.AccountId).HasColumnName("account_id");

                entity.Property(e => e.AccountName)
                    .IsRequired()
                    .HasColumnName("account_name")
                    .HasMaxLength(32);

                entity.Property(e => e.AccountPassword)
                    .IsRequired()
                    .HasColumnName("account_password")
                    .HasMaxLength(200);

                entity.Property(e => e.AppId).HasColumnName("app_id");

                entity.Property(e => e.CreateDt)
                    .HasColumnName("create_dt")
                    .HasDefaultValueSql("(getutcdate())");

                entity.Property(e => e.IsActive)
                    .IsRequired()
                    .HasColumnName("is_active")
                    .HasDefaultValueSql("((1))");

                entity.Property(e => e.LastSignInDt).HasColumnName("last_sign_in_dt");

                entity.Property(e => e.MachineId)
                    .HasColumnName("machine_id")
                    .HasMaxLength(100);

                entity.Property(e => e.MediusStats)
                    .HasColumnName("medius_stats")
                    .HasMaxLength(350);

                entity.Property(e => e.ModifiedDt).HasColumnName("modified_dt");
            });

            modelBuilder.Entity<AccountFriend>(entity =>
            {
                entity.ToTable("account_friend", "ACCOUNTS");

                entity.Property(e => e.Id).HasColumnName("id");

                entity.Property(e => e.AccountId).HasColumnName("account_id");

                entity.Property(e => e.CreateDt)
                    .HasColumnName("create_dt")
                    .HasDefaultValueSql("(getutcdate())");

                entity.Property(e => e.FriendAccountId).HasColumnName("friend_account_id");

                entity.HasOne(d => d.Account)
                    .WithMany(p => p.AccountFriend)
                    .HasForeignKey(d => d.AccountId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_account_friend_account");
            });

            modelBuilder.Entity<AccountIgnored>(entity =>
            {
                entity.ToTable("account_ignored", "ACCOUNTS");

                entity.Property(e => e.Id).HasColumnName("id");

                entity.Property(e => e.AccountId).HasColumnName("account_id");

                entity.Property(e => e.CreateDt)
                    .HasColumnName("create_dt")
                    .HasDefaultValueSql("(getutcdate())");

                entity.Property(e => e.IgnoredAccountId).HasColumnName("ignored_account_id");

                entity.HasOne(d => d.Account)
                    .WithMany(p => p.AccountIgnored)
                    .HasForeignKey(d => d.AccountId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_account_ignored_account");
            });

            modelBuilder.Entity<AccountStat>(entity =>
            {
                entity.ToTable("account_stat", "STATS");

                entity.Property(e => e.Id).HasColumnName("id");

                entity.Property(e => e.AccountId).HasColumnName("account_id");

                entity.Property(e => e.ModifiedDt).HasColumnName("modified_dt");

                entity.Property(e => e.StatId).HasColumnName("stat_id");

                entity.Property(e => e.StatValue).HasColumnName("stat_value");

                entity.HasOne(d => d.Account)
                    .WithMany(p => p.AccountStat)
                    .HasForeignKey(d => d.AccountId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_account_stat_account");

                entity.HasOne(d => d.Stat)
                    .WithMany(p => p.AccountStat)
                    .HasForeignKey(d => d.StatId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_account_stat_dim_stats");
            });

            modelBuilder.Entity<AccountStatus>(entity =>
            {
                entity.ToTable("account_status", "ACCOUNTS");

                entity.Property(e => e.Id).HasColumnName("id");

                entity.Property(e => e.AccountId).HasColumnName("account_id");

                entity.Property(e => e.ChannelId).HasColumnName("channel_id");

                entity.Property(e => e.GameId).HasColumnName("game_id");

                entity.Property(e => e.LoggedIn).HasColumnName("logged_in");

                entity.Property(e => e.WorldId).HasColumnName("world_id");
            });

            modelBuilder.Entity<Banned>(entity =>
            {
                entity.ToTable("banned", "ACCOUNTS");

                entity.Property(e => e.Id).HasColumnName("id");

                entity.Property(e => e.AccountId).HasColumnName("account_id");

                entity.Property(e => e.FromDt)
                    .HasColumnName("from_dt")
                    .HasDefaultValueSql("(getutcdate())");

                entity.Property(e => e.ToDt).HasColumnName("to_dt");
            });

            modelBuilder.Entity<DimStats>(entity =>
            {
                entity.HasKey(e => e.StatId);

                entity.ToTable("dim_stats", "KEYS");

                entity.Property(e => e.StatId).HasColumnName("stat_id");

                entity.Property(e => e.DefaultValue).HasColumnName("default_value");

                entity.Property(e => e.StatName)
                    .IsRequired()
                    .HasColumnName("stat_name")
                    .HasMaxLength(100);
            });

            OnModelCreatingPartial(modelBuilder);
        }

        partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
    }
}
