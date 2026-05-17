using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SchoolsManagement.Api.Models.School;

[Table("sync_checkpoints")]
public class SyncCheckpointRecord
{
    [Key]
    [MaxLength(120)]
    public string Key { get; set; } = string.Empty;

    [Column("synced_at")]
    public DateTimeOffset SyncedAt { get; set; }
}
