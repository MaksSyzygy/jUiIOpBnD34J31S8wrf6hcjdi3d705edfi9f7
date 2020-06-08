using Checkitlink.Models.Data;

namespace Checkitlink.Models.ViewModels
{
    //подписка на юзера
    public class SubscribeOnUserVM
    {
        public int FollowNumber { get; set; }
        public int UserSubscriber { get; set; }
        public int LeadUser { get; set; }
        public bool SubscribeStatus { get; set; } = false;

        public SubscribeOnUserVM(SubscribeOnUserDTO row)
        {
            FollowNumber = row.FollowNumber;
            UserSubscriber = row.UserSubscriber;
            LeadUser = row.LeadUser;
            SubscribeStatus = row.SubscribeStatus;
        }

        public SubscribeOnUserVM() { }
    }
}