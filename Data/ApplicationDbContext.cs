using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using RegistrDN.Models.Entities;

namespace RegistrDN.Data;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<DnDocumentEntity> DnDocuments { get; set; }
    public DbSet<GstEntity> GstRecords { get; set; }
    public DbSet<GptEntity> GptRecords { get; set; }
    public DbSet<GfEntity> GfRecords { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<ApplicationUser>(entity =>
        {
            entity.ToTable("AspNetUsers");
            entity.Property(e => e.FullName).HasMaxLength(200);
            entity.Property(e => e.HospitalCode).HasMaxLength(10);
            entity.Property(e => e.RegionCode).HasMaxLength(10);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("GETDATE()");
            entity.Property(e => e.IsActive).HasDefaultValue(true);
        });

        modelBuilder.Entity<DnDocumentEntity>(entity =>
        {
            entity.ToTable("DN_DOCUMENTS");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("ID");
            entity.Property(e => e.FileName).IsRequired().HasMaxLength(255).HasColumnName("FILE_NAME");
            entity.Property(e => e.FileType).IsRequired().HasMaxLength(10).HasColumnName("FILE_TYPE");
            entity.Property(e => e.XmlContent).IsRequired().HasColumnName("XML_CONTENT");
            entity.Property(e => e.RegionCode).HasMaxLength(10).HasColumnName("REGION_CODE");
            entity.Property(e => e.HospitalCode).HasMaxLength(10).HasColumnName("HOSPITAL_CODE");
            entity.Property(e => e.FileDate).HasColumnName("FILE_DATE");
            entity.Property(e => e.RecordsCount).HasColumnName("RECORDS_COUNT");
            entity.Property(e => e.Version).HasMaxLength(50).HasColumnName("VERSION");
            entity.Property(e => e.FileNumber).HasColumnName("FILE_NUMBER");
            entity.Property(e => e.ValidatedEnpCount).HasColumnName("VALIDATED_ENP_COUNT");
            entity.Property(e => e.Period).HasMaxLength(10).HasColumnName("PERIOD");
            entity.Property(e => e.IsValid).HasColumnName("IS_VALID");
            entity.Property(e => e.ValidationErrors).HasColumnName("VALIDATION_ERRORS");
            entity.Property(e => e.UploadDate).HasColumnName("UPLOAD_DATE").HasDefaultValueSql("GETDATE()");
            entity.Property(e => e.UploadedBy).HasMaxLength(50).HasColumnName("UPLOADED_BY");
            entity.Property(e => e.CreatedAt).HasColumnName("CREATED_AT").HasDefaultValueSql("GETDATE()");

            entity.HasIndex(e => e.FileType).HasDatabaseName("IX_DN_DOCUMENTS_FILE_TYPE");
            entity.HasIndex(e => e.UploadDate).HasDatabaseName("IX_DN_DOCUMENTS_UPLOAD_DATE");
            entity.HasIndex(e => e.RegionCode).HasDatabaseName("IX_DN_DOCUMENTS_REGION_CD");
            entity.HasIndex(e => e.HospitalCode).HasDatabaseName("IX_DN_DOCUMENTS_HOSPITAL_CODE");
            entity.HasIndex(e => e.Period).HasDatabaseName("IX_DN_DOCUMENTS_PERIOD");
        });


        modelBuilder.Entity<GstEntity>(entity =>
        {
            entity.ToTable("GST_RECORDS");
            entity.HasKey(e => e.Id);
            
            entity.Property(e => e.Id).HasColumnName("ID");
            entity.Property(e => e.CodeL).IsRequired().HasMaxLength(50).HasColumnName("CODE_L");
            entity.Property(e => e.ENP).IsRequired().HasMaxLength(20).HasColumnName("ENP");
            entity.Property(e => e.CodePinf).IsRequired().HasMaxLength(10).HasColumnName("CODE_PINF");
            entity.Property(e => e.DnPatientId).HasMaxLength(50).HasColumnName("DN_PATIENT_ID");
            entity.Property(e => e.DiagCode).IsRequired().HasMaxLength(10).HasColumnName("DIAG_CODE");
            entity.Property(e => e.DiagDate).HasColumnName("DIAG_DATE");
            entity.Property(e => e.DateDnIn).HasColumnName("DATE_DN_IN");
            entity.Property(e => e.DateDnOut).HasColumnName("DATE_DN_OUT");
            entity.Property(e => e.DnPrvs).HasColumnName("DN_PRVS");
            entity.Property(e => e.LastSlMcod).HasColumnName("LAST_SL_MCOD");
            entity.Property(e => e.LastSlNhistory).HasMaxLength(50).HasColumnName("LAST_SL_NHISTORY");
            entity.Property(e => e.LastSlDate).HasColumnName("LAST_SL_DATE");
            entity.Property(e => e.StatusDnIn).HasColumnName("STATUS_DN_IN");
            entity.Property(e => e.ReasonDnOut).HasMaxLength(50).HasColumnName("REASON_DN_OUT");
            entity.Property(e => e.ReasonDnIn).HasColumnName("REASON_DN_IN");
            entity.Property(e => e.Mcod).HasMaxLength(10).HasColumnName("MCOD");
            entity.Property(e => e.MoAssign).HasColumnName("MO_ASSIGN");
            entity.Property(e => e.DateChecking).HasColumnName("DATE_CHECKING");
            entity.Property(e => e.DocumentId).HasColumnName("DOCUMENT_ID");
            entity.Property(e => e.CreatedAt).HasColumnName("CREATED_AT").HasDefaultValueSql("GETDATE()");
            entity.Property(e => e.UpdatedAt).HasColumnName("UPDATED_AT");

            entity.HasOne(e => e.Document)
                  .WithMany(d => d.GstRecords)
                  .HasForeignKey(e => e.DocumentId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(e => e.ENP).HasDatabaseName("IX_GST_RECORDS_ENP");
            entity.HasIndex(e => e.CodeL).HasDatabaseName("IX_GST_RECORDS_CODE_L");
            entity.HasIndex(e => e.DocumentId).HasDatabaseName("IX_GST_RECORDS_DOCUMENT_ID");
            entity.HasIndex(e => e.DnPatientId).HasDatabaseName("IX_GST_RECORDS_DN_PATIENT_ID");
        });


        modelBuilder.Entity<GptEntity>(entity =>
        {
            entity.ToTable("GPT_RECORDS");
            entity.HasKey(e => e.Id);
            
            entity.Property(e => e.Id).HasColumnName("ID");
            entity.Property(e => e.CodeP).IsRequired().HasMaxLength(50).HasColumnName("CODE_P");
            entity.Property(e => e.DnPatientId).HasMaxLength(50).HasColumnName("DN_PATIENT_ID");
            entity.Property(e => e.ENP).IsRequired().HasMaxLength(20).HasColumnName("ENP");
            entity.Property(e => e.CodePinf).IsRequired().HasMaxLength(10).HasColumnName("CODE_PINF");
            entity.Property(e => e.McodPlan).IsRequired().HasMaxLength(10).HasColumnName("MCOD_PLAN");
            entity.Property(e => e.MoPodrId).HasMaxLength(50).HasColumnName("MO_PODR_ID");
            entity.Property(e => e.MedAreaCode).HasMaxLength(50).HasColumnName("MED_AREA_CODE");
            entity.Property(e => e.MoAssign).HasColumnName("MO_ASSIGN");
            entity.Property(e => e.EndDateInf).HasColumnName("END_DATE_INF");
            entity.Property(e => e.PrimaryInf).HasColumnName("PRIMARY_INF");
            entity.Property(e => e.DsCode).HasMaxLength(10).HasColumnName("DS_CODE");
            entity.Property(e => e.PlanDateStart).HasColumnName("PLAN_DATE_START");
            entity.Property(e => e.PlanDateEnd).HasColumnName("PLAN_DATE_END");
            entity.Property(e => e.DocumentId).HasColumnName("DOCUMENT_ID");
            entity.Property(e => e.CreatedAt).HasColumnName("CREATED_AT").HasDefaultValueSql("GETDATE()");
            entity.Property(e => e.UpdatedAt).HasColumnName("UPDATED_AT");

            entity.HasOne(e => e.Document)
                  .WithMany(d => d.GptRecords)
                  .HasForeignKey(e => e.DocumentId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(e => e.ENP).HasDatabaseName("IX_GPT_RECORDS_ENP");
            entity.HasIndex(e => e.CodeP).HasDatabaseName("IX_GPT_RECORDS_CODE_P");
            entity.HasIndex(e => e.DocumentId).HasDatabaseName("IX_GPT_RECORDS_DOCUMENT_ID");
            entity.HasIndex(e => e.DnPatientId).HasDatabaseName("IX_GPT_RECORDS_DN_PATIENT_ID");
        });

        modelBuilder.Entity<GfEntity>(entity =>
        {
            entity.ToTable("GF_RECORDS");
            entity.HasKey(e => e.Id);
            
            entity.Property(e => e.Id).HasColumnName("ID");
            entity.Property(e => e.DnPatientId).HasMaxLength(50).HasColumnName("DN_PATIENT_ID");
            entity.Property(e => e.ENP).IsRequired().HasMaxLength(20).HasColumnName("ENP");
            entity.Property(e => e.Gender).HasColumnName("GENDER");
            entity.Property(e => e.BirthDate).HasColumnName("BIRTH_DATE");
            entity.Property(e => e.Smo).HasMaxLength(10).HasColumnName("SMO");
            entity.Property(e => e.AttachMcode).HasMaxLength(10).HasColumnName("ATTACH_MCODE");
            entity.Property(e => e.AttachDate).HasColumnName("ATTACH_DATE");
            entity.Property(e => e.SmoRegionCode).HasMaxLength(10).HasColumnName("SMO_REGION_CD");
            entity.Property(e => e.GroupRhCode).HasColumnName("GROUP_RH_CD");
            entity.Property(e => e.GroupRhDs).HasMaxLength(10).HasColumnName("GROUP_RH_DS");
            entity.Property(e => e.DnPrvs).HasColumnName("DN_PRVS");
            entity.Property(e => e.GroupRhProfile).HasMaxLength(255).HasColumnName("GROUP_RH_PROFILE");
            entity.Property(e => e.GroupRhName).HasMaxLength(255).HasColumnName("GROUP_RH_NAME");
            entity.Property(e => e.DnRuleInName).HasMaxLength(255).HasColumnName("DN_RULE_IN_NAME");
            entity.Property(e => e.DnListPeriodCode).HasColumnName("DN_LIST_PERIOD_CD");
            entity.Property(e => e.DnListFilename).HasMaxLength(50).HasColumnName("DN_LIST_FILENAME");
            entity.Property(e => e.CodeL).HasMaxLength(50).HasColumnName("CODE_L");
            entity.Property(e => e.DnListResultCode).HasMaxLength(10).HasColumnName("DN_LIST_RESULT_CODE");
            entity.Property(e => e.DnListDateChecking).HasColumnName("DN_LIST_DATE_CHECKING");
            entity.Property(e => e.DnListResultDescr).HasColumnName("DN_LIST_RESULT_DESCR");
            entity.Property(e => e.DnPlanPeriod).HasMaxLength(10).HasColumnName("DN_PLAN_PERIOD");
            entity.Property(e => e.DnPlanFilename).HasMaxLength(50).HasColumnName("DN_PLAN_FILENAME");
            entity.Property(e => e.CodeP).HasMaxLength(50).HasColumnName("CODE_P");
            entity.Property(e => e.DnPlanResultCode).HasColumnName("DN_PLAN_RESULT_CODE");
            entity.Property(e => e.DnPlanDateChecking).HasColumnName("DN_PLAN_DATE_CHECKING");
            entity.Property(e => e.DnPlanResultDescr).HasColumnName("DN_PLAN_RESULT_DESCR");
            entity.Property(e => e.TriggerSchetnFilename).HasMaxLength(50).HasColumnName("TRIGGER_SCHET_FILENAME");
            entity.Property(e => e.TriggerSchetnCode).HasMaxLength(10).HasColumnName("TRIGGER_SCHET_CODE");
            entity.Property(e => e.TriggerNschet).HasMaxLength(20).HasColumnName("TRIGGER_NSCHET");
            entity.Property(e => e.TriggerDschet).HasColumnName("TRIGGER_DSCHET");
            entity.Property(e => e.TriggerIdCase).HasMaxLength(50).HasColumnName("TRIGGER_IDCASE");
            entity.Property(e => e.TriggerSlId).HasMaxLength(50).HasColumnName("TRIGGER_SL_ID");
            entity.Property(e => e.TriggerSlNhistory).HasMaxLength(50).HasColumnName("TRIGGER_SL_NHISTORY");
            entity.Property(e => e.TriggerDsCd).HasMaxLength(10).HasColumnName("TRIGGER_DS_CD");
            entity.Property(e => e.TriggerMcode).HasMaxLength(10).HasColumnName("TRIGGER_MCODE");
            entity.Property(e => e.TriggerDt).HasColumnName("TRIGGER_DT");
            entity.Property(e => e.InsertDttm).HasColumnName("INSERT_DTTM");
            entity.Property(e => e.UpdateDttm).HasColumnName("UPDATE_DTTM");
            entity.Property(e => e.DocumentId).HasColumnName("DOCUMENT_ID");
            entity.Property(e => e.CreatedAt).HasColumnName("CREATED_AT").HasDefaultValueSql("GETDATE()");

            entity.HasOne(e => e.Document)
                  .WithMany(d => d.GfRecords)
                  .HasForeignKey(e => e.DocumentId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(e => e.ENP).HasDatabaseName("IX_GF_RECORDS_ENP");
            entity.HasIndex(e => e.DocumentId).HasDatabaseName("IX_GF_RECORDS_DOCUMENT_ID");
            entity.HasIndex(e => e.DnPatientId).HasDatabaseName("IX_GF_RECORDS_DN_PATIENT_ID");
            entity.HasIndex(e => e.CodeL).HasDatabaseName("IX_GF_RECORDS_CODE_L");
            entity.HasIndex(e => e.CodeP).HasDatabaseName("IX_GF_RECORDS_CODE_P");
        });
    }
}