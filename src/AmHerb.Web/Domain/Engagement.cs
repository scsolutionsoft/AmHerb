using System.ComponentModel.DataAnnotations;
namespace AmHerb.Web.Domain;
public class MediaAsset : Entity
{
    public long? ProductId { get; set; }
    public Product? Product { get; set; }
    public int Slot { get; set; }
    [MaxLength(30)] public string Purpose { get; set; } = "product";
    [MaxLength(200)] public string Alt { get; set; } = "";
    [MaxLength(40)] public string ContentType { get; set; } = "image/jpeg";
    public byte[] Data { get; set; } = [];
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
public enum ContentAudience { Public, Members, Everyone }
public enum ContentKind { News, Advertisement }
public class ContentPost : VersionedEntity
{
    [MaxLength(180)] public string Title { get; set; } = "";
    [MaxLength(6000)] public string Body { get; set; } = "";
    [MaxLength(300)] public string Link { get; set; } = "/Catalog";
    public ContentAudience Audience { get; set; }
    public ContentKind Kind { get; set; }
    public DateTime StartsAt { get; set; }
    public DateTime EndsAt { get; set; }
    public bool Published { get; set; }
    public long? MediaAssetId { get; set; }
    public MediaAsset? MediaAsset { get; set; }
}
public enum ConversationStatus { WaitingForStaff, WaitingForMember, Closed }
public class Conversation : VersionedEntity
{
    public long MemberId { get; set; }
    public Member Member { get; set; } = null!;
    [MaxLength(180)] public string Subject { get; set; } = "";
    public ConversationStatus Status { get; set; }
    public DateTime UpdatedAt { get; set; }
    public List<ConversationMessage> Messages { get; set; } = [];
}
public class ConversationMessage : Entity
{
    public long ConversationId { get; set; }
    public Conversation Conversation { get; set; } = null!;
    public Guid RequestKey { get; set; }
    [MaxLength(450)] public string SenderUserId { get; set; } = "";
    public bool FromStaff { get; set; }
    [MaxLength(4000)] public string Body { get; set; } = "";
    public DateTime SentAt { get; set; }
    public DateTime? ReadAt { get; set; }
}
public class JournalEntry : Entity
{
    [MaxLength(100)] public string EventKey { get; set; } = "";
    [MaxLength(200)] public string Description { get; set; } = "";
    public long OrderId { get; set; }
    public Order Order { get; set; } = null!;
    public DateTime PostedAt { get; set; }
    public List<JournalLine> Lines { get; set; } = [];
}
public class JournalLine : Entity
{
    public long JournalEntryId { get; set; }
    public JournalEntry JournalEntry { get; set; } = null!;
    [MaxLength(20)] public string Account { get; set; } = "";
    [MaxLength(120)] public string Name { get; set; } = "";
    public decimal Debit { get; set; }
    public decimal Credit { get; set; }
}
