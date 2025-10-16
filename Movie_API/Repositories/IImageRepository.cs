using Movie_API.Models.Domain;

namespace Movie_API.Repositories
{
    public interface IImageRepository
    {

        void Upload(Image image);

        IEnumerable<Image> GetAllInfoImages();


        (byte[]?, string /*downloadName*/, string /*contentType*/) DownloadFile(int id);

        Image? GetById(int id);

        void Update(Image image);

        bool Delete(int id);


    }
}