using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ConsoleApp1
{
    public class CatalogService : ICatalogService
    {
        private readonly ILibraryItemRepository _libraryItemRepository;
        private readonly ICopyRepository _copyRepository;
        private readonly IValidationService _validationService;

        public CatalogService(
            ILibraryItemRepository libraryItemRepository,
            ICopyRepository copyRepository,
            IValidationService validationService)
        {
            _libraryItemRepository = libraryItemRepository
                                     ?? throw new ArgumentNullException(nameof(libraryItemRepository));
            _copyRepository = copyRepository
                              ?? throw new ArgumentNullException(nameof(copyRepository));
            _validationService = validationService
                                 ?? throw new ArgumentNullException(nameof(validationService));
        }

        // ДОБАВЛЕНИЕ/ЗАМЕНА ПАРАМЕТРА:
        // раньше был только dto, теперь добавлен optional-параметр createdBy,
        // чтобы фиксировать автора записи; в будущем можно передавать из UserSession.
        public async Task<int?> AddLibraryItemAsync(LibraryItemDto dto, string? createdBy = null)
        {
            if (!IsValidLibraryItem(dto))
                return null;

            // ВСТРАИВАНИЕ/ЗАМЕНА ВРЕМЕННОЙ ПЕРЕМЕННОЙ:
            // вместо временной переменной itemId мы сразу возвращаем item.Id
            var item = MapDtoToEntity(dto, createdBy);

            await _libraryItemRepository.AddAsync(item);
            return item.Id;
        }

        // ВЫДЕЛЕНИЕ МЕТОДА:
        // общая проверка вынесена из AddLibraryItemAsync в отдельный метод.
        private bool IsValidLibraryItem(LibraryItemDto dto)
        {
            if (dto == null)
                return false;

            if (!_validationService.ValidateIsbn(dto.Isbn))
                return false;

            if (string.IsNullOrWhiteSpace(dto.Title))
                return false;

            if (!_validationService.ValidateYear(dto.Year))
                return false;

            return true;
        }

        // ВЫДЕЛЕНИЕ МЕТОДА + ЗАМЕНА ВРЕМЕННОЙ ПЕРЕМЕННОЙ:
        // маппинг DTO → сущность вынесен в отдельный метод,
        // чтобы его переиспользовать и проще изменять.
        private static LibraryItem MapDtoToEntity(LibraryItemDto dto, string? createdBy)
        {
            return new LibraryItem
            {
                Title = dto.Title.Trim(),
                Authors = dto.Authors?.Trim(),
                Year = dto.Year,
                Isbn = dto.Isbn,
                Udk = dto.Udk,
                Bbk = dto.Bbk,
                ItemType = dto.ItemType,
                SubjectAreaId = dto.SubjectAreaId,
                CreatedBy = createdBy,
                CreatedAt = DateTime.Now
            };
        }

        // ПРИМЕР ВСТРАИВАНИЯ МЕТОДА:
        // предположим, раньше был отдельный метод SearchByTitleAsync,
        // который просто вызывал SearchAsync(query).
        // Его можно удалить, а вызовы заменить прямым SearchAsync.
        public Task<IEnumerable<LibraryItem>> SearchAsync(string query) =>
            _libraryItemRepository.SearchAsync(query);

        public Task<LibraryItem?> GetByIdAsync(int id) =>
            _libraryItemRepository.GetByIdAsync(id);

        // ПЕРЕМЕЩЕНИЕ МЕТОДА:
        // пример метода, который логичнее держать в репозитории, а не в сервисе.
        // ты можешь перенести этот метод в ILibraryItemRepository/LibraryItemRepository
        // и здесь оставить только тонкий вызов, либо вообще убрать из сервиса.
        public Task<IEnumerable<LibraryItem>> GetByAuthorAsync(string author)
        {
            // после перемещения логики в репозиторий здесь останется один вызов:
            return _libraryItemRepository.GetByAuthorAsync(author);
        }
    }

    // Пример изменения DTO и сущности под рефакторинг:

    public class LibraryItemDto
    {
        public string Title { get; set; } = string.Empty;
        public string? Authors { get; set; }
        public int Year { get; set; }
        public string? Isbn { get; set; }
        public string? Udk { get; set; }
        public string? Bbk { get; set; }
        public string ItemType { get; set; } = string.Empty;
        public int SubjectAreaId { get; set; }
    }

    public class LibraryItem
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Authors { get; set; }
        public int Year { get; set; }
        public string? Isbn { get; set; }
        public string? Udk { get; set; }
        public string? Bbk { get; set; }
        public string ItemType { get; set; } = string.Empty;
        public int SubjectAreaId { get; set; }

        // новое поле/параметр — пример добавления параметра и его использования
        public string? CreatedBy { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public interface ICatalogService
    {
        Task<int?> AddLibraryItemAsync(LibraryItemDto dto, string? createdBy = null);
        Task<IEnumerable<LibraryItem>> SearchAsync(string query);
        Task<LibraryItem?> GetByIdAsync(int id);
        Task<IEnumerable<LibraryItem>> GetByAuthorAsync(string author);
    }

    public interface ILibraryItemRepository
    {
        Task AddAsync(LibraryItem item);
        Task<IEnumerable<LibraryItem>> SearchAsync(string query);
        Task<LibraryItem?> GetByIdAsync(int id);
        Task<IEnumerable<LibraryItem>> GetByAuthorAsync(string author);
    }

    public interface ICopyRepository { }

    public interface IValidationService
    {
        bool ValidateIsbn(string? isbn);
        bool ValidateYear(int year);
    }
}
