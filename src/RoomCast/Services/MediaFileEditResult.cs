using System.Collections.Generic;
using RoomCast.Models;

namespace RoomCast.Services
{
    public record MediaFileEditError(string? FieldName, string Message);

    public class MediaFileEditResult
    {
        public bool Succeeded => Errors.Count == 0;

        public List<MediaFileEditError> Errors { get; } = new();

        public MediaFile? UpdatedFile { get; set; }
    }
}
