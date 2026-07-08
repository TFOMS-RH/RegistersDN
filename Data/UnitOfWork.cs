using RegistrDN.Data.Repositories;
using RegistrDN.Models.Entities;

namespace RegistrDN.Data;

public interface IUnitOfWork : IDisposable
{
    IRepository<DnDocumentEntity> Documents { get; }
    IRepository<GstEntity> GstRecords { get; }
    IRepository<GptEntity> GptRecords { get; }
    IRepository<GfEntity> GfRecords { get; }
    Task<int> SaveChangesAsync();
}

public class UnitOfWork : IUnitOfWork
{
    private readonly ApplicationDbContext _context;
    private IRepository<DnDocumentEntity>? _documents;
    private IRepository<GstEntity>? _gstRecords;
    private IRepository<GptEntity>? _gptRecords;
    private IRepository<GfEntity>? _gfRecords;

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

    public async Task<int> SaveChangesAsync()
    {
        return await _context.SaveChangesAsync();
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}