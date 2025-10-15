namespace Movie_API.Models.DTO
{
    public class StudioWithMoviesAndActorsDTO
    {
        public string Name { get; set; }
        public List<MovieActorDTO> MovieActors { set; get; }
    }
}
