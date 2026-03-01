using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using System.Threading.Tasks;
using System.Linq.Expressions;
using System.Collections.Generic;
using System;

namespace MonadicSharp.Extensions;

/// <summary>
/// Extension methods for DbSet to integrate with monadic types
/// </summary>
public static class DbSetExtensions
{
    /// <summary>
    /// Adds an entity to the database and returns a Result
    /// </summary>
    public static async Task<Result<T>> AddAsync<T>(this DbSet<T> dbSet, T entity) where T : class
    {
        var entry = await dbSet.AddAsync(entity);
        return Result<T>.Success(entry.Entity);
    }

    /// <summary>
    /// Updates an entity in the database and returns a Result
    /// </summary>
    public static Result<T> Update<T>(this DbSet<T> dbSet, T entity) where T : class
    {
        var entry = dbSet.Update(entity);
        return Result<T>.Success(entry.Entity);
    }

    /// <summary>
    /// Removes an entity from the database and returns a Result
    /// </summary>
    public static Result<T> Remove<T>(this DbSet<T> dbSet, T entity) where T : class
    {
        var entry = dbSet.Remove(entity);
        return Result<T>.Success(entry.Entity);
    }

    /// <summary>
    /// Finds an entity by its primary key and returns an Option
    /// </summary>
    public static async Task<Option<T>> FindAsync<T>(this DbSet<T> dbSet, params object[] keyValues) where T : class
    {
        var entity = await dbSet.FindAsync(keyValues);
        return Option<T>.From(entity);
    }

    /// <summary>
    /// Gets the first entity matching the predicate and returns an Option
    /// </summary>
    public static async Task<Option<T>> FirstOrDefaultAsync<T>(this DbSet<T> dbSet, Expression<Func<T, bool>> predicate) where T : class
    {
        var entity = await EntityFrameworkQueryableExtensions.FirstOrDefaultAsync(dbSet, predicate);
        return Option<T>.From(entity);
    }

    /// <summary>
    /// Gets all entities matching the predicate and returns a Result
    /// </summary>
    public static async Task<Result<IEnumerable<T>>> WhereAsync<T>(this DbSet<T> dbSet, Expression<Func<T, bool>> predicate) where T : class
    {
        var entities = await dbSet.Where(predicate).ToListAsync();
        return Result<IEnumerable<T>>.Success(entities);
    }

    /// <summary>
    /// Executes a query and returns a Result with the count
    /// </summary>
    public static async Task<Result<int>> CountAsync<T>(this DbSet<T> dbSet, Expression<Func<T, bool>> predicate) where T : class
    {
        var count = await EntityFrameworkQueryableExtensions.CountAsync(dbSet, predicate);
        return Result<int>.Success(count);
    }

    /// <summary>
    /// Checks if any entity matches the predicate and returns a Result
    /// </summary>
    public static async Task<Result<bool>> AnyAsync<T>(this DbSet<T> dbSet, Expression<Func<T, bool>> predicate) where T : class
    {
        var exists = await EntityFrameworkQueryableExtensions.AnyAsync(dbSet, predicate);
        return Result<bool>.Success(exists);
    }

    /// <summary>
    /// Wraps the result of AddAsync in a Result monad
    /// </summary>
    public static async Task<Result<EntityEntry<T>>> ToResultAsync<T>(this Task<EntityEntry<T>> addTask) where T : class
    {
        var result = await addTask;
        return Result<EntityEntry<T>>.Success(result);
    }

    /// <summary>
    /// Wraps the result of FindAsync in an Option monad
    /// </summary>
    public static async Task<Option<T>> ToOptionAsync<T>(this Task<T?> findTask) where T : class
    {
        var result = await findTask;
        return Option<T>.From(result);
    }

    /// <summary>
    /// Wraps the result of ToListAsync in a Result monad
    /// </summary>
    public static async Task<Result<List<T>>> ToResultAsync<T>(this Task<List<T>> listTask)
    {
        var result = await listTask;
        return Result<List<T>>.Success(result);
    }

    /// <summary>
    /// Wraps the result of CountAsync in a Result monad
    /// </summary>
    public static async Task<Result<int>> ToResultAsync(this Task<int> countTask)
    {
        var result = await countTask;
        return Result<int>.Success(result);
    }

    /// <summary>
    /// Wraps the result of AnyAsync in a Result monad
    /// </summary>
    public static async Task<Result<bool>> ToResultAsync(this Task<bool> anyTask)
    {
        var result = await anyTask;
        return Result<bool>.Success(result);
    }

    /// <summary>
    /// Enables monadic operations on the result of AddAsync
    /// </summary>
    public static async Task<Result<T>> BindAsync<T>(this Task<EntityEntry<T>> addTask, Func<T, Task<Result<T>>> binder) where T : class
    {
        var entry = await addTask;
        return await binder(entry.Entity);
    }

    /// <summary>
    /// Enables monadic operations on the result of FindAsync
    /// </summary>
    public static async Task<Option<T>> BindAsync<T>(this Task<T?> findTask, Func<T, Task<Option<T>>> binder) where T : class
    {
        var entity = await findTask;
        if (entity == null)
            return Option<T>.None;
        return await binder(entity);
    }

    /// <summary>
    /// Enables monadic operations on the result of ToListAsync
    /// </summary>
    public static async Task<Result<List<T>>> BindAsync<T>(this Task<List<T>> listTask, Func<List<T>, Task<Result<List<T>>>> binder)
    {
        var list = await listTask;
        return await binder(list);
    }

    /// <summary>
    /// Enables monadic operations on the result of CountAsync
    /// </summary>
    public static async Task<Result<int>> BindAsync(this Task<int> countTask, Func<int, Task<Result<int>>> binder)
    {
        var count = await countTask;
        return await binder(count);
    }

    /// <summary>
    /// Enables monadic operations on the result of AnyAsync
    /// </summary>
    public static async Task<Result<bool>> BindAsync(this Task<bool> anyTask, Func<bool, Task<Result<bool>>> binder)
    {
        var result = await anyTask;
        return await binder(result);
    }
}

/// <summary>
/// Extension methods for DbContext to support functional operations
/// </summary>
public static class DbContextExtensions
{
    /// <summary>
    /// Saves changes and returns a Result
    /// </summary>
    public static async Task<Result<T>> SaveChangesAsync<T>(this Result<EntityEntry<T>> entry) where T : class
    {
        return await entry.BindAsync(async e =>
        {
            await e.Context.SaveChangesAsync();
            return Result<T>.Success(e.Entity);
        });
    }
}