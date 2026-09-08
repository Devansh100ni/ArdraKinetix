namespace TaskBoard.Domain.Exceptions;

public class NotFoundException : Exception
{
    public string EntityName { get; }
    public object Key { get; }

    public NotFoundException(string entityName, object key)
        : base($"Entity \"{entityName}\" ({key}) was not found.")
    {
        EntityName = entityName;
        Key = key;
    }
}
