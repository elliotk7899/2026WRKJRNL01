
public partial class GoogleCalendarConnection
{
    public Guid PkId { get; set; }

    public string UserEmail { get; set; } = string.Empty;

    public string GoogleRefreshToken { get; set; } = string.Empty;

    public bool GoogleConnected { get; set; }

    public bool AutoImportEvents { get; set; }

    public DateTime CreatedDateUtc { get; set; }

    public DateTime UpdatedDateUtc { get; set; }
}