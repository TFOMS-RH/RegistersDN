using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using RegistrDN.Models.Entities;
using RegistrDN.Models.Enums;


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
    public DbSet<DsDnEntity> DsDn { get; set; }
    public DbSet<DspnEntity> DspnRecords { get; set; }
    public DbSet<ProfEntity> ProfRecords { get; set; }
    public DbSet<ProfMerEntity> ProfMerRecords { get; set; }
    public DbSet<ImportResponseEntity> ImportResponses { get; set; }
    public DbSet<DfEntity> DfRecords { get; set; }

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
            
            // НОВЫЕ ПОЛЯ
            entity.Property(e => e.Status)
                .HasColumnName("STATUS")
                .HasDefaultValue(DocumentStatus.Uploaded)
                .HasConversion<int>();
            
            entity.Property(e => e.CheckedAt).HasColumnName("CHECKED_AT");
            entity.Property(e => e.CheckedBy).HasMaxLength(50).HasColumnName("CHECKED_BY");
            entity.Property(e => e.CheckAttempts).HasColumnName("CHECK_ATTEMPTS").HasDefaultValue(0);
            
            entity.Property(e => e.UploadDate).HasColumnName("UPLOAD_DATE").HasDefaultValueSql("GETDATE()");
            entity.Property(e => e.UploadedBy).HasMaxLength(50).HasColumnName("UPLOADED_BY");
            entity.Property(e => e.CreatedAt).HasColumnName("CREATED_AT").HasDefaultValueSql("GETDATE()");

            entity.HasIndex(e => e.FileType).HasDatabaseName("IX_DN_DOCUMENTS_FILE_TYPE");
            entity.HasIndex(e => e.UploadDate).HasDatabaseName("IX_DN_DOCUMENTS_UPLOAD_DATE");
            entity.HasIndex(e => e.RegionCode).HasDatabaseName("IX_DN_DOCUMENTS_REGION_CD");
            entity.HasIndex(e => e.HospitalCode).HasDatabaseName("IX_DN_DOCUMENTS_HOSPITAL_CODE");
            entity.HasIndex(e => e.Period).HasDatabaseName("IX_DN_DOCUMENTS_PERIOD");
            entity.HasIndex(e => e.Status).HasDatabaseName("IX_DN_DOCUMENTS_STATUS");
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

        modelBuilder.Entity<DsDnEntity>(entity =>
        {
            entity.ToTable("DS_DN");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("Id");
            entity.Property(e => e.DiagCode).IsRequired().HasMaxLength(10).HasColumnName("DiagCode");
            entity.Property(e => e.DiagName).HasMaxLength(255).HasColumnName("DiagName");
            entity.Property(e => e.IsActive).HasColumnName("IsActive").HasDefaultValue(true);  // <-- ИСПРАВЛЕНО!
            entity.Property(e => e.CreatedAt).HasColumnName("CreatedAt").HasDefaultValueSql("GETDATE()");
            entity.Property(e => e.UpdatedAt).HasColumnName("UpdatedAt");
        });

        // ============================================
        // DSPN_RECORDS
        // ============================================
        modelBuilder.Entity<DspnEntity>(entity =>
        {
            entity.ToTable("DSPN_RECORDS");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("Id");
            entity.Property(e => e.N_Zap).HasColumnName("N_ZAP");
            entity.Property(e => e.PacientId).HasColumnName("PACIENT_ID").HasMaxLength(50);
            entity.Property(e => e.Smo).HasColumnName("SMO").HasMaxLength(5);
            entity.Property(e => e.Spolis).HasColumnName("SPOLIS").HasMaxLength(10);
            entity.Property(e => e.Vpolis).HasColumnName("VPOLIS");
            entity.Property(e => e.Npolis).HasColumnName("NPOLIS").IsRequired().HasMaxLength(20);
            entity.Property(e => e.Fam).HasColumnName("FAM").IsRequired().HasMaxLength(40);
            entity.Property(e => e.Im).HasColumnName("IM").IsRequired().HasMaxLength(40);
            entity.Property(e => e.Ot).HasColumnName("OT").HasMaxLength(40);
            entity.Property(e => e.Dr).HasColumnName("DR");
            entity.Property(e => e.W).HasColumnName("W");
            entity.Property(e => e.Adres).HasColumnName("ADRES").HasMaxLength(100);
            entity.Property(e => e.Tel).HasColumnName("TEL").HasMaxLength(20);
            entity.Property(e => e.MoP).HasColumnName("MO_P").IsRequired().HasMaxLength(6);
            entity.Property(e => e.DiagCode).HasColumnName("DIAG_CODE").IsRequired().HasMaxLength(7);
            entity.Property(e => e.Iddokt).HasColumnName("IDDOKT").IsRequired().HasMaxLength(25);
            entity.Property(e => e.DateDnIn).HasColumnName("DATE_DN_IN");
            entity.Property(e => e.DateDnOut).HasColumnName("DATE_DN_OUT");
            entity.Property(e => e.DiagDate).HasColumnName("DIAG_DATE");
            entity.Property(e => e.DnPrvs).HasColumnName("DN_PRVS");
            entity.Property(e => e.StatusDnIn).HasColumnName("STATUS_DN_IN");
            entity.Property(e => e.ReasonDnOut).HasColumnName("REASON_DN_OUT").HasMaxLength(50);
            entity.Property(e => e.ReasonDnIn).HasColumnName("REASON_DN_IN");
            entity.Property(e => e.McodPlan).HasColumnName("MCOD_PLAN").IsRequired().HasMaxLength(6);
            entity.Property(e => e.MoPodrId).HasColumnName("MO_PODR_ID").HasMaxLength(19);
            entity.Property(e => e.MedAreaCode).HasColumnName("MED_AREA_CODE").HasMaxLength(36);
            entity.Property(e => e.MoAssign).HasColumnName("MO_ASSIGN");
            entity.Property(e => e.DsCode).HasColumnName("DS_CODE").HasMaxLength(7);
            entity.Property(e => e.PlanDateStart).HasColumnName("PLAN_DATE_START");
            entity.Property(e => e.PlanDateEnd).HasColumnName("PLAN_DATE_END");
            entity.Property(e => e.InfType).HasColumnName("INFTYPE");
            entity.Property(e => e.SposobInf).HasColumnName("SPOSOBINF");
            entity.Property(e => e.DataInf).HasColumnName("DATAINF");
            entity.Property(e => e.Period).HasColumnName("PERIOD").IsRequired().HasMaxLength(6);
            entity.Property(e => e.DocumentId).HasColumnName("DOCUMENT_ID");
            entity.Property(e => e.IsProcessed).HasColumnName("IS_PROCESSED");
            entity.Property(e => e.CreatedAt).HasColumnName("CREATED_AT").HasDefaultValueSql("GETDATE()");
            entity.Property(e => e.UpdatedAt).HasColumnName("UPDATED_AT");

            entity.HasOne(e => e.Document)
                .WithMany()
                .HasForeignKey(e => e.DocumentId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(e => e.Npolis).HasDatabaseName("IX_DSPN_RECORDS_ENP");
            entity.HasIndex(e => e.DocumentId).HasDatabaseName("IX_DSPN_RECORDS_DOCUMENT_ID");
            entity.HasIndex(e => e.Period).HasDatabaseName("IX_DSPN_RECORDS_PERIOD");
            entity.HasIndex(e => e.DiagCode).HasDatabaseName("IX_DSPN_RECORDS_DIAG_CODE");
            entity.HasIndex(e => e.N_Zap).HasDatabaseName("IX_DSPN_RECORDS_N_ZAP");
        });

        // ============================================
        // PROF_RECORDS
        // ============================================
        modelBuilder.Entity<ProfEntity>(entity =>
        {
            entity.ToTable("PROF_RECORDS");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("Id");
            entity.Property(e => e.NomerZ).HasColumnName("NOMER_Z");
            entity.Property(e => e.PersonId).HasColumnName("PERSON_ID").HasMaxLength(16);
            entity.Property(e => e.SmoCod).HasColumnName("SMOCOD").IsRequired().HasMaxLength(5);
            entity.Property(e => e.Enp).HasColumnName("ENP").HasMaxLength(16);
            entity.Property(e => e.Fam).HasColumnName("FAM").IsRequired().HasMaxLength(40);
            entity.Property(e => e.Im).HasColumnName("IM").IsRequired().HasMaxLength(40);
            entity.Property(e => e.Ot).HasColumnName("OT").HasMaxLength(40);
            entity.Property(e => e.Dr).HasColumnName("DR");
            entity.Property(e => e.W).HasColumnName("W");
            entity.Property(e => e.DocType).HasColumnName("DOCTYPE").HasMaxLength(2);
            entity.Property(e => e.DocSer).HasColumnName("DOCSER").HasMaxLength(10);
            entity.Property(e => e.DocNum).HasColumnName("DOCNUM").HasMaxLength(20);
            entity.Property(e => e.Snils).HasColumnName("SNILS").HasMaxLength(14);
            entity.Property(e => e.Vpolis).HasColumnName("VPOLIS");
            entity.Property(e => e.Spolis).HasColumnName("SPOLIS").HasMaxLength(10);
            entity.Property(e => e.Npolis).HasColumnName("NPOLIS").IsRequired().HasMaxLength(16);
            entity.Property(e => e.Tel).HasColumnName("TEL").HasMaxLength(20);
            entity.Property(e => e.Iddokt).HasColumnName("IDDOKT").IsRequired().HasMaxLength(14);
            entity.Property(e => e.Adres).HasColumnName("ADRES").HasMaxLength(100);
            entity.Property(e => e.KatLg).HasColumnName("KAT_LG");
            entity.Property(e => e.Year).HasColumnName("YEAR");
            entity.Property(e => e.Comment).HasColumnName("COMMENT").HasMaxLength(250);
            entity.Property(e => e.Period).HasColumnName("PERIOD").IsRequired().HasMaxLength(6);
            entity.Property(e => e.DocumentId).HasColumnName("DOCUMENT_ID");
            entity.Property(e => e.IsProcessed).HasColumnName("IS_PROCESSED");
            entity.Property(e => e.CreatedAt).HasColumnName("CREATED_AT").HasDefaultValueSql("GETDATE()");
            entity.Property(e => e.UpdatedAt).HasColumnName("UPDATED_AT");

            entity.HasOne(e => e.Document)
                .WithMany()
                .HasForeignKey(e => e.DocumentId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(e => e.Npolis).HasDatabaseName("IX_PROF_RECORDS_ENP");
            entity.HasIndex(e => e.DocumentId).HasDatabaseName("IX_PROF_RECORDS_DOCUMENT_ID");
            entity.HasIndex(e => e.Period).HasDatabaseName("IX_PROF_RECORDS_PERIOD");
            entity.HasIndex(e => e.NomerZ).HasDatabaseName("IX_PROF_RECORDS_NOMER_Z");
        });

        // ============================================
        // PROF_MER_RECORDS
        // ============================================
        modelBuilder.Entity<ProfMerEntity>(entity =>
        {
            entity.ToTable("PROF_MER_RECORDS");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("Id");
            entity.Property(e => e.ProfRecordId).HasColumnName("PROF_RECORD_ID");
            entity.Property(e => e.Month).HasColumnName("MONTH");
            entity.Property(e => e.Disp).HasColumnName("DISP").IsRequired().HasMaxLength(3);
            entity.Property(e => e.CreatedAt).HasColumnName("CREATED_AT").HasDefaultValueSql("GETDATE()");

            entity.HasOne(e => e.ProfRecord)
                .WithMany(p => p.ProfMerRecords)
                .HasForeignKey(e => e.ProfRecordId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(e => e.ProfRecordId).HasDatabaseName("IX_PROF_MER_RECORDS_PROF_RECORD_ID");
        });

        // ============================================
        // IMPORT_RESPONSES
        // ============================================
        modelBuilder.Entity<ImportResponseEntity>(entity =>
        {
            entity.ToTable("IMPORT_RESPONSES");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("Id");
            entity.Property(e => e.SourceFileType).HasColumnName("SOURCE_FILE_TYPE").IsRequired().HasMaxLength(10);
            entity.Property(e => e.SourceDocumentId).HasColumnName("SOURCE_DOCUMENT_ID");
            entity.Property(e => e.ResponseXml).HasColumnName("RESPONSE_XML").IsRequired();
            entity.Property(e => e.ResponseFileName).HasColumnName("RESPONSE_FILE_NAME").IsRequired().HasMaxLength(50);
            entity.Property(e => e.RecordsTotal).HasColumnName("RECORDS_TOTAL");
            entity.Property(e => e.RecordsApproved).HasColumnName("RECORDS_APPROVED");
            entity.Property(e => e.RecordsRejected).HasColumnName("RECORDS_REJECTED");
            entity.Property(e => e.CreatedAt).HasColumnName("CREATED_AT").HasDefaultValueSql("GETDATE()");
            entity.Property(e => e.DownloadedAt).HasColumnName("DOWNLOADED_AT");

            entity.HasOne(e => e.SourceDocument)
                .WithMany()
                .HasForeignKey(e => e.SourceDocumentId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(e => e.SourceDocumentId).HasDatabaseName("IX_IMPORT_RESPONSES_SOURCE_DOCUMENT_ID");
            entity.HasIndex(e => e.SourceFileType).HasDatabaseName("IX_IMPORT_RESPONSES_SOURCE_FILE_TYPE");
        });


        modelBuilder.Entity<DfEntity>(entity =>
        {
            entity.ToTable("DF_RECORDS");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("ID");
            entity.Property(e => e.MasterPersonId).HasMaxLength(50).HasColumnName("MASTER_PERSON_ID");
            entity.Property(e => e.ENP).IsRequired().HasMaxLength(20).HasColumnName("ENP");
            entity.Property(e => e.BirthDate).HasColumnName("BIRTH_DATE");
            entity.Property(e => e.PolName).HasMaxLength(10).HasColumnName("POL_NAME");
            entity.Property(e => e.Smo).HasMaxLength(5).HasColumnName("SMO");
            entity.Property(e => e.AttachMoCd).HasMaxLength(10).HasColumnName("ATTACH_MO_CD");
            entity.Property(e => e.AttachMoVers).HasMaxLength(10).HasColumnName("ATTACH_MO_VERS");
            entity.Property(e => e.AttachDate).HasColumnName("ATTACH_DATE");
            entity.Property(e => e.SmoRegionCd).HasMaxLength(10).HasColumnName("SMO_REGION_CD");
            entity.Property(e => e.DispansType).HasMaxLength(10).HasColumnName("DISPANS_TYPE");
            entity.Property(e => e.DispansTypeName).HasMaxLength(100).HasColumnName("DISPANS_TYPE_NAME");
            entity.Property(e => e.DispansStatus).HasMaxLength(10).HasColumnName("DISPANS_STATUS");
            entity.Property(e => e.Period).HasMaxLength(10).HasColumnName("PERIOD");
            entity.Property(e => e.DocumentId).HasColumnName("DOCUMENT_ID");
            entity.Property(e => e.CreatedAt).HasColumnName("CREATED_AT").HasDefaultValueSql("GETDATE()");

            entity.HasOne(e => e.Document)
                .WithMany()
                .HasForeignKey(e => e.DocumentId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(e => e.ENP).HasDatabaseName("IX_DF_RECORDS_ENP");
            entity.HasIndex(e => e.DocumentId).HasDatabaseName("IX_DF_RECORDS_DOCUMENT_ID");
            entity.HasIndex(e => e.Period).HasDatabaseName("IX_DF_RECORDS_PERIOD");
        });
    }
}