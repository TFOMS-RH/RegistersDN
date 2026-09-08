using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using RegistrDN.Data.Repositories;
using RegistrDN.Models.Entities;

namespace RegistrDN.Data;

public interface IUnitOfWork : IDisposable
{
    IRepository<DnDocumentEntity> Documents { get; }
    IRepository<GstEntity> GstRecords { get; }
    IRepository<GptEntity> GptRecords { get; }
    IRepository<GfEntity> GfRecords { get; }
    
    // Новые репозитории
    IRepository<DspnEntity> DspnRecords { get; }
    IRepository<ProfEntity> ProfRecords { get; }
    IRepository<ProfMerEntity> ProfMerRecords { get; }
    IRepository<ImportResponseEntity> ImportResponses { get; }
    IRepository<DfEntity> DfRecords { get; }
    
    // Методы для управления таймаутом
    void SetCommandTimeout(int seconds);
    void ResetCommandTimeout();
    
    Task<int> SaveChangesAsync();
    /// <summary>
    /// Проверяет, существует ли документ с такими же параметрами
    /// </summary>
    Task<bool> DocumentExistsAsync(string fileName, string period, string? hospitalCode, string fileType);
}

public class UnitOfWork : IUnitOfWork
{
    private readonly ApplicationDbContext _context;
    
    private IRepository<DnDocumentEntity>? _documents;
    private IRepository<GstEntity>? _gstRecords;
    private IRepository<GptEntity>? _gptRecords;
    private IRepository<GfEntity>? _gfRecords;
    
    private IRepository<DspnEntity>? _dspnRecords;
    private IRepository<ProfEntity>? _profRecords;
    private IRepository<ProfMerEntity>? _profMerRecords;
    private IRepository<ImportResponseEntity>? _importResponses;
    private IRepository<DfEntity>? _dfRecords;

    public IRepository<DfEntity> DfRecords =>
        _dfRecords ??= new Repository<DfEntity>(_context);

    public UnitOfWork(ApplicationDbContext context)
    {
        _context = context;
    }

    public IRepository<DnDocumentEntity> Documents =>
        _documents ??= new Repository<DnDocumentEntity>(_context);

    public IRepository<GstEntity> GstRecords =>
        _gstRecords ??= new Repository<GstEntity>(_context);

    public IRepository<GptEntity> GptRecords =>
        _gptRecords ??= new Repository<GptEntity>(_context);

    public IRepository<GfEntity> GfRecords =>
        _gfRecords ??= new Repository<GfEntity>(_context);

    public IRepository<DspnEntity> DspnRecords =>
        _dspnRecords ??= new Repository<DspnEntity>(_context);

    public IRepository<ProfEntity> ProfRecords =>
        _profRecords ??= new Repository<ProfEntity>(_context);

    public IRepository<ProfMerEntity> ProfMerRecords =>
        _profMerRecords ??= new Repository<ProfMerEntity>(_context);

    public IRepository<ImportResponseEntity> ImportResponses =>
        _importResponses ??= new Repository<ImportResponseEntity>(_context);

    /// <summary>
    /// Установка таймаута для всех команд
    /// </summary>
    public void SetCommandTimeout(int seconds)
    {
        // Используем расширение для реляционных баз данных
        _context.Database.SetCommandTimeout(seconds);
    }

    /// <summary>
    /// Сброс таймаута на значение по умолчанию
    /// </summary>
    public void ResetCommandTimeout()
    {
        _context.Database.SetCommandTimeout(null);
    }

    public async Task<int> SaveChangesAsync()
    {
        return await _context.SaveChangesAsync();
    }

    public void Dispose()
    {
        _context.Dispose();
    }

    public async Task<bool> DocumentExistsAsync(string fileName, string period, string? hospitalCode, string fileType)
    {
        return await _context.DnDocuments
            .AnyAsync(x => x.FileName == fileName 
                           && x.Period == period 
                           && x.HospitalCode == hospitalCode 
                           && x.FileType == fileType);
    }
}