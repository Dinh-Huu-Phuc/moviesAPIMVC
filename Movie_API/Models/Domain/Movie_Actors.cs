using System.ComponentModel.DataAnnotations.Schema;
using static System.Reflection.Metadata.BlobBuilder;

namespace Movie_API.Models.Domain
{
    [Table("Movie_Actors")] 
    public class Movie_Actors
    {
        public int Id { get; set; }

        // Foreign keys
        public int MovieId { get; set; } 
        public Movies Movie { get; set; } 

        public int ActorId { get; set; } 
        public Actors Actor { get; set; } 
    }

}
