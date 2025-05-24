using System.ComponentModel.DataAnnotations;

namespace EmoTagger.Models
{
    public class ProfileViewModel
    {
        [Required]
        public string FirstName { get; set; }
        [Required]
        public string LastName { get; set; }
        [Required]
        public string Email { get; set; }

        [Required]
        public string PhoneNumber { get; set; }
        public string Country { get; set; }
        public string ProfileImageUrl { get; set; }
        public int IncomingRequestsCount { get; set; }
        public string MostTaggedGenre { get; set; }
        public int MostTaggedCount { get; set; }
        public int TotalTagCount { get; set; }
        public int TotalPlayedMusic { get; set; }
        public int FavoriteCount { get; set; }
        public List<FriendViewModel> Friends { get; set; }
        public List<FriendRequestViewModel> IncomingRequests { get; set; }
        
        // Pagination properties
        public int FriendsPage { get; set; } = 1;
        public int RequestsPage { get; set; } = 1;
        public int FriendsTotalPages { get; set; }
        public int RequestsTotalPages { get; set; }
        public int PageSize { get; set; } = 5;
    }
}
