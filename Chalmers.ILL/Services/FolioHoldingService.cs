using Chalmers.ILL.Models;
using Chalmers.ILL.Repositories;
using System;

namespace Chalmers.ILL.Services
{
    public class FolioHoldingService : IFolioHoldingService
    {
        private readonly string path = "/holdings-storage/holdings";
        private readonly IFolioRepository _folioRepository;
        private readonly IJsonService _jsonService;

        public FolioHoldingService(IFolioRepository folioRepository, IJsonService jsonService)
        {
            _folioRepository = folioRepository;
            _jsonService = jsonService;
        }

        public Holding Post(HoldingBasic item)
        {
            if (item == null)
            {
                throw new ArgumentNullException(nameof(item));
            }
            var response = _folioRepository.Post(path, _jsonService.SerializeObject(item));
            return _jsonService.DeserializeObject<Holding>(response);
        }
    }
}