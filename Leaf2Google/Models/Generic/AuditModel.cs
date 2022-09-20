namespace Leaf2Google.Models.Generic
{
    public enum AuditContext
    {
        Leaf,
        Google,
        Account
    }

    public enum AuditAction
    {
        Access,
        Execute,
        Delete,
        Update,
        Create,
        Modify
    }

    public class Audit<T> : BaseModel
    {
        public Guid Id { get; set; }

        public virtual T? Owner { get; set; }

        public AuditContext Context { get; set; }

        public AuditAction Action { get; set; }

        public string? Data { get; set; }

        public DateTime Time { get; set; } = DateTime.UtcNow;
    }
}