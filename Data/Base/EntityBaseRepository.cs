﻿using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using RopeyDVDSystem.Data;

namespace eTickets.Data.Base;

public class EntityBaseRepository<T> : IEntityBaseRepository<T> where T : class, IEntityBase, new()
{
    private readonly ApplicationDbContext _context;

    public EntityBaseRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(T entity)
    {
        await _context.Set<T>().AddAsync(entity);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(int id)
    {
        var entity = await _context.Set<T>().FirstOrDefaultAsync(n => n.DVDNumber == id);
        EntityEntry entityEntry = _context.Entry<T>(entity);
        entityEntry.State = EntityState.Deleted;

        await _context.SaveChangesAsync();
    }

    public async Task<IEnumerable<T>> GetAllAsync()
    {
        return await _context.Set<T>().ToListAsync();
    }

    public async Task<IEnumerable<T>> GetAllAsync(params Expression<Func<T, object>>[] includeProperties)
    {
        IQueryable<T> query = _context.Set<T>();
        query = includeProperties.Aggregate(query, (current, includeProperty) => current.Include(includeProperty));
        return await query.ToListAsync();
    }

    public async Task<T> GetDVDTitleAsync(int id)
    {
        return await _context.Set<T>().FirstOrDefaultAsync(n => n.DVDNumber == id);
    }

    public async Task<T> GetDVDTitleAsync(int id, params Expression<Func<T, object>>[] includeProperties)
    {
        IQueryable<T> query = _context.Set<T>();
        query = includeProperties.Aggregate(query, (current, includeProperty) => current.Include(includeProperty));
        return await query.FirstOrDefaultAsync(n => n.DVDNumber == id);
    }

    public async Task UpdateAsync(int id, T entity)
    {
        EntityEntry entityEntry = _context.Entry(entity);
        entityEntry.State = EntityState.Modified;

        await _context.SaveChangesAsync();
    }
}