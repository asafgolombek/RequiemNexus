using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RequiemNexus.Data.Models;

public class FidoStoredCredential
{
    [Key]
    public string UserId { get; set; } = string.Empty;

    public byte[] PublicKey { get; set; } = Array.Empty<byte>();

    public byte[] UserHandle { get; set; } = Array.Empty<byte>();

    public uint SignatureCounter { get; set; }

    public string CredId { get; set; } = string.Empty;

    public DateTime RegDate { get; set; }

    public Guid AaGuid { get; set; }

    public string CredType { get; set; } = string.Empty;

    // Navigation property
    [ForeignKey("UserId")]
    public virtual ApplicationUser User { get; set; } = null!;
}
