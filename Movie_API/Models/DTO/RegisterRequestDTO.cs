using System.ComponentModel.DataAnnotations;

namespace Movie_API.Models.DTO
{
    public class RegisterRequestDTO
    {
        [Required]
        [EmailAddress(ErrorMessage = "Email không hợp lệ.")]
        public string Username { get; set; }

        [Required]
        [DataType(DataType.Password)]
        [MinLength(6, ErrorMessage = "Mật khẩu phải có ít nhất 6 ký tự.")]
        public string Password { get; set; }

        public string[] Roles { get; set; } = new string[] { "User" };
    }
}
