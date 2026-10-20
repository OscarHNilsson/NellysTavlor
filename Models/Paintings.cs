namespace Paintings.Models;

public class Painting
{
    public long Id{get; set;}
    public string Title { get; set;} = string.Empty;
    public string Medium {get; set;} = string.Empty;

    public decimal Width {get; set;}
    public decimal Height {get; set;}
    public string ImagePath {get;set;} = string.Empty;
    public DateTime UploadedAt {get;set;} = DateTime.UtcNow;
}
