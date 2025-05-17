using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;


namespace EmoTagger.Models
{
    public class User
    {
        [Key]
     
        public int Id { get; set; }

        [Required]
        [StringLength(50)]
        public string FirstName { get; set; }

        [Required]
        [StringLength(50)]
        public string LastName { get; set; }

        [Required]
        [EmailAddress]
        public string Email { get; set; }

        [Required]
        [StringLength(100, MinimumLength = 6)]
        [DataType(DataType.Password)]
        public string Password { get; set; }

        [Required]
        [StringLength(15)]
        public string PhoneNumber { get; set; }

        [Required]
        [StringLength(50)]
        public string Country { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [NotMapped]
        [Required(ErrorMessage = "Şifre tekrarı zorunludur.")]
        [Compare("Password", ErrorMessage = "Şifreler uyuşmuyor!")]
        [DataType(DataType.Password)]
        public string ConfirmPassword { get; set; }

        [Column("reset_token")]
        public string? ResetToken { get; set; }

        [Column("reset_token_expiry")]
        public DateTime? ResetTokenExpiry { get; set; }

        [Column("profile_image_url")]
        [StringLength(255)]
        public string? ProfileImageUrl { get; set; }
        // Yeni eklenen ilişkisel özellikler
        public virtual ICollection<Music> Musics { get; set; }
        public bool IsAdmin { get; set; } = false;


    }
}