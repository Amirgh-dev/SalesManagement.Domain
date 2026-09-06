namespace SalesManagement.Domain.Entities
{
    /// <summary>
    /// دسته‌بندی (گروه فروش) کالا از جدول GNR.Grouping.
    /// </summary>
    public class Category
    {
        public int Id { get; private set; }
        public string? Code { get; private set; }
        public string? Title { get; private set; }
        public int? ParentGroupRef { get; private set; }
        public string? EntityType { get; private set; }

        /// <summary>دسته‌ی سطح اول (کدِ تک‌رقمی) در برابر زیرگروه.</summary>
        public bool IsTopLevel => Code is { Length: 1 };

        private Category() { }

        public static Category Reconstitute(
            int id, string? code, string? title, int? parentGroupRef, string? entityType)
        {
            return new Category
            {
                Id = id,
                Code = code,
                Title = title,
                ParentGroupRef = parentGroupRef,
                EntityType = entityType
            };
        }
    }
}
