using static System.Reflection.Metadata.BlobBuilder;
using System.ComponentModel.DataAnnotations;

namespace Movie_API.Models.Domain
{
    public class Studios
    {
        [Key]
        public int Id { get; set; }
        public string Name { get; set; }

        // Navigation property cho quan hệ một-nhiều
        public List<Movies> Movies { get; set; }
    }
}
