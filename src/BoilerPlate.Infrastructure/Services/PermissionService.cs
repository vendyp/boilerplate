﻿using BoilerPlate.Core.Abstractions;
using BoilerPlate.Domain.Entities;
using BoilerPlate.Shared.Abstraction.Databases;
using Microsoft.EntityFrameworkCore;

namespace BoilerPlate.Infrastructure.Services;

internal class PermissionService : IPermissionService
{
    private readonly IDbContext _dbContext;

    public PermissionService(IDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<Permission?> GetPermissionByIdAsync(string id, CancellationToken cancellationToken)
        => _dbContext.Set<Permission>().Where(e => e.Code == id).FirstOrDefaultAsync(cancellationToken);

    public async Task<bool> AllIdIsValidAsync(string[] ids, CancellationToken cancellationToken)
    {
        var listId = ids.ToList();

        var listPermission = await _dbContext.Set<Permission>().AsNoTracking().Where(e => listId.Contains(e.Code))
            .ToListAsync(cancellationToken);

        if (!listPermission.Any()) return false;

        return listPermission.Count == listId.Count;
    }
}