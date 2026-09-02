using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace InfrastructureManager.Web.ViewModels.Locations;

public class UploadLocationPhotoViewModel
{
    public int LocationId { get; set; }

    [Required]
    public IFormFile File { get; set; }
        = null!;

    public string? Caption { get; set; }
}