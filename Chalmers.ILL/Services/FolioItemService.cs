using Chalmers.ILL.Models;
using Chalmers.ILL.Repositories;
using System;

namespace Chalmers.ILL.Services
{
    public class FolioItemService : IFolioItemService
    {
        private readonly string path = "/item-storage/items";
        private readonly IChillinTextRepository _chillinTextRepository;
        private readonly IFolioRepository _folioRepository;
        private readonly IJsonService _jsonService;

        public FolioItemService
        (
            IChillinTextRepository chillinTextRepository,
            IFolioRepository folioRepository, 
            IJsonService jsonService)
        {
            _folioRepository = folioRepository;
            _jsonService = jsonService;
            _chillinTextRepository = chillinTextRepository;
        }

        public ItemQuery ByQuery(string query)
        {
            if (string.IsNullOrEmpty(query))
            {
                throw new ArgumentNullException(nameof(query));
            }
            var response = _folioRepository.ByQuery($"{path}?query=({query})");
            return _jsonService.DeserializeObject<ItemQuery>(response);
        }

        public Item Post(ItemBasic item, bool readOnlyAtLibrary)
        {
            if (item == null)
            {
                throw new ArgumentNullException(nameof(item));
            }

            item.CirculationNotes.Add(new CirculationNote
            {
                NoteType = "Check in",
                Note = _chillinTextRepository.ByTextField("checkInNote").CheckInNote,
            });

            if (readOnlyAtLibrary)
            {
                item.CirculationNotes.Add(new CirculationNote
                {
                    NoteType = "Check out",
                    Note = _chillinTextRepository.ByTextField("checkOutNote").CheckOutNote,
                });
            }

            var response = _folioRepository.Post(path, _jsonService.SerializeObject(item));
            return _jsonService.DeserializeObject<Item>(response);
        }

        public Item Put(Item item)
        {
            if (item == null)
            {
                throw new ArgumentNullException(nameof(item));
            }
            var response = _folioRepository.Put($"{path}/{item.Id}", _jsonService.SerializeObject(item));
            return _jsonService.DeserializeObject<Item>(response);
        }
    }
}