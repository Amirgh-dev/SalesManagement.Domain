namespace SalesManagement.Domain.Entities
{
    public class Location
    {
        public int Id { get; private set; }
        public string? Title { get; private set; }
        public string? TitleEn { get; private set; }
        public string? Code { get; private set; }
        public int? Parent { get; private set; } 
        public int Type { get; private set; }     

        private Location() { }

        public static Location Reconstitute(
            int id, string? title, string? titleEn, string? code, int? parent, int type)
        {
            return new Location
            {
                Id = id,
                Title = title,
                TitleEn = titleEn,
                Code = code,
                Parent = parent,
                Type = type
            };
        }
    }
}