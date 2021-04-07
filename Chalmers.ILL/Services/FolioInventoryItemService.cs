using Chalmers.ILL.Models;
using Chalmers.ILL.Repositories;
using System;

namespace Chalmers.ILL.Services
{
    public class FolioInventoryItemService : IFolioInventoryItemService
    {
        private readonly string path = "/inventory/items";
        private readonly IChillinTextRepository _chillinTextRepository;
        private readonly IFolioRepository _folioRepository;
        private readonly IJsonService _jsonService;

        public FolioInventoryItemService
        (
            IChillinTextRepository chillinTextRepository,
            IFolioRepository folioRepository,
            IJsonService jsonService)
        {
            _folioRepository = folioRepository;
            _jsonService = jsonService;
            _chillinTextRepository = chillinTextRepository;
        }

        public Item Post(InventoryItemBasic item, bool readOnlyAtLibrary)
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
    }
}