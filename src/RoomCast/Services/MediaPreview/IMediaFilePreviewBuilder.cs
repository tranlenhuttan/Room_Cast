using RoomCast.Models;
using RoomCast.Models.ViewModels;

namespace RoomCast.Services.MediaPreview
{
    public interface IMediaFilePreviewBuilder
    {
        MediaFilePreviewViewModel Build(MediaFile mediaFile);
    }
}
