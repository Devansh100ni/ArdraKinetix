using Microsoft.EntityFrameworkCore;
using TaskBoard.Application.Abstractions.Repositories;
using TaskBoard.Application.Common;
using TaskBoard.Domain.Entities;
using TaskBoard.Infrastructure.Persistence;

namespace TaskBoard.Infrastructure.Persistence.Repositories;

public class TaskRepository : ITaskRepository
{
    private readonly TaskBoardDbContext _context;

    public TaskRepository(TaskBoardDbContext context)
    {
        _context = context;
    }

    public async Task<TaskItem?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Tasks
            .FirstOrDefaultAsync(t => t.Id == id && !t.IsDeleted, cancellationToken);
    }

    public async Task<TaskItem?> GetByIdWithDetailsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Tasks
            .Include(t => t.Tenant)
            .Include(t => t.AssignedUser)
            .Include(t => t.Creator)
            .Include(t => t.Status)
            .Include(t => t.Priority)
            .Include(t => t.ParentTask)
            .Include(t => t.ChildTasks.Where(c => !c.IsDeleted))
                .ThenInclude(c => c.Status)
            .Include(t => t.ChildTasks.Where(c => !c.IsDeleted))
                .ThenInclude(c => c.Priority)
            .Include(t => t.ChildTasks.Where(c => !c.IsDeleted))
                .ThenInclude(c => c.AssignedUser)
            .Include(t => t.Comments.Where(c => !c.IsDeleted))
                .ThenInclude(c => c.User)
            .Include(t => t.Attachments.Where(a => !a.IsDeleted))
                .ThenInclude(a => a.Uploader)
            .Include(t => t.Audits)
                .ThenInclude(a => a.User)
            .FirstOrDefaultAsync(t => t.Id == id && !t.IsDeleted, cancellationToken);
    }

    public async Task<TaskItem?> GetByNumberAsync(string taskNumber, Guid tenantId, CancellationToken cancellationToken = default)
    {
        return await _context.Tasks
            .Include(t => t.Status)
            .Include(t => t.Priority)
            .Include(t => t.AssignedUser)
            .FirstOrDefaultAsync(t => t.TaskNumber == taskNumber && t.TenantId == tenantId && !t.IsDeleted, cancellationToken);
    }

    public async Task<PagedResult<TaskItem>> GetPagedAsync(TaskFilterCriteria criteria, CancellationToken cancellationToken = default)
    {
        IQueryable<TaskItem> query = _context.Tasks
                                             .Include(t => t.Tenant)
                                             .Include(t => t.AssignedUser)
                                             .Include(t => t.Creator)
                                             .Include(t => t.Status)
                                             .Include(t => t.Priority)
                                             .Include(t => t.ParentTask)
                                             .Include(t => t.Comments.Where(c => !c.IsDeleted))
                                             .Include(t => t.Attachments.Where(a => !a.IsDeleted))
                                             .Include(t => t.ChildTasks.Where(c => !c.IsDeleted))
                                             .AsNoTracking()
                                             .Where(t => !t.IsDeleted);

        // Tenant Scoping
        if (criteria.TenantId.HasValue)
        {
            query = query.Where(t => t.TenantId == criteria.TenantId.Value);
        }
        else if (criteria.AllowedTenantIds != null && criteria.AllowedTenantIds.Count > 0)
        {
            query = query.Where(t => criteria.AllowedTenantIds.Contains(t.TenantId));
        }

        // Filtering
        if (criteria.StatusId.HasValue)
        {
            query = query.Where(t => t.StatusId == criteria.StatusId.Value);
        }

        if (criteria.PriorityId.HasValue)
        {
            query = query.Where(t => t.PriorityId == criteria.PriorityId.Value);
        }

        if (criteria.AssignedUserId.HasValue)
        {
            query = query.Where(t => t.AssignedUserId == criteria.AssignedUserId.Value);
        }

        if (criteria.ParentTaskId.HasValue)
        {
            query = query.Where(t => t.ParentTaskId == criteria.ParentTaskId.Value);
        }

        if (criteria.OnlyParentTasks)
        {
            query = query.Where(t => t.ParentTaskId == null);
        }

        if (criteria.FromCreatedDate.HasValue)
        {
            query = query.Where(t => t.CreatedOn >= criteria.FromCreatedDate.Value);
        }

        if (criteria.ToCreatedDate.HasValue)
        {
            query = query.Where(t => t.CreatedOn <= criteria.ToCreatedDate.Value);
        }

        if (criteria.FromStartedDate.HasValue)
        {
            query = query.Where(t => t.StartedOn >= criteria.FromStartedDate.Value);
        }

        if (criteria.ToStartedDate.HasValue)
        {
            query = query.Where(t => t.StartedOn <= criteria.ToStartedDate.Value);
        }

        if (criteria.FromEndDate.HasValue)
        {
            query = query.Where(t => t.EndOn >= criteria.FromEndDate.Value);
        }

        if (criteria.ToEndDate.HasValue)
        {
            query = query.Where(t => t.EndOn <= criteria.ToEndDate.Value);
        }

        if (!string.IsNullOrWhiteSpace(criteria.Search))
        {
            var search = criteria.Search.Trim().ToLower();
            query = query.Where(t => t.Title.Contains(search, StringComparison.CurrentCultureIgnoreCase)
                                  || t.TaskNumber.Contains(search, StringComparison.CurrentCultureIgnoreCase)
                                  || (t.Description != null && t.Description.Contains(search, StringComparison.CurrentCultureIgnoreCase)));
        }

        // Sorting
        query = criteria.SortBy?.ToLower() switch
        {
            "tasknumber" => criteria.SortDescending ? query.OrderByDescending(t => t.TaskNumber) : query.OrderBy(t => t.TaskNumber),
            "title" => criteria.SortDescending ? query.OrderByDescending(t => t.Title) : query.OrderBy(t => t.Title),
            "status" => criteria.SortDescending ? query.OrderByDescending(t => t.Status.DisplayOrder) : query.OrderBy(t => t.Status.DisplayOrder),
            "priority" => criteria.SortDescending ? query.OrderByDescending(t => t.Priority.DisplayOrder) : query.OrderBy(t => t.Priority.DisplayOrder),
            "assignee" => criteria.SortDescending ? query.OrderByDescending(t => t.AssignedUser!.FirstName) : query.OrderBy(t => t.AssignedUser!.FirstName),
            "startedon" => criteria.SortDescending ? query.OrderByDescending(t => t.StartedOn) : query.OrderBy(t => t.StartedOn),
            "endon" => criteria.SortDescending ? query.OrderByDescending(t => t.EndOn) : query.OrderBy(t => t.EndOn),
            _ => criteria.SortDescending ? query.OrderByDescending(t => t.CreatedOn) : query.OrderBy(t => t.CreatedOn)
        };

        int totalCount = await query.CountAsync(cancellationToken);
        List<TaskItem> items = await query.Skip((criteria.PageNumber - 1) * criteria.PageSize)
                                          .Take(criteria.PageSize)
                                          .ToListAsync(cancellationToken);

        return new PagedResult<TaskItem>(items, totalCount, criteria.PageNumber, criteria.PageSize);
    }

    public async Task<IReadOnlyList<TaskItem>> GetBoardTasksAsync(Guid? tenantId, IReadOnlyList<Guid>? allowedTenantIds, CancellationToken cancellationToken = default) 
    {
        IQueryable<TaskItem> query = _context.Tasks
            .Include(t => t.Tenant)
            .Include(t => t.AssignedUser)
            .Include(t => t.Creator)
            .Include(t => t.Status)
            .Include(t => t.Priority)
            .Include(t => t.ParentTask)
            .Include(t => t.Comments.Where(c => !c.IsDeleted))
            .Include(t => t.Attachments.Where(a => !a.IsDeleted))
            .Include(t => t.ChildTasks.Where(c => !c.IsDeleted))
            .AsNoTracking()
            .Where(t => !t.IsDeleted);

        if (tenantId.HasValue)
        {
            query = query.Where(t => t.TenantId == tenantId.Value);
        }
        else if (allowedTenantIds != null && allowedTenantIds.Count > 0)
        {
            query = query.Where(t => allowedTenantIds.Contains(t.TenantId));
        }

        return await query
            .OrderBy(t => t.Priority.DisplayOrder)
            .ThenByDescending(t => t.CreatedOn)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<TaskItem>> GetChildTasksAsync(Guid parentTaskId, CancellationToken cancellationToken = default)
    {
        return await _context.Tasks
            .Include(t => t.Status)
            .Include(t => t.Priority)
            .Include(t => t.AssignedUser)
            .AsNoTracking()
            .Where(t => t.ParentTaskId == parentTaskId && !t.IsDeleted)
            .OrderBy(t => t.TaskNumber)
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> HasCircularDependencyAsync(Guid taskId, Guid prospectiveParentId, CancellationToken cancellationToken = default)
    {
        if (taskId == prospectiveParentId)
        {
            return true;
        }

        // Traverse upwards from prospectiveParentId to see if taskId is reached
        Guid? currentParentId = (Guid?)prospectiveParentId;
        HashSet<Guid> visited = [];

        while (currentParentId.HasValue)
        {
            if (currentParentId.Value == taskId)
            {
                return true;
            }

            if (!visited.Add(currentParentId.Value))
            {
                // Circular detected in ancestor tree
                return true;
            }

            var nextParent = await _context.Tasks
                .AsNoTracking()
                .Where(t => t.Id == currentParentId.Value && !t.IsDeleted)
                .Select(t => t.ParentTaskId)
                .FirstOrDefaultAsync(cancellationToken);

            currentParentId = nextParent;
        }

        return false;
    }

    public async Task AddAsync(TaskItem task, CancellationToken cancellationToken = default)
    {
        await _context.Tasks.AddAsync(task, cancellationToken);
    }

    public void Update(TaskItem task)
    {
        _context.Tasks.Update(task);
    }

    // Comments
    public async Task<TaskComment?> GetCommentByIdAsync(Guid commentId, CancellationToken cancellationToken = default)
    {
        return await _context.TaskComments
            .Include(c => c.User)
            .FirstOrDefaultAsync(c => c.Id == commentId && !c.IsDeleted, cancellationToken);
    }

    public async Task AddCommentAsync(TaskComment comment, CancellationToken cancellationToken = default)
    {
        await _context.TaskComments.AddAsync(comment, cancellationToken);
    }

    public void UpdateComment(TaskComment comment)
    {
        _context.TaskComments.Update(comment);
    }

    // Attachments
    public async Task<TaskAttachment?> GetAttachmentByIdAsync(Guid attachmentId, CancellationToken cancellationToken = default)
    {
        return await _context.TaskAttachments
            .Include(a => a.Uploader)
            .FirstOrDefaultAsync(a => a.Id == attachmentId && !a.IsDeleted, cancellationToken);
    }

    public async Task AddAttachmentAsync(TaskAttachment attachment, CancellationToken cancellationToken = default)
    {
        await _context.TaskAttachments.AddAsync(attachment, cancellationToken);
    }

    public void UpdateAttachment(TaskAttachment attachment)
    {
        _context.TaskAttachments.Update(attachment);
    }

    // Metrics for Dashboard
    public async Task<int> GetTotalCountAsync(Guid? tenantId, IReadOnlyList<Guid>? allowedTenantIds, CancellationToken cancellationToken = default)
    {
        var query = _context.Tasks.Where(t => !t.IsDeleted);
        if (tenantId.HasValue)
        {
            query = query.Where(t => t.TenantId == tenantId.Value);
        }
        else if (allowedTenantIds != null && allowedTenantIds.Count > 0)
        {
            query = query.Where(t => allowedTenantIds.Contains(t.TenantId));
        }

        return await query.CountAsync(cancellationToken);
    }

    public async Task<int> GetCountByStatusAsync(Guid statusId, Guid? tenantId, IReadOnlyList<Guid>? allowedTenantIds, CancellationToken cancellationToken = default)
    {
        var query = _context.Tasks.Where(t => t.StatusId == statusId && !t.IsDeleted);
        if (tenantId.HasValue)
        {
            query = query.Where(t => t.TenantId == tenantId.Value);
        }
        else if (allowedTenantIds != null && allowedTenantIds.Count > 0)
        {
            query = query.Where(t => allowedTenantIds.Contains(t.TenantId));
        }

        return await query.CountAsync(cancellationToken);
    }

    public async Task<Dictionary<string, int>> GetTaskCountsByStatusAsync(Guid? tenantId, IReadOnlyList<Guid>? allowedTenantIds, CancellationToken cancellationToken = default)
    {
        var query = _context.Tasks.Include(t => t.Status).Where(t => !t.IsDeleted);
        if (tenantId.HasValue)
        {
            query = query.Where(t => t.TenantId == tenantId.Value);
        }
        else if (allowedTenantIds != null && allowedTenantIds.Count > 0)
        {
            query = query.Where(t => allowedTenantIds.Contains(t.TenantId));
        }

        var groups = await query
            .GroupBy(t => t.Status.Name)
            .Select(g => new { StatusName = g.Key, Count = g.Count() })
            .ToListAsync(cancellationToken);

        return groups.ToDictionary(g => g.StatusName, g => g.Count);
    }

    public async Task<Dictionary<string, int>> GetTaskCountsByPriorityAsync(Guid? tenantId, IReadOnlyList<Guid>? allowedTenantIds, CancellationToken cancellationToken = default)
    {
        var query = _context.Tasks.Include(t => t.Priority).Where(t => !t.IsDeleted);
        if (tenantId.HasValue)
        {
            query = query.Where(t => t.TenantId == tenantId.Value);
        }
        else if (allowedTenantIds != null && allowedTenantIds.Count > 0)
        {
            query = query.Where(t => allowedTenantIds.Contains(t.TenantId));
        }

        var groups = await query
            .GroupBy(t => t.Priority.Name)
            .Select(g => new { PriorityName = g.Key, Count = g.Count() })
            .ToListAsync(cancellationToken);

        return groups.ToDictionary(g => g.PriorityName, g => g.Count);
    }

    public async Task<IReadOnlyList<TaskItem>> GetRecentTasksAsync(int count, Guid? tenantId, IReadOnlyList<Guid>? allowedTenantIds, CancellationToken cancellationToken = default)
    {
        var query = _context.Tasks
            .Include(t => t.Tenant)
            .Include(t => t.Status)
            .Include(t => t.Priority)
            .Include(t => t.AssignedUser)
            .AsNoTracking()
            .Where(t => !t.IsDeleted);

        if (tenantId.HasValue)
        {
            query = query.Where(t => t.TenantId == tenantId.Value);
        }
        else if (allowedTenantIds != null && allowedTenantIds.Count > 0)
        {
            query = query.Where(t => allowedTenantIds.Contains(t.TenantId));
        }

        return await query
            .OrderByDescending(t => t.CreatedOn)
            .Take(count)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<TaskItem>> GetOverdueTasksAsync(Guid? tenantId, IReadOnlyList<Guid>? allowedTenantIds, CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var query = _context.Tasks
            .Include(t => t.Tenant)
            .Include(t => t.Status)
            .Include(t => t.Priority)
            .Include(t => t.AssignedUser)
            .AsNoTracking()
            .Where(t => !t.IsDeleted && t.EndOn.HasValue && t.EndOn.Value < now && t.Status.Code != "DONE");

        if (tenantId.HasValue)
        {
            query = query.Where(t => t.TenantId == tenantId.Value);
        }
        else if (allowedTenantIds != null && allowedTenantIds.Count > 0)
        {
            query = query.Where(t => allowedTenantIds.Contains(t.TenantId));
        }

        return await query
            .OrderBy(t => t.EndOn)
            .ToListAsync(cancellationToken);
    }
}
