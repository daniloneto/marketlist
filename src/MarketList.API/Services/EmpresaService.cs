using MarketList.Application.DTOs;
using MarketList.Application.Interfaces;
using MarketList.Domain.Entities;
using MarketList.Domain.Interfaces;
using MarketList.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace MarketList.API.Services;

public class EmpresaService : IEmpresaService
{
    private readonly AppDbContext _context;
    private readonly IUnitOfWork _unitOfWork;

    public EmpresaService(AppDbContext context, IUnitOfWork unitOfWork)
    {
        _context = context;
        _unitOfWork = unitOfWork;
    }

    public async Task<IEnumerable<EmpresaDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var empresas = await _context.Empresas
            .Include(e => e.Listas)
            .OrderBy(e => e.Nome)
            .ToListAsync(cancellationToken);

        return empresas.Select(e => new EmpresaDto(
            e.Id,
            e.Nome,
            e.Cnpj,
            e.CreatedAt,
            e.Listas.Count
        ));
    }

    public async Task<EmpresaDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var empresa = await _context.Empresas
            .Include(e => e.Listas)
            .FirstOrDefaultAsync(e => e.Id == id, cancellationToken);

        if (empresa == null)
            return null;

        return new EmpresaDto(
            empresa.Id,
            empresa.Nome,
            empresa.Cnpj,
            empresa.CreatedAt,
            empresa.Listas.Count
        );
    }

    public async Task<EmpresaDto> CreateAsync(EmpresaCreateDto dto, CancellationToken cancellationToken = default)
    {
        var empresa = new Empresa
        {
            Id = Guid.NewGuid(),
            Nome = dto.Nome,
            Cnpj = dto.Cnpj,
            CreatedAt = MarketList.Domain.Helpers.DateTimeHelper.EnsureUtc(DateTime.UtcNow)
        };

        _context.Empresas.Add(empresa);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new EmpresaDto(
            empresa.Id,
            empresa.Nome,
            empresa.Cnpj,
            empresa.CreatedAt,
            0
        );
    }

    public async Task<EmpresaDto?> UpdateAsync(Guid id, EmpresaUpdateDto dto, CancellationToken cancellationToken = default)
    {
        var empresa = await _context.Empresas.FindAsync([id], cancellationToken);
        if (empresa == null)
            return null;

        empresa.Nome = dto.Nome;
        empresa.Cnpj = dto.Cnpj;
        empresa.UpdatedAt = MarketList.Domain.Helpers.DateTimeHelper.EnsureUtc(DateTime.UtcNow);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var count = await _context.ListasDeCompras.CountAsync(l => l.EmpresaId == id, cancellationToken);

        return new EmpresaDto(
            empresa.Id,
            empresa.Nome,
            empresa.Cnpj,
            empresa.CreatedAt,
            count
        );
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var empresa = await _context.Empresas.FindAsync([id], cancellationToken);
        if (empresa == null)
            return false;

        _context.Empresas.Remove(empresa);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return true;
    }
}
