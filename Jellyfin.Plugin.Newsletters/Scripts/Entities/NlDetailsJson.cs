#pragma warning disable 1591
namespace Jellyfin.Plugin.Newsletters.Scripts.ENTITIES;

public class NlDetailsJson
{
    /// <summary>
    /// Initializes a new instance of the <see cref="NlDetailsJson"/> class.
    /// </summary>
    public NlDetailsJson()
    {
        Title = string.Empty;
        Season = 0;
        Episode = 0;
        EpisodeRange = string.Empty;
    }

    public string Title { get; set; }

    public int Season { get; set; }

    public int Episode { get; set; }

    public string EpisodeRange { get; set; }
}