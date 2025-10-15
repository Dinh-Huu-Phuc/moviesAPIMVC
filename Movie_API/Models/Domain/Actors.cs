using System.ComponentModel.DataAnnotations;

namespace Movie_API.Models.Domain
{
    public class Actors
    {
        [Key]
        public int Id { get; set; }

        public string FullName { get; set; }

        // Many-to-many: Author <-> Book (via Book_Authors)
        public List<Movie_Actors> Movie_Actors { get; set; }
    }
}
