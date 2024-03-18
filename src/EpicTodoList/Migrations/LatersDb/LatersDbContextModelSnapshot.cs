﻿// <auto-generated />
using System;
using System.Collections.Generic;
using Laters.Data.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace EpicTodoList.Migrations.LatersDb
{
    [DbContext(typeof(LatersDbContext))]
    partial class LatersDbContextModelSnapshot : ModelSnapshot
    {
        protected override void BuildModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "8.0.3")
                .HasAnnotation("Relational:MaxIdentifierLength", 63);

            NpgsqlModelBuilderExtensions.HasPostgresExtension(modelBuilder, "hstore");
            NpgsqlModelBuilderExtensions.UseIdentityByDefaultColumns(modelBuilder);

            modelBuilder.Entity("Laters.Models.CronJob", b =>
                {
                    b.Property<string>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("text");

                    b.Property<string>("Cron")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<Dictionary<string, string>>("Headers")
                        .IsRequired()
                        .HasColumnType("hstore");

                    b.Property<bool>("IsGlobal")
                        .HasColumnType("boolean");

                    b.Property<string>("JobType")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<int>("MaxRetries")
                        .HasColumnType("integer");

                    b.Property<string>("Payload")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<Guid?>("Revision")
                        .IsConcurrencyToken()
                        .HasColumnType("uuid");

                    b.Property<int?>("TimeToLiveInSeconds")
                        .HasColumnType("integer");

                    b.Property<string>("WindowName")
                        .IsRequired()
                        .HasColumnType("text");

                    b.HasKey("Id");

                    b.ToTable("CronJobs");
                });

            modelBuilder.Entity("Laters.Models.Job", b =>
                {
                    b.Property<string>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("text");

                    b.Property<int>("Attempts")
                        .HasColumnType("integer");

                    b.Property<bool>("DeadLettered")
                        .HasColumnType("boolean");

                    b.Property<Dictionary<string, string>>("Headers")
                        .IsRequired()
                        .HasColumnType("hstore");

                    b.Property<string>("JobType")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<DateTime>("LastAttempted")
                        .HasColumnType("timestamp with time zone");

                    b.Property<int>("MaxRetries")
                        .HasColumnType("integer");

                    b.Property<string>("ParentCron")
                        .HasColumnType("text");

                    b.Property<string>("Payload")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<Guid?>("Revision")
                        .IsConcurrencyToken()
                        .HasColumnType("uuid");

                    b.Property<DateTime>("ScheduledFor")
                        .HasColumnType("timestamp with time zone");

                    b.Property<int?>("TimeToLiveInSeconds")
                        .HasColumnType("integer");

                    b.Property<string>("WindowName")
                        .IsRequired()
                        .HasColumnType("text");

                    b.HasKey("Id");

                    b.ToTable("Jobs");
                });

            modelBuilder.Entity("Laters.Models.Leader", b =>
                {
                    b.Property<string>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("text");

                    b.Property<Guid?>("Revision")
                        .IsConcurrencyToken()
                        .HasColumnType("uuid");

                    b.Property<string>("ServerId")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<DateTime>("Updated")
                        .HasColumnType("timestamp with time zone");

                    b.HasKey("Id");

                    b.ToTable("Leaders");
                });
#pragma warning restore 612, 618
        }
    }
}