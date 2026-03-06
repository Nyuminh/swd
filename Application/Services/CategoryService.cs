using swd.Application.DTOs.Category;
using swd.Domain.Interfaces;

namespace swd.Application.Services
{
    public class CategoryService
    {
        private readonly ICategoryRepository _categoryRepository;

        public CategoryService(ICategoryRepository categoryRepository)
        {
            _categoryRepository = categoryRepository;
        }

        public async Task<List<Category>> GetAllAsync()
            => await _categoryRepository.GetAllAsync();

        public async Task<Category> GetByIdAsync(string id)
        {
            var category = await _categoryRepository.GetByIdAsync(id);
            if (category is null)
                throw new KeyNotFoundException($"Category with id '{id}' was not found.");

            return category;
        }

        public async Task<Category> CreateCategoryAsync(CreateCategoryRequest request)
        {
            var category = new Category
            {
                Name = request.Name,
                Description = request.Description
            };

            await _categoryRepository.CreateAsync(category);
            return category;
        }

        public async Task<Category> UpdateCategoryAsync(string id, UpdateCategoryRequest request)
        {
            var category = await _categoryRepository.GetByIdAsync(id);
            if (category is null)
                throw new KeyNotFoundException($"Category with id '{id}' was not found.");

            category.Name = request.Name;
            category.Description = request.Description;

            await _categoryRepository.UpdateAsync(id, category);
            return category;
        }

        public async Task DeleteCategoryAsync(string id)
        {
            var category = await _categoryRepository.GetByIdAsync(id);
            if (category is null)
                throw new KeyNotFoundException($"Category with id '{id}' was not found.");

            await _categoryRepository.DeleteAsync(id);
        }
    }
}
