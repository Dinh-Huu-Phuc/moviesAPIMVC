using System.ComponentModel.DataAnnotations;

namespace Movie_API.Models.DTO
{
    public class LoginRequestDTO
    {
        [Required, DataType(DataType.EmailAddress)]
        public string Username { get; set; }

        [Required, DataType(DataType.Password)]
        public string Password { get; set; }
    }
}
