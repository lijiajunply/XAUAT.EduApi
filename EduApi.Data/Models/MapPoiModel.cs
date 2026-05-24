using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace CampusMapAPI.Models;

[Table("map_pois")]
public class MapPoiModel : EduApi.Data.DataModel
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    [Required]
    [MaxLength(100)]
    [JsonPropertyName("name")]
    public string Name { get; set; } = "";

    [Required]
    [MaxLength(50)]
    [JsonPropertyName("category")]
    public string Category { get; set; } = "";

    [Required]
    [Column(TypeName = "decimal(10, 7)")]
    [JsonPropertyName("latitude")]
    public decimal Latitude { get; set; }

    [Required]
    [Column(TypeName = "decimal(10, 7)")]
    [JsonPropertyName("longitude")]
    public decimal Longitude { get; set; }

    [MaxLength(500)]
    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [MaxLength(200)]
    [JsonPropertyName("address")]
    public string? Address { get; set; }

    [MaxLength(50)]
    [JsonPropertyName("campus")]
    public string? Campus { get; set; }

    [MaxLength(200)]
    [JsonPropertyName("icon")]
    public string? Icon { get; set; }

    [JsonPropertyName("is_active")]
    public bool IsActive { get; set; } = true;

    [JsonPropertyName("sort_order")]
    public int SortOrder { get; set; } = 0;

    [DatabaseGenerated(DatabaseGeneratedOption.Computed)]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [DatabaseGenerated(DatabaseGeneratedOption.Computed)]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
