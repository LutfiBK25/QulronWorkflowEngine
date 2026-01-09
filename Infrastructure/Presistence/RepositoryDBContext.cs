using Domain.ProcessEngine.Entities;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Presistence;

public class RepositoryDBContext : DbContext
{
    public RepositoryDBContext(DbContextOptions<RepositoryDBContext> options)
        : base(options)
        { }

    public DbSet<Application> Applications { get; set; }
    public DbSet<Module> Modules { get; set; }
    public DbSet<ProcessModule> ProcessModules { get; set; }
    public DbSet<ProcessModuleDetail> ProcessModuleDetails { get; set; }
    public DbSet<DatabaseActionModule> DatabaseActionModules { get; set; }
    public DbSet<FieldModule> FieldModules { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // =============================================
        // APPLICATIONS
        // =============================================
        modelBuilder.Entity<Application>(entity =>
        {
            entity.ToTable("applications");
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Id)
                .HasColumnName("id")
                .ValueGeneratedOnAdd();

            entity.Property(e => e.Name)
                .HasColumnName("name")
                .HasMaxLength(255)
                .IsRequired();

            entity.Property(e => e.Version)
                .HasColumnName("version")
                .HasMaxLength(10)
                .IsRequired();

            entity.Property(e => e.VersionBuild)
                .HasColumnName("version_build")
                .HasMaxLength(10);

            entity.Property(e => e.LastCompiled)
                .HasColumnName("last_compiled");

            entity.Property(e => e.LastActivated)
                .HasColumnName("last_activated");

            entity.Property(e => e.CreatedDate)
                .HasColumnName("created_date")
                .HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.Property(e => e.ModifiedDate)
                .HasColumnName("modified_date")
                .HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.HasIndex(e => e.Name)
                .IsUnique()
                .HasDatabaseName("uq_application_name");

            entity.HasMany(e => e.Modules)
                .WithOne(m => m.Application)
                .HasForeignKey(e => e.ApplicationId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // =============================================
        // MODULES
        // =============================================
        modelBuilder.Entity<Module>(entity =>
        {
            entity.ToTable("modules");
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Id)
                .HasColumnName("id")
                .ValueGeneratedOnAdd();

            entity.Property(e => e.ApplicationId)
                .HasColumnName("application_id")
                .IsRequired();

            entity.Property(e => e.ModuleType)
                .HasColumnName("module_type")
                .HasConversion<int>()
                .IsRequired();

            entity.Property(e => e.Version)
                .HasColumnName("version")
                .IsRequired();

            entity.Property(e => e.Name)
                .HasColumnName("name")
                .HasMaxLength(255)
                .IsRequired();

            entity.Property(e => e.Description)
                .HasColumnName("description");

            entity.Property(e => e.LockedBy)
                .HasColumnName("locked_by")
                .HasMaxLength(255);

            entity.Property(e => e.CreatedDate)
                 .HasColumnName("created_date")
                 .HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.Property(e => e.ModifiedDate)
                .HasColumnName("modified_date")
                .HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.HasIndex(e => e.ApplicationId)
                .HasDatabaseName("idx_modules_application");

            entity.HasIndex(e => e.ModuleType)
                .HasDatabaseName("idx_modules_type");

            entity.HasIndex(e => e.Name)
                .HasDatabaseName("idx_modules_name");

        });

        // =============================================
        // PROCESS MODULES
        // =============================================
        modelBuilder.Entity<ProcessModule>(entity =>
        {
            entity.ToTable("process_modules");
            entity.HasKey(e => e.ModuleId);

            entity.Property(e => e.ModuleId)
                .HasColumnName("module_id");

            entity.Property(e => e.Subtype)
                .HasColumnName("subtype")
                .HasMaxLength(100);

            entity.Property(e => e.Remote)
                .HasColumnName("remote")
                .HasDefaultValue(false);

            entity.Property(e => e.DynamicCall)
                .HasColumnName("dynamic_call")
                .HasDefaultValue(false);

            entity.Property(e => e.Comment)
                .HasColumnName("comment");

            entity.HasOne(e => e.Module)
                .WithOne()
                .HasForeignKey<ProcessModule>(e => e.ModuleId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasMany(e => e.Details)
                .WithOne(d => d.ProcessModule)
                .HasForeignKey(d => d.ProcessModuleId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // =============================================
        // PROCESS MODULE DETAILS
        // =============================================
        modelBuilder.Entity<ProcessModuleDetail>(entity =>
        {
            entity.ToTable("process_module_details");
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Id)
                .HasColumnName("id")
                .ValueGeneratedOnAdd();

            entity.Property(e => e.ProcessModuleId)
                .HasColumnName("process_module_id")
                .IsRequired();

            entity.Property(e => e.Sequence)
                .HasColumnName("sequence")
                .IsRequired();

            entity.Property(e => e.LabelName)
                .HasColumnName("label_name")
                .HasMaxLength(255);

            entity.Property(e => e.ActionType)
                .HasColumnName("action_type")
                .HasConversion<int?>();

            entity.Property(e => e.ActionId)
                .HasColumnName("action_id");

            entity.Property(e => e.ActionModuleType)
                .HasColumnName("action_module_type")
                .HasConversion<int?>();

            entity.Property(e => e.PassLabel)
                .HasColumnName("pass_label")
                .HasMaxLength(255);

            entity.Property(e => e.FailLabel)
                .HasColumnName("fail_label")
                .HasMaxLength(255);

            entity.Property(e => e.CommentedFlag)
                .HasColumnName("commented_flag")
                .HasDefaultValue(false);

            entity.Property(e => e.Comment)
                .HasColumnName("comment");

            entity.Property(e => e.CreatedDate)
                .HasColumnName("created_date")
                .HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.HasIndex(e => e.ProcessModuleId)
                .HasDatabaseName("idx_process_details_module");

            entity.HasIndex(e => new { e.ProcessModuleId, e.Sequence })
                .IsUnique()
                .HasDatabaseName("uq_process_sequence");

            entity.HasIndex(e => new { e.ProcessModuleId, e.LabelName })
                .IsUnique()
                .HasDatabaseName("uq_process_label");
        });

        // =============================================
        // DATABASE ACTION MODULES
        // =============================================
        modelBuilder.Entity<DatabaseActionModule>(entity =>
        {
            entity.ToTable("database_action_modules");
            entity.HasKey(e => e.ModuleId);

            entity.Property(e => e.ModuleId)
                .HasColumnName("module_id");

            entity.Property(e => e.Statement)
                .HasColumnName("statement")
                .IsRequired();

            entity.HasOne(e => e.Module)
                .WithOne()
                .HasForeignKey<DatabaseActionModule>(e => e.ModuleId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // =============================================
        // FIELD MODULES
        // =============================================
        modelBuilder.Entity<FieldModule>(entity =>
        {
            entity.ToTable("field_modules");
            entity.HasKey(e => e.ModuleId);

            entity.Property(e => e.ModuleId)
                .HasColumnName("module_id");

            entity.Property(e => e.FieldType)
                .HasColumnName("field_type")
                .HasConversion<int>()
                .IsRequired();

            entity.Property(e => e.DefaultValue)
                .HasColumnName("default_value");

            entity.HasOne(e => e.Module)
                .WithOne()
                .HasForeignKey<FieldModule>(e => e.ModuleId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
