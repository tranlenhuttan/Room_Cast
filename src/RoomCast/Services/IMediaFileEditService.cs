using System.Threading.Tasks;
using RoomCast.Models;
using RoomCast.Models.ViewModels;

namespace RoomCast.Services
{
    public interface IMediaFileEditService
    {
        Task<MediaFileEditViewModel?> BuildEditViewModelAsync(int fileId, ApplicationUser user);

        Task<MediaFileEditResult> UpdateAsync(MediaFileEditViewModel model, ApplicationUser user);
    }
}
