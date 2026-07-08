using AutoMapper;
using RegistrDN.Models.DTOs.Import;
using RegistrDN.Models.DTOs.Export;
using RegistrDN.Models.Entities;

namespace RegistrDN.Mappings;

public class AutoMapperProfile : Profile
{
    public AutoMapperProfile()
    {

        // GST: IMPORT DTO → ENTITY

        CreateMap<GstImportRecord, GstEntity>()
            .ForMember(dest => dest.DiagDate,
                opt => opt.MapFrom(src => ParseDate(src.DiagDate)))
            .ForMember(dest => dest.DateDnIn,
                opt => opt.MapFrom(src => ParseDate(src.DateDnIn)))
            .ForMember(dest => dest.DateDnOut,
                opt => opt.MapFrom(src => ParseDate(src.DateDnOut)))
            .ForMember(dest => dest.LastSlDate,
                opt => opt.MapFrom(src => ParseDate(src.LastSlDate)))
            .ForMember(dest => dest.DateChecking,
                opt => opt.MapFrom(src => ParseDate(src.DateChecking) ?? DateTime.Now))
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.DocumentId, opt => opt.Ignore())
            .ForMember(dest => dest.Document, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore());


        // GSM: IMPORT DTO → ENTITY (полный аналог GST)

        CreateMap<GsmImportRecord, GstEntity>()
            .ForMember(dest => dest.DiagDate,
                opt => opt.MapFrom(src => ParseDate(src.DiagDate)))
            .ForMember(dest => dest.DateDnIn,
                opt => opt.MapFrom(src => ParseDate(src.DateDnIn)))
            .ForMember(dest => dest.DateDnOut,
                opt => opt.MapFrom(src => ParseDate(src.DateDnOut)))
            .ForMember(dest => dest.LastSlDate,
                opt => opt.MapFrom(src => ParseDate(src.LastSlDate)))
            .ForMember(dest => dest.DateChecking,
                opt => opt.MapFrom(src => ParseDate(src.DateChecking) ?? DateTime.Now))
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.DocumentId, opt => opt.Ignore())
            .ForMember(dest => dest.Document, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore());


        // GPT: IMPORT DTO → ENTITY

        CreateMap<GptImportRecord, GptEntity>()
            .ForMember(dest => dest.EndDateInf,
                opt => opt.MapFrom(src => ParseDate(src.EndDateInf) ?? DateTime.Now))
            .ForMember(dest => dest.PlanDateStart,
                opt => opt.MapFrom(src => ParseDate(src.PlanDateStart) ?? DateTime.Now))
            .ForMember(dest => dest.PlanDateEnd,
                opt => opt.MapFrom(src => ParseDate(src.PlanDateEnd) ?? DateTime.Now))
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.DocumentId, opt => opt.Ignore())
            .ForMember(dest => dest.Document, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore());


        // GPM: IMPORT DTO → ENTITY (полный аналог GPT)

        CreateMap<GpmImportRecord, GptEntity>()
            .ForMember(dest => dest.EndDateInf,
                opt => opt.MapFrom(src => ParseDate(src.EndDateInf) ?? DateTime.Now))
            .ForMember(dest => dest.PlanDateStart,
                opt => opt.MapFrom(src => ParseDate(src.PlanDateStart) ?? DateTime.Now))
            .ForMember(dest => dest.PlanDateEnd,
                opt => opt.MapFrom(src => ParseDate(src.PlanDateEnd) ?? DateTime.Now))
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.DocumentId, opt => opt.Ignore())
            .ForMember(dest => dest.Document, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore());


        // GF: IMPORT DTO → ENTITY

        CreateMap<GfImportRecord, GfEntity>()
            .ForMember(dest => dest.DnPatientId,
                opt => opt.MapFrom(src => src.DnPatientId))
            .ForMember(dest => dest.ENP,
                opt => opt.MapFrom(src => src.ENP))
            .ForMember(dest => dest.Gender,
                opt => opt.MapFrom(src => src.Gender))
            .ForMember(dest => dest.BirthDate,
                opt => opt.MapFrom(src => ParseDate(src.BirthDate)))
            .ForMember(dest => dest.Smo,
                opt => opt.MapFrom(src => src.Smo))
            .ForMember(dest => dest.AttachMcode,
                opt => opt.MapFrom(src => src.AttachMcode))
            .ForMember(dest => dest.AttachDate,
                opt => opt.MapFrom(src => ParseDate(src.AttachDate)))
            .ForMember(dest => dest.SmoRegionCode,
                opt => opt.MapFrom(src => src.SmoRegionCode))
            .ForMember(dest => dest.GroupRhCode,
                opt => opt.MapFrom(src => src.GroupRhCode))
            .ForMember(dest => dest.GroupRhDs,
                opt => opt.MapFrom(src => src.GroupRhDs))
            .ForMember(dest => dest.DnPrvs,
                opt => opt.MapFrom(src => src.DnPrvs))
            .ForMember(dest => dest.GroupRhProfile,
                opt => opt.MapFrom(src => src.GroupRhProfile))
            .ForMember(dest => dest.GroupRhName,
                opt => opt.MapFrom(src => src.GroupRhName))
            .ForMember(dest => dest.DnRuleInName,
                opt => opt.MapFrom(src => src.DnRuleInName))
            .ForMember(dest => dest.DnListPeriodCode,
                opt => opt.MapFrom(src => src.DnList != null ? src.DnList.DnListPeriodCode : (int?)null))
            .ForMember(dest => dest.DnListFilename,
                opt => opt.MapFrom(src => src.DnList != null ? src.DnList.DnListFilename : null))
            .ForMember(dest => dest.CodeL,
                opt => opt.MapFrom(src => src.DnList != null ? src.DnList.CodeL : null))
            .ForMember(dest => dest.DnListResultCode,
                opt => opt.MapFrom(src => src.DnList != null ? src.DnList.DnListResultCode : null))
            .ForMember(dest => dest.DnListDateChecking,
                opt => opt.MapFrom(src => src.DnList != null ? ParseDate(src.DnList.DnListDateChecking) : null))
            .ForMember(dest => dest.DnListResultDescr,
                opt => opt.MapFrom(src => src.DnList != null ? src.DnList.DnListResultDescr : null))
            .ForMember(dest => dest.DnPlanPeriod,
                opt => opt.MapFrom(src => src.DnPlan != null ? src.DnPlan.DnPlanPeriod : null))
            .ForMember(dest => dest.DnPlanFilename,
                opt => opt.MapFrom(src => src.DnPlan != null ? src.DnPlan.DnPlanFilename : null))
            .ForMember(dest => dest.CodeP,
                opt => opt.MapFrom(src => src.DnPlan != null ? src.DnPlan.CodeP : null))
            .ForMember(dest => dest.DnPlanResultCode,
                opt => opt.MapFrom(src => src.DnPlan != null ? src.DnPlan.DnPlanResultCode : (int?)null))
            .ForMember(dest => dest.DnPlanDateChecking,
                opt => opt.MapFrom(src => src.DnPlan != null ? ParseDate(src.DnPlan.DnPlanDateChecking) : null))
            .ForMember(dest => dest.DnPlanResultDescr,
                opt => opt.MapFrom(src => src.DnPlan != null ? src.DnPlan.DnPlanResultDescr : null))
            .ForMember(dest => dest.TriggerSchetnFilename,
                opt => opt.MapFrom(src => src.DnGis != null ? src.DnGis.TriggerSchetnFilename : null))
            .ForMember(dest => dest.TriggerSchetnCode,
                opt => opt.MapFrom(src => src.DnGis != null ? src.DnGis.TriggerSchetnCode : null))
            .ForMember(dest => dest.TriggerNschet,
                opt => opt.MapFrom(src => src.DnGis != null ? src.DnGis.TriggerNschet : null))
            .ForMember(dest => dest.TriggerDschet,
                opt => opt.MapFrom(src => src.DnGis != null ? ParseDate(src.DnGis.TriggerDschet) : null))
            .ForMember(dest => dest.TriggerIdCase,
                opt => opt.MapFrom(src => src.DnGis != null ? src.DnGis.TriggerIdCase : null))
            .ForMember(dest => dest.TriggerSlId,
                opt => opt.MapFrom(src => src.DnGis != null ? src.DnGis.TriggerSlId : null))
            .ForMember(dest => dest.TriggerSlNhistory,
                opt => opt.MapFrom(src => src.DnGis != null ? src.DnGis.TriggerSlNhistory : null))
            .ForMember(dest => dest.TriggerDsCd,
                opt => opt.MapFrom(src => src.DnGis != null ? src.DnGis.TriggerDsCd : null))
            .ForMember(dest => dest.TriggerMcode,
                opt => opt.MapFrom(src => src.DnGis != null ? src.DnGis.TriggerMcode : null))
            .ForMember(dest => dest.TriggerDt,
                opt => opt.MapFrom(src => src.DnGis != null ? ParseDate(src.DnGis.TriggerDt) : null))
            .ForMember(dest => dest.InsertDttm,
                opt => opt.MapFrom(src => ParseDate(src.InsertDttm) ?? DateTime.Now))
            .ForMember(dest => dest.UpdateDttm,
                opt => opt.MapFrom(src => ParseDate(src.UpdateDttm) ?? DateTime.Now))
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.DocumentId, opt => opt.Ignore())
            .ForMember(dest => dest.Document, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedAt, opt => opt.Ignore());


        // ENTITY → EXPORT DTO


        // GST: Entity → Export
        CreateMap<GstEntity, GstExportRecord>()
            .ForMember(dest => dest.DiagDate,
                opt => opt.MapFrom(src => src.DiagDate != null ? src.DiagDate.Value.ToString("yyyy-MM-dd") : null))
            .ForMember(dest => dest.DateDnIn,
                opt => opt.MapFrom(src => src.DateDnIn != null ? src.DateDnIn.Value.ToString("yyyy-MM-dd") : null))
            .ForMember(dest => dest.DateDnOut,
                opt => opt.MapFrom(src => src.DateDnOut != null ? src.DateDnOut.Value.ToString("yyyy-MM-dd") : null))
            .ForMember(dest => dest.LastSlDate,
                opt => opt.MapFrom(src => src.LastSlDate != null ? src.LastSlDate.Value.ToString("yyyy-MM-dd") : null))
            .ForMember(dest => dest.DateChecking,
                opt => opt.MapFrom(src => src.DateChecking.ToString("yyyy-MM-dd")));

        // GSM: Entity → Export (полный аналог GST)
        CreateMap<GstEntity, GsmExportRecord>()
            .ForMember(dest => dest.DiagDate,
                opt => opt.MapFrom(src => src.DiagDate != null ? src.DiagDate.Value.ToString("yyyy-MM-dd") : null))
            .ForMember(dest => dest.DateDnIn,
                opt => opt.MapFrom(src => src.DateDnIn != null ? src.DateDnIn.Value.ToString("yyyy-MM-dd") : null))
            .ForMember(dest => dest.DateDnOut,
                opt => opt.MapFrom(src => src.DateDnOut != null ? src.DateDnOut.Value.ToString("yyyy-MM-dd") : null))
            .ForMember(dest => dest.LastSlDate,
                opt => opt.MapFrom(src => src.LastSlDate != null ? src.LastSlDate.Value.ToString("yyyy-MM-dd") : null))
            .ForMember(dest => dest.DateChecking,
                opt => opt.MapFrom(src => src.DateChecking.ToString("yyyy-MM-dd")));

        // GPT: Entity → Export
        CreateMap<GptEntity, GptExportRecord>()
            .ForMember(dest => dest.EndDateInf,
                opt => opt.MapFrom(src => src.EndDateInf.ToString("yyyy-MM-dd")))
            .ForMember(dest => dest.PlanDateStart,
                opt => opt.MapFrom(src => src.PlanDateStart.ToString("yyyy-MM-dd")))
            .ForMember(dest => dest.PlanDateEnd,
                opt => opt.MapFrom(src => src.PlanDateEnd.ToString("yyyy-MM-dd")));

        // GPM: Entity → Export (полный аналог GPT)
        CreateMap<GptEntity, GpmExportRecord>()
            .ForMember(dest => dest.EndDateInf,
                opt => opt.MapFrom(src => src.EndDateInf.ToString("yyyy-MM-dd")))
            .ForMember(dest => dest.PlanDateStart,
                opt => opt.MapFrom(src => src.PlanDateStart.ToString("yyyy-MM-dd")))
            .ForMember(dest => dest.PlanDateEnd,
                opt => opt.MapFrom(src => src.PlanDateEnd.ToString("yyyy-MM-dd")));

        // GF: Entity → Export
        CreateMap<GfEntity, GfExportRecord>()
            .ForMember(dest => dest.DnPatientId,
                opt => opt.MapFrom(src => src.DnPatientId))
            .ForMember(dest => dest.ENP,
                opt => opt.MapFrom(src => src.ENP))
            .ForMember(dest => dest.Gender,
                opt => opt.MapFrom(src => src.Gender))
            .ForMember(dest => dest.BirthDate,
                opt => opt.MapFrom(src => src.BirthDate != null ? src.BirthDate.Value.ToString("yyyy-MM-dd") : null))
            .ForMember(dest => dest.Smo,
                opt => opt.MapFrom(src => src.Smo))
            .ForMember(dest => dest.AttachMcode,
                opt => opt.MapFrom(src => src.AttachMcode))
            .ForMember(dest => dest.AttachDate,
                opt => opt.MapFrom(src => src.AttachDate != null ? src.AttachDate.Value.ToString("yyyy-MM-dd") : null))
            .ForMember(dest => dest.SmoRegionCode,
                opt => opt.MapFrom(src => src.SmoRegionCode))
            .ForMember(dest => dest.GroupRhCode,
                opt => opt.MapFrom(src => src.GroupRhCode))
            .ForMember(dest => dest.GroupRhDs,
                opt => opt.MapFrom(src => src.GroupRhDs))
            .ForMember(dest => dest.DnPrvs,
                opt => opt.MapFrom(src => src.DnPrvs))
            .ForMember(dest => dest.GroupRhProfile,
                opt => opt.MapFrom(src => src.GroupRhProfile))
            .ForMember(dest => dest.GroupRhName,
                opt => opt.MapFrom(src => src.GroupRhName))
            .ForMember(dest => dest.DnRuleInName,
                opt => opt.MapFrom(src => src.DnRuleInName))
            .ForMember(dest => dest.InsertDttm,
                opt => opt.MapFrom(src => src.InsertDttm.ToString("yyyy-MM-dd")))
            .ForMember(dest => dest.UpdateDttm,
                opt => opt.MapFrom(src => src.UpdateDttm.ToString("yyyy-MM-dd")))
            .ForMember(dest => dest.DnGis,
                opt => opt.MapFrom(src => new DnGisExportInfo
                {
                    TriggerSchetnFilename = src.TriggerSchetnFilename,
                    TriggerSchetnCode = src.TriggerSchetnCode,
                    TriggerNschet = src.TriggerNschet,
                    TriggerDschet = src.TriggerDschet != null ? src.TriggerDschet.Value.ToString("yyyy-MM-dd") : null,
                    TriggerIdCase = src.TriggerIdCase,
                    TriggerSlId = src.TriggerSlId,
                    TriggerSlNhistory = src.TriggerSlNhistory,
                    TriggerDsCd = src.TriggerDsCd,
                    TriggerMcode = src.TriggerMcode,
                    TriggerDt = src.TriggerDt != null ? src.TriggerDt.Value.ToString("yyyy-MM-dd") : null
                }))
            .ForMember(dest => dest.DnList,
                opt => opt.MapFrom(src => new DnListExportResult
                {
                    DnListPeriodCode = src.DnListPeriodCode,
                    DnListFilename = src.DnListFilename,
                    CodeL = src.CodeL,
                    DnListResultCode = src.DnListResultCode,
                    DnListDateChecking = src.DnListDateChecking != null ? src.DnListDateChecking.Value.ToString("yyyy-MM-dd") : null,
                    DnListResultDescr = src.DnListResultDescr
                }))
            .ForMember(dest => dest.DnPlan,
                opt => opt.MapFrom(src => new DnPlanExportResult
                {
                    DnPlanPeriod = src.DnPlanPeriod,
                    DnPlanFilename = src.DnPlanFilename,
                    CodeP = src.CodeP,
                    DnPlanResultCode = src.DnPlanResultCode,
                    DnPlanDateChecking = src.DnPlanDateChecking != null ? src.DnPlanDateChecking.Value.ToString("yyyy-MM-dd") : null,
                    DnPlanResultDescr = src.DnPlanResultDescr
                }));
    }

    private DateTime? ParseDate(string? dateString)
    {
        if (string.IsNullOrWhiteSpace(dateString))
            return null;

        if (DateTime.TryParse(dateString, out var date))
            return date;

        return null;
    }
}