namespace ContainerManagerBackend.Helpers
{
    public static class FileHandler
    {
        public static Stream GetFileStream(IFormFile file){
            var stream = file.OpenReadStream();
            return stream;
            
        }
    }
}
