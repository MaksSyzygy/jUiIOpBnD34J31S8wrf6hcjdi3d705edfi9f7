using Checkitlink.Models.Data;

namespace Checkitlink.Models.ViewModels
{
    //модель лайков/дизлайков
    public class LinkLikeStatusVM
    {
        public int LinkListNumber { get; set; }
        public int LinkId { get; set; }
        public bool LikeStatus { get; set; }
        public int LikeAuthorId { get; set; }

        public LinkLikeStatusVM(LinkLikeStatusDTO row)
        {
            LinkListNumber = row.LinkListNumber;
            LinkId = row.LinkId;
            LikeStatus = row.LikeStatus;
            LikeAuthorId = row.LikeAuthorId;
        }

        public LinkLikeStatusVM() { }
    }
}